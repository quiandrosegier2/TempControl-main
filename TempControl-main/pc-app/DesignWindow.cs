using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Diagnostics;

sealed class DesignWindow : Form
{
    [DllImport("dwmapi.dll")]
    static extern int DwmSetWindowAttribute(IntPtr window,int attribute,ref int value,int size);
    ScreenDesign design;
    readonly string directory;
    readonly DesignRenderer renderer = new();
    readonly PictureBox canvas = new() { Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.FromArgb(18, 19, 24), AllowDrop = true };
    readonly ListBox elements = new() { Dock = DockStyle.Fill, BackColor = Color.FromArgb(32,34,42), ForeColor = Color.White, BorderStyle = BorderStyle.None };
    readonly PropertyGrid properties = new() { Dock = DockStyle.Fill, HelpVisible = true, ToolbarVisible = false, ViewBackColor = Color.FromArgb(32,34,42), ViewForeColor = Color.White, HelpBackColor = Color.FromArgb(24,25,31), HelpForeColor = Color.White, LineColor = Color.FromArgb(55,58,70) };
    readonly Label status = new() { Dock = DockStyle.Bottom, Height = 42, Padding = new Padding(10), ForeColor = Color.Silver };
    readonly TextBox name = new() { Dock = DockStyle.Top };
    readonly ComboBox fit = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 265 };
    readonly NumericUpDown darken = new() { Minimum=0, Maximum=100, Width=80 };
    readonly System.Windows.Forms.Timer clock = new() { Interval = 1000 };
    readonly List<string> undo = [], redo = [];
    string lastSnapshot;
    string? dragSnapshot;
    Point dragStart, widgetStart;
    bool dragging, changed, refreshing;
    readonly Action requestMonitorStart;
    string? pendingRevision;
    DateTime requestedUtc;
    ScreenWidget? Selected => elements.SelectedItem as ScreenWidget;

    public DesignWindow(string appDirectory, Action? startMonitor = null)
    {
        directory = appDirectory;
        requestMonitorStart = startMonitor ?? (() => Process.Start(new ProcessStartInfo("powershell.exe", "-NoProfile -Command \"Start-ScheduledTask -TaskName TempMonitor -ErrorAction Stop\"") { UseShellExecute=false,CreateNoWindow=true }));
        string draft = Path.Combine(directory, "custom-draft.json");
        string active = Path.Combine(directory, "custom-design.json");
        design = ScreenDesign.CreateDefault();
        try { if (File.Exists(draft)) design = DesignFiles.Load(draft); else if (File.Exists(active)) design = DesignFiles.Load(active); }
        catch (Exception ex) when (ex is IOException or System.Text.Json.JsonException or InvalidDataException or UnauthorizedAccessException) { status.Text = "Opgeslagen ontwerp kon niet worden geopend: " + ex.Message; }
        lastSnapshot = DesignFiles.Serialize(design);
        Text = "Qscreen — Ontwerpstudio"; StartPosition = FormStartPosition.CenterParent;
        Size = new Size(1220, 820); MinimumSize = new Size(1000, 700);
        BackColor = Color.FromArgb(24,25,31); ForeColor = Color.White; Font = new Font("Segoe UI", 10);
        var bar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 100, Padding = new Padding(8), BackColor = Color.FromArgb(32,34,42), WrapContents = true };
        Button Button(string title, Action action)
        {
            var b = new Button { Text = title, AutoSize = true, Height = 34, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(40,42,51), ForeColor = Color.White, Margin = new Padding(3) };
            b.Click += (_, _) => Safe(action); bar.Controls.Add(b); return b;
        }
        Button("Nieuw", () => { Remember(); design = ScreenDesign.CreateDefault(); Commit(); RefreshList(); });
        Button("Openen…", Open);
        Button("Opslaan", SaveDraft);
        Button("Exporteren…", Export);
        Button("Ongedaan", Undo);
        Button("Opnieuw", Redo);
        Button("Achtergrond…", ImportBackground);
        Button("Geen afbeelding", () => Change(() => design.BackgroundPng = null));
        Button("Achtergrondkleur", () => { using var d = new ColorDialog { Color = Color.FromArgb(design.BackgroundArgb) }; if(d.ShowDialog(this)==DialogResult.OK) Change(() => design.BackgroundArgb=d.Color.ToArgb()); });
        Button("Lettertype…", ChooseFont);
        Button("Toepassen op scherm", Apply).BackColor = Color.FromArgb(165,35,43);
        Button("Firmware bijwerken…",()=>Process.Start(new ProcessStartInfo("powershell.exe","-NoProfile -STA -ExecutionPolicy Bypass -WindowStyle Hidden -File \""+Path.Combine(directory,"flash-firmware.ps1")+"\""){UseShellExecute=true,WindowStyle=ProcessWindowStyle.Hidden}));

        var left = new Panel { Dock = DockStyle.Left, Width = 228, Padding = new Padding(12) };
        var nameLabel = new Label { Text = "ONTWERPNAAM", Dock = DockStyle.Top, Height = 27 };
        name.Text = design.Name;
        name.Leave += (_, _) => { if(name.Text != design.Name) Change(() => design.Name = name.Text); };
        var add = new ComboBox { Dock = DockStyle.Top, DropDownStyle = ComboBoxStyle.DropDownList, Height = 32 };
        add.FormattingEnabled=true;
        add.Format+=(_,e)=>{if(e.ListItem is WidgetKind kind)e.Value=WidgetLabels.Title(kind);};
        add.Items.AddRange(Enum.GetValues<WidgetKind>().Cast<object>().ToArray()); add.SelectedIndex = 0;
        var addButton = new Button { Text = "+ Element toevoegen", Dock = DockStyle.Top, Height = 38, FlatStyle = FlatStyle.Flat };
        addButton.Click += (_, _) => Safe(() => AddWidget((WidgetKind)add.SelectedItem!));
        var layerBar = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 80 };
        foreach (var (caption, action) in new (string,Action)[] { ("Verwijder",Delete), ("Kopieer",Duplicate), ("Naar voren",()=>MoveLayer(1)), ("Naar achter",()=>MoveLayer(-1)) })
        {
            var b=new Button { Text=caption, Width=96, Height=32, FlatStyle=FlatStyle.Flat }; b.Click+=(_,_)=>Safe(action); layerBar.Controls.Add(b);
        }
        left.Controls.Add(elements); left.Controls.Add(layerBar); left.Controls.Add(addButton); left.Controls.Add(add); left.Controls.Add(name); left.Controls.Add(nameLabel);
        var right = new Panel { Dock = DockStyle.Right, Width = 300, Padding = new Padding(8) };
        var hint = new Label { Text = "EIGENSCHAPPEN\nKlik op een element en versleep het.\nKleur, maat, opmaak en lettertype hieronder.", Dock = DockStyle.Top, Height = 90 };
        var backgroundOptions = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 115, FlowDirection = FlowDirection.TopDown };
        backgroundOptions.Controls.Add(new Label { Text = "Achtergrond schalen", AutoSize = true });
        fit.Items.AddRange(Enum.GetNames<BackgroundFit>()); fit.SelectedItem = design.Fit.ToString();
        fit.SelectedIndexChanged += (_,_) => Change(() => design.Fit = Enum.Parse<BackgroundFit>((string)fit.SelectedItem!));
        darken.Value=design.Darken;
        darken.ValueChanged += (_,_)=>Change(()=>design.Darken=(int)darken.Value);
        backgroundOptions.Controls.Add(fit); backgroundOptions.Controls.Add(new Label { Text="Verdonkeren (%)", AutoSize=true }); backgroundOptions.Controls.Add(darken);
        right.Controls.Add(properties); right.Controls.Add(backgroundOptions); right.Controls.Add(hint);
        var center = new Panel { Dock=DockStyle.Fill, Padding=new Padding(14) };
        var note = new Label { Text="412 × 412 · rond scherm · voorbeeld met testwaarden\nDe pc tekent je ontwerp; datum, tijd en meetwaarden worden elke seconde bijgewerkt.", Dock=DockStyle.Bottom, Height=50, TextAlign=ContentAlignment.MiddleCenter };
        center.Controls.Add(canvas); center.Controls.Add(note);
        Controls.Add(center); Controls.Add(right); Controls.Add(left); Controls.Add(status); Controls.Add(bar);
        elements.SelectedIndexChanged += (_,_) => { properties.SelectedObject=Selected; Render(); };
        properties.PropertyValueChanged += (_,_) => Safe(() => { int i=elements.SelectedIndex; PushUndo(lastSnapshot); Commit(); RefreshList(null,i); });
        canvas.MouseDown += MouseDownCanvas; canvas.MouseMove += MouseMoveCanvas; canvas.MouseUp += MouseUpCanvas;
        canvas.Paint += (_,e) => { if(Selected is { } w){var r=ToCanvas(new RectangleF(w.X,w.Y,w.Width,w.Height));using var pen=new Pen(Color.FromArgb(230,70,90),2){DashStyle=System.Drawing.Drawing2D.DashStyle.Dash};e.Graphics.DrawRectangle(pen,r.X,r.Y,r.Width,r.Height);} };
        canvas.DragEnter += (_,e) => e.Effect=e.Data?.GetDataPresent(DataFormats.FileDrop)==true?DragDropEffects.Copy:DragDropEffects.None;
        canvas.DragDrop += (_,e) => Safe(()=>{ if(e.Data?.GetData(DataFormats.FileDrop) is string[] files && files.Length>0) SetBackground(files[0]); });
        clock.Tick += (_,_) => { Render(); if(pendingRevision!=null){var delivery=ScreenDelivery.Read(directory);status.Text=ScreenDelivery.Describe(delivery,pendingRevision,requestedUtc,DateTime.UtcNow);status.ForeColor=status.Text.StartsWith("Ontwerp ontvangen",StringComparison.Ordinal)?Color.LightGreen:Color.Orange;} }; clock.Start();
        FormClosing += (_,e) => { if(changed){var answer=MessageBox.Show(this,"Wijzigingen opslaan als concept?","Qscreen",MessageBoxButtons.YesNoCancel,MessageBoxIcon.Question);if(answer==DialogResult.Cancel)e.Cancel=true;else if(answer==DialogResult.Yes){try{SaveDraft();}catch(Exception ex){MessageBox.Show(this,ex.Message);e.Cancel=true;}}} };
        FormClosed += (_,_) => { clock.Dispose(); renderer.Dispose(); canvas.Image?.Dispose(); };
        RefreshList();
    }

    void Safe(Action action) { try { action(); } catch(Exception ex) { pendingRevision=null;status.ForeColor=Color.Orange;status.Text=ex.Message; } }
    void Remember() => PushUndo(DesignFiles.Serialize(design));
    void PushUndo(string state) { undo.Add(state); if(undo.Count>20)undo.RemoveAt(0); redo.Clear(); }
    void Commit()
    {
        design=DesignFiles.Parse(DesignFiles.Serialize(design));
        lastSnapshot=DesignFiles.Serialize(design); changed=true; Render();
        pendingRevision=null;status.ForeColor=Color.Silver;status.Text="Wijzigingen nog niet toegepast.";
    }
    void Change(Action action)
    {
        if(refreshing)return;
        int selection=elements.SelectedIndex; Remember(); action(); Commit(); RefreshList(null,selection);
    }
    void RefreshList(ScreenWidget? selection=null,int index=-1)
    {
        refreshing=true;
        if(index<0)index=selection==null?elements.SelectedIndex:design.Widgets.IndexOf(selection);
        elements.Items.Clear(); elements.Items.AddRange(design.Widgets.Cast<object>().ToArray());
        if(elements.Items.Count>0)elements.SelectedIndex=Math.Clamp(index,0,elements.Items.Count-1);
        else properties.SelectedObject=null;
        name.Text=design.Name;fit.SelectedItem=design.Fit.ToString();darken.Value=design.Darken; refreshing=false; Render();
    }
    void Render()
    {
        try { var next=renderer.Render(design,ScreenReadings.Demo); var previous=canvas.Image;canvas.Image=next;previous?.Dispose();canvas.Invalidate(); }
        catch(Exception ex) { status.Text="Voorbeeld: "+ex.Message; }
    }
    void AddWidget(WidgetKind kind)
    {
        if(design.Widgets.Count>=64)throw new InvalidOperationException("Maximaal 64 elementen per ontwerp.");
        Remember();
        var w=new ScreenWidget { Name=WidgetLabels.Title(kind),Kind=kind,Format=kind switch {WidgetKind.Text=>"Mijn tekst",WidgetKind.Time=>"HH:mm:ss",WidgetKind.Date=>"dd-MM-yyyy",_=>"{value}"} };
        if(kind is WidgetKind.Rectangle or WidgetKind.Ellipse){w.Width=120;w.Height=80;w.Foreground=Color.FromArgb(60,190,255);}
        if(kind==WidgetKind.ProgressBar){w.Height=12;w.Foreground=Color.FromArgb(60,190,255);}
        design.Widgets.Add(w);Commit();RefreshList(null,design.Widgets.Count-1);
    }
    void Delete(){if(Selected is {} w){Remember();design.Widgets.Remove(w);Commit();RefreshList();}}
    void Duplicate(){if(Selected is {} w){if(design.Widgets.Count>=64)return;Remember();var copy=System.Text.Json.JsonSerializer.Deserialize<ScreenWidget>(System.Text.Json.JsonSerializer.Serialize(w,DesignFiles.Json),DesignFiles.Json)!;copy.Name+=" kopie";copy.X+=10;copy.Y+=10;design.Widgets.Add(copy);Commit();RefreshList(null,design.Widgets.Count-1);}}
    void MoveLayer(int delta){int i=elements.SelectedIndex,j=i+delta;if(i<0||j<0||j>=design.Widgets.Count)return;Remember();(design.Widgets[i],design.Widgets[j])=(design.Widgets[j],design.Widgets[i]);Commit();RefreshList(null,j);}
    void Undo(){if(undo.Count==0)return;redo.Add(DesignFiles.Serialize(design));design=DesignFiles.Parse(undo[^1]);undo.RemoveAt(undo.Count-1);lastSnapshot=DesignFiles.Serialize(design);changed=true;RefreshList();}
    void Redo(){if(redo.Count==0)return;undo.Add(DesignFiles.Serialize(design));design=DesignFiles.Parse(redo[^1]);redo.RemoveAt(redo.Count-1);lastSnapshot=DesignFiles.Serialize(design);changed=true;RefreshList();}
    void SaveDraft(){design.Name=name.Text;DesignFiles.Save(Path.Combine(directory,"custom-draft.json"),design);changed=false;status.Text="Concept opgeslagen. Klik op Toepassen op scherm om dit ontwerp te gebruiken.";}
    void Apply()
    {
        design.Name=name.Text; DesignFiles.Apply(directory,design); SaveDraft();
        pendingRevision=ScreenDelivery.RevisionOf(directory); requestedUtc=DateTime.UtcNow;
        requestMonitorStart();
        status.ForeColor=Color.Orange;
        status.Text=ScreenDelivery.Describe(null,pendingRevision,requestedUtc,requestedUtc);
    }
    void Open(){using var d=new OpenFileDialog { Filter="Qscreen ontwerp (*.qscreen;*.json)|*.qscreen;*.json" };if(d.ShowDialog(this)!=DialogResult.OK)return;var loaded=DesignFiles.Load(d.FileName);Remember();design=loaded;Commit();RefreshList();status.Text="Ontwerp geopend; nog niet toegepast.";}
    void Export(){using var d=new SaveFileDialog {Filter="Qscreen ontwerp (*.qscreen)|*.qscreen",FileName="Mijn ontwerp.qscreen"};if(d.ShowDialog(this)==DialogResult.OK){design.Name=name.Text;DesignFiles.Save(d.FileName,design);status.Text="Ontwerp inclusief achtergrond geëxporteerd.";}}
    void ImportBackground(){using var d=new OpenFileDialog{Filter="Afbeeldingen|*.png;*.jpg;*.jpeg;*.bmp"};if(d.ShowDialog(this)==DialogResult.OK)SetBackground(d.FileName);}
    void SetBackground(string path)
    {
        using var image=Image.FromFile(path);
        if((long)image.Width*image.Height>40_000_000)throw new InvalidDataException("Afbeelding te groot (maximaal 40 megapixels).");
        float scale=Math.Min(1,1024f/Math.Max(image.Width,image.Height));
        using var reduced=new Bitmap(Math.Max(1,(int)(image.Width*scale)),Math.Max(1,(int)(image.Height*scale)));
        using(var g=Graphics.FromImage(reduced)){g.InterpolationMode=System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;g.DrawImage(image,0,0,reduced.Width,reduced.Height);}
        using var stream=new MemoryStream();reduced.Save(stream,ImageFormat.Png);string encoded=Convert.ToBase64String(stream.ToArray());
        Change(()=>design.BackgroundPng=encoded);status.Text="Achtergrond geïmporteerd en in je ontwerp opgenomen.";
    }
    void ChooseFont(){if(Selected is not {} w)return;using var d=new FontDialog{Font=new Font(w.FontFamily,w.FontSize,FontStyle.Regular,GraphicsUnit.Pixel),ShowColor=true,Color=w.Foreground};if(d.ShowDialog(this)==DialogResult.OK)Change(()=>{w.FontFamily=d.Font.FontFamily.Name;w.FontSize=d.Font.SizeInPoints*96/72;w.Bold=d.Font.Bold;w.Italic=d.Font.Italic;w.Foreground=d.Color;});}
    RectangleF ScreenBounds { get {float size=Math.Min(canvas.ClientSize.Width,canvas.ClientSize.Height);return new((canvas.ClientSize.Width-size)/2,(canvas.ClientSize.Height-size)/2,size,size);} }
    Point ScreenPoint(Point p){var r=ScreenBounds;return new((int)((p.X-r.X)*412/Math.Max(1,r.Width)),(int)((p.Y-r.Y)*412/Math.Max(1,r.Height)));}
    RectangleF ToCanvas(RectangleF rect){var r=ScreenBounds;float scale=r.Width/412;return new(r.X+rect.X*scale,r.Y+rect.Y*scale,rect.Width*scale,rect.Height*scale);}
    void MouseDownCanvas(object? sender,MouseEventArgs e)
    {
        if(e.Button!=MouseButtons.Left)return;var p=ScreenPoint(e.Location);
        var w=design.Widgets.LastOrDefault(w=>w.Visible&&new Rectangle(w.X,w.Y,w.Width,w.Height).Contains(p));
        if(w==null)return;elements.SelectedItem=w;dragStart=p;widgetStart=new(w.X,w.Y);dragSnapshot=DesignFiles.Serialize(design);dragging=true;canvas.Capture=true;
    }
    void MouseMoveCanvas(object? sender,MouseEventArgs e){if(!dragging||Selected is not {} w)return;var p=ScreenPoint(e.Location);w.X=Math.Clamp(widgetStart.X+p.X-dragStart.X,-412,412);w.Y=Math.Clamp(widgetStart.Y+p.Y-dragStart.Y,-412,412);properties.Refresh();Render();}
    void MouseUpCanvas(object? sender,MouseEventArgs e){if(!dragging)return;dragging=false;canvas.Capture=false;if(dragSnapshot!=null&&dragSnapshot!=DesignFiles.Serialize(design)){int index=elements.SelectedIndex;PushUndo(dragSnapshot);Commit();RefreshList(null,index);}dragSnapshot=null;}
    protected override bool ProcessCmdKey(ref Message msg,Keys keyData)
    {
        if(keyData==(Keys.Control|Keys.S)){Safe(SaveDraft);return true;}
        if(keyData==(Keys.Control|Keys.Z)){Safe(Undo);return true;}
        if(keyData==(Keys.Control|Keys.Y)){Safe(Redo);return true;}
        if(keyData==Keys.Delete&&elements.Focused){Safe(Delete);return true;}
        return base.ProcessCmdKey(ref msg,keyData);
    }
    protected override void OnHandleCreated(EventArgs e){base.OnHandleCreated(e);int dark=1;DwmSetWindowAttribute(Handle,20,ref dark,sizeof(int));}
}

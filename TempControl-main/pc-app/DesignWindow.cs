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
    readonly PictureBox canvas = new StudioPreview() { Dock = DockStyle.Fill, AllowDrop = true };
    readonly ListBox elements = new() { Dock = DockStyle.Fill, BackColor = Color.FromArgb(32,34,42), ForeColor = Color.White, BorderStyle = BorderStyle.None };
    readonly PropertyGrid properties = new() { Dock = DockStyle.Fill, HelpVisible = true, ToolbarVisible = false, ViewBackColor = Color.FromArgb(32,34,42), ViewForeColor = Color.White, HelpBackColor = Color.FromArgb(24,25,31), HelpForeColor = Color.White, LineColor = Color.FromArgb(55,58,70) };
    readonly Label status = new() { Dock = DockStyle.Bottom, Height = 42, Padding = new Padding(10), ForeColor = Color.Silver };
    readonly TextBox name = new() { Dock = DockStyle.Top };
    readonly ComboBox fit = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 265 };
    readonly NumericUpDown darken = new() { Minimum=0, Maximum=100, Width=80 };
    readonly ComboBox animation=new(){DropDownStyle=ComboBoxStyle.DropDownList,Dock=DockStyle.Top};
    readonly NumericUpDown animationSpeed=new(){Minimum=.1m,Maximum=5,DecimalPlaces=1,Increment=.1m,Width=80};
    readonly CheckBox animationsEnabled=new(){Text="Animaties inschakelen",AutoSize=true};
    readonly System.Windows.Forms.Timer clock = new() { Interval = 33 };
    bool renderQueued;
    long lastClockUpdate;
    readonly List<DesignSnapshot> undo = [], redo = [];
    DesignSnapshot lastSnapshot;
    DesignSnapshot? dragSnapshot;
    Point dragStart, widgetStart;
    bool dragging, changed, refreshing;
    readonly Action requestMonitorStart;
    readonly Action<ScreenDesign>? savePage;
    string? pendingRevision;
    DateTime requestedUtc;
    ScreenWidget? Selected => elements.SelectedItem as ScreenWidget;

    public DesignWindow(string appDirectory, Action? startMonitor = null,ScreenDesign? initialDesign=null,Action<ScreenDesign>? savePage=null)
    {
        directory = appDirectory;
        this.savePage=savePage;
        requestMonitorStart = startMonitor ?? (() => Process.Start(new ProcessStartInfo("powershell.exe", "-NoProfile -Command \"Start-ScheduledTask -TaskName TempMonitor -ErrorAction Stop\"") { UseShellExecute=false,CreateNoWindow=true }));
        string draft = Path.Combine(directory, "custom-draft.json");
        string active = Path.Combine(directory, "custom-design.json");
        design = ScreenDesign.CreateDefault();
        try { if(initialDesign!=null)design=DesignSnapshot.Capture(initialDesign).Restore();else if (File.Exists(draft)) design = DesignFiles.Load(draft); else if (File.Exists(active)) design = DesignFiles.Load(active); }
        catch (Exception ex) when (ex is IOException or System.Text.Json.JsonException or InvalidDataException or UnauthorizedAccessException) { status.Text = "Opgeslagen ontwerp kon niet worden geopend: " + ex.Message; }
        lastSnapshot = DesignSnapshot.Capture(design);
        Text="Qscreen — Ontwerpstudio";StartPosition=FormStartPosition.CenterParent;
        Size=new Size(1360,900);MinimumSize=new Size(1100,760);
        BackColor=StudioTheme.Background;ForeColor=StudioTheme.Text;Font=new Font("Segoe UI",10);
        string icon=Path.Combine(directory,"branding","qprints.ico");if(File.Exists(icon))Icon=new Icon(icon);
        var top=new Panel{Dock=DockStyle.Top,Height=80,Padding=new Padding(20,16,20,14),BackColor=StudioTheme.Surface};
        var brand=StudioTheme.Label("Qscreen / Studio",20,true,46);brand.Dock=DockStyle.Left;brand.Width=224;
        var files=new FlowLayoutPanel{Dock=DockStyle.Fill,WrapContents=false,Padding=new Padding(0,0,0,0)};
        Button ActionButton(string title,Action action,bool primary=false,int width=90)
        {
            var button=StudioTheme.Button(title,primary);button.Width=width;button.Height=40;button.Font=new Font("Segoe UI",9,FontStyle.Bold);
            button.Click+=(_,_)=>Safe(action);return button;
        }
        files.Controls.Add(ActionButton("Nieuw",()=>{Remember();design=ScreenDesign.CreateDefault();Commit();RefreshList();},false,65));
        files.Controls.Add(ActionButton("Openen",Open,false,72));files.Controls.Add(ActionButton("Opslaan",SaveDraft,false,78));files.Controls.Add(ActionButton("Export",Export,false,70));
        files.Controls.Add(ActionButton("↶",Undo,false,40));files.Controls.Add(ActionButton("↷",Redo,false,40));
        var apply=ActionButton(savePage!=null?"Wijzigingen bewaren":"Toepassen op scherm",Apply,true,190);apply.Dock=DockStyle.Right;
        top.Controls.Add(files);top.Controls.Add(apply);top.Controls.Add(brand);
        var tools=new FlowLayoutPanel{Dock=DockStyle.Top,Height=62,Padding=new Padding(20,8,20,8),BackColor=StudioTheme.Background,WrapContents=false};
        tools.Controls.Add(ActionButton("Afbeelding kiezen",ImportBackground,false,155));
        tools.Controls.Add(ActionButton("Afbeelding wissen",()=>Change(()=>design.BackgroundPng=null),false,150));
        tools.Controls.Add(ActionButton("Achtergrondkleur",()=>{using var d=new ColorDialog{Color=Color.FromArgb(design.BackgroundArgb)};if(d.ShowDialog(this)==DialogResult.OK)Change(()=>design.BackgroundArgb=d.Color.ToArgb());},false,150));
        tools.Controls.Add(ActionButton("Lettertype",ChooseFont,false,110));
        tools.Controls.Add(ActionButton("Kleurregels",()=>{if(Selected==null)throw new InvalidOperationException("Selecteer een element.");using var rules=new ColorRulesWindow(Selected);if(rules.ShowDialog(this)==DialogResult.OK)Change(()=>Selected.ColorRules=rules.Rules);},false,105));
        tools.Controls.Add(ActionButton("Als template",()=>{using var dialog=new SaveFileDialog{Filter="Qscreen templatepakket|*.qscreenpack",FileName="Mijn-template.qscreenpack"};if(dialog.ShowDialog(this)!=DialogResult.OK)return;var manifest=new TemplateManifest{Name=design.Name,Author="Lokaal",TemplateId=Guid.NewGuid().ToString("N"),MinimumAppVersion="2.1.0",Animated=DesignRenderer.IsAnimated(design),Description="Aangepast ontwerp"};QscreenPackage.Export(dialog.FileName,design,manifest);new LocalTemplateLibrary(directory).Add(design,TemplateCategory.Gaming,manifest);status.Text="Nieuw template geëxporteerd en in je lokale bibliotheek bewaard.";},false,110));
        tools.Controls.Add(ActionButton("Pagina's",()=>{using var manager=new PagesWindow(directory,design);if(manager.ShowDialog(this)==DialogResult.OK)Change(()=>design=manager.Project);},false,92));
        tools.Controls.Add(ActionButton("Aanraakzones",()=>{using var zones=new TouchZonesWindow(design.TouchZones);if(zones.ShowDialog(this)==DialogResult.OK)Change(()=>design.TouchZones=zones.Zones);},false,120));
        tools.Controls.Add(ActionButton("Firmware bijwerken",()=>Process.Start(new ProcessStartInfo("powershell.exe","-NoProfile -STA -ExecutionPolicy Bypass -WindowStyle Hidden -File \""+Path.Combine(directory,"flash-firmware.ps1")+"\""){UseShellExecute=true,WindowStyle=ProcessWindowStyle.Hidden}),false,165));
        tools.WrapContents=true;tools.AutoSize=true;tools.AutoSizeMode=AutoSizeMode.GrowAndShrink;tools.MinimumSize=new Size(0,62);

        var left=new StudioCard{Dock=DockStyle.Left,Width=242,Padding=new Padding(16)};
        var nameLabel=StudioTheme.Label("MIJN ONTWERP",9,true,30);nameLabel.ForeColor=StudioTheme.Accent;
        name.Text=design.Name;StudioTheme.Input(name);
        name.Leave+=(_,_)=>{if(name.Text!=design.Name)Change(()=>design.Name=name.Text);};
        var gap=new Panel{Dock=DockStyle.Top,Height=20};
        var elementLabel=StudioTheme.Label("ELEMENTEN",9,true,32);
        var add=new ComboBox{Dock=DockStyle.Top,DropDownStyle=ComboBoxStyle.DropDownList,Height=32};StudioTheme.Input(add);
        add.FormattingEnabled=true;add.Format+=(_,e)=>{if(e.ListItem is WidgetKind kind)e.Value=WidgetLabels.Title(kind);};
        add.Items.AddRange(Enum.GetValues<WidgetKind>().Cast<object>().ToArray());add.SelectedIndex=0;
        var addButton=ActionButton("+  Toevoegen",()=>AddWidget((WidgetKind)add.SelectedItem!),false,200);addButton.Dock=DockStyle.Top;addButton.Height=40;
        elements.BackColor=StudioTheme.Surface;elements.ForeColor=StudioTheme.Text;elements.DrawMode=DrawMode.OwnerDrawFixed;elements.ItemHeight=48;elements.IntegralHeight=false;
        elements.DrawItem+=(_,e)=>
        {
            if(e.Index<0)return;var widget=(ScreenWidget)elements.Items[e.Index];using(var background=new SolidBrush(StudioTheme.Surface))e.Graphics.FillRectangle(background,e.Bounds);
            if((e.State&DrawItemState.Selected)!=0){using var fill=new SolidBrush(StudioTheme.Raised);using var shape=StudioTheme.Rounded(new RectangleF(e.Bounds.X,e.Bounds.Y+2,e.Bounds.Width-3,e.Bounds.Height-4),8);e.Graphics.FillPath(fill,shape);}
            using var titleFont=new Font("Segoe UI",10,FontStyle.Bold);using var subFont=new Font("Segoe UI",8);
            TextRenderer.DrawText(e.Graphics,widget.Name,titleFont,new Rectangle(e.Bounds.X+12,e.Bounds.Y+5,e.Bounds.Width-20,20),widget.Visible?StudioTheme.Text:StudioTheme.Muted,TextFormatFlags.EndEllipsis);
            TextRenderer.DrawText(e.Graphics,WidgetLabels.Title(widget.Kind),subFont,new Rectangle(e.Bounds.X+12,e.Bounds.Y+26,e.Bounds.Width-20,18),StudioTheme.Muted,TextFormatFlags.EndEllipsis);
        };
        var layerBar=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=98,WrapContents=true};
        foreach(var (caption,action) in new (string,Action)[]{("Verwijder",Delete),("Kopieer",Duplicate),("Naar voren",()=>MoveLayer(1)),("Naar achter",()=>MoveLayer(-1))})layerBar.Controls.Add(ActionButton(caption,action,false,94));
        left.Controls.Add(elements);left.Controls.Add(layerBar);left.Controls.Add(addButton);left.Controls.Add(add);left.Controls.Add(elementLabel);left.Controls.Add(gap);left.Controls.Add(name);left.Controls.Add(nameLabel);
        var right=new StudioCard{Dock=DockStyle.Right,Width=300,Padding=new Padding(16)};
        var hint=StudioTheme.Label("EIGENSCHAPPEN",9,true,38);
        properties.ViewBackColor=StudioTheme.Surface;properties.ViewForeColor=StudioTheme.Text;properties.HelpBackColor=StudioTheme.Surface;properties.HelpForeColor=StudioTheme.Muted;properties.LineColor=StudioTheme.Border;properties.CategoryForeColor=StudioTheme.Text;properties.CategorySplitterColor=StudioTheme.Raised;properties.Font=new Font("Segoe UI",10);
        var backgroundOptions=new Panel{Dock=DockStyle.Bottom,Height=220,Padding=new Padding(0,18,0,0)};
        var backgroundLabel=StudioTheme.Label("ACHTERGROND",9,true,32);
        var fitLabel=StudioTheme.Label("Plaatsing",9,false,25);fit.Dock=DockStyle.Top;StudioTheme.Input(fit);
        fit.FormattingEnabled=true;fit.Format+=(_,e)=>{e.Value=e.ListItem?.ToString() switch{"Cover"=>"Vullend (bijsnijden)","Contain"=>"Volledig zichtbaar","Stretch"=>"Uitrekken",_=>e.ListItem};};
        fit.Items.AddRange(Enum.GetNames<BackgroundFit>());fit.SelectedItem=design.Fit.ToString();
        fit.SelectedIndexChanged+=(_,_)=>Change(()=>design.Fit=Enum.Parse<BackgroundFit>((string)fit.SelectedItem!));
        var darkLabel=StudioTheme.Label("Verdonkeren (%)",9,false,30);darken.Dock=DockStyle.Top;darken.BackColor=StudioTheme.Raised;darken.ForeColor=StudioTheme.Text;darken.BorderStyle=BorderStyle.FixedSingle;darken.Value=design.Darken;
        darken.ValueChanged+=(_,_)=>Change(()=>design.Darken=(int)darken.Value);
        StudioTheme.Input(animation);animation.Items.AddRange(Enum.GetNames<PageAnimation>());animation.SelectedItem=design.Animation.ToString();animation.SelectedIndexChanged+=(_,_)=>Change(()=>design.Animation=Enum.Parse<PageAnimation>((string)animation.SelectedItem!));
        var animationLabel=StudioTheme.Label("Animatie",9,false,26);
        animationSpeed.Value=(decimal)design.AnimationSpeed;animationSpeed.ValueChanged+=(_,_)=>Change(()=>design.AnimationSpeed=(float)animationSpeed.Value);
        animationsEnabled.Checked=design.AnimationsEnabled;animationsEnabled.CheckedChanged+=(_,_)=>Change(()=>design.AnimationsEnabled=animationsEnabled.Checked);
        var animationControls=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=56};animationControls.Controls.AddRange([animationsEnabled,animationSpeed]);backgroundOptions.Controls.Add(animationControls);backgroundOptions.Height+=56;
        backgroundOptions.Controls.Add(animation);backgroundOptions.Controls.Add(animationLabel);backgroundOptions.Controls.Add(darken);backgroundOptions.Controls.Add(darkLabel);backgroundOptions.Controls.Add(fit);backgroundOptions.Controls.Add(fitLabel);backgroundOptions.Controls.Add(backgroundLabel);
        right.Controls.Add(properties);right.Controls.Add(backgroundOptions);right.Controls.Add(hint);
        var center=new Panel{Dock=DockStyle.Fill,Padding=new Padding(14,0,14,0)};
        var stage=new StudioCard{Dock=DockStyle.Fill,Padding=new Padding(12)};
        var canvasLabel=StudioTheme.Label("CANVAS                                         412 × 412",8,true,34);canvasLabel.Padding=new Padding(10,0,0,0);
        var note=StudioTheme.Label("Sleep om te verplaatsen · Ctrl+Z om te herstellen\nVoorbeeld met testwaarden",9,false,54);note.Dock=DockStyle.Bottom;note.TextAlign=ContentAlignment.MiddleCenter;
        stage.Controls.Add(canvas);stage.Controls.Add(note);stage.Controls.Add(canvasLabel);center.Controls.Add(stage);
        var body=new Panel{Dock=DockStyle.Fill,Padding=new Padding(20,0,20,16)};
        body.Controls.Add(center);body.Controls.Add(right);body.Controls.Add(left);
        status.Dock=DockStyle.Bottom;status.Height=56;status.Padding=new Padding(22,12,22,8);status.BackColor=StudioTheme.Surface;status.ForeColor=StudioTheme.Muted;
        Controls.Add(body);Controls.Add(status);Controls.Add(tools);Controls.Add(top);
        elements.SelectedIndexChanged += (_,_) => { properties.SelectedObject=Selected is {} widget?new WidgetInspector(widget):null; canvas.Invalidate(); };
        properties.PropertyValueChanged += (_,_) => Safe(() => { int i=elements.SelectedIndex; PushUndo(lastSnapshot); Commit(); RefreshList(null,i); });
        canvas.MouseDown += MouseDownCanvas; canvas.MouseMove += MouseMoveCanvas; canvas.MouseUp += MouseUpCanvas;
        ((StudioPreview)canvas).SelectionPainter = e => { if(Selected is { } w){var r=ToCanvas(new RectangleF(w.X,w.Y,w.Width,w.Height));using var pen=new Pen(Color.FromArgb(230,70,90),2){DashStyle=System.Drawing.Drawing2D.DashStyle.Dash};e.Graphics.DrawRectangle(pen,r.X,r.Y,r.Width,r.Height);} };
        canvas.DragEnter += (_,e) => e.Effect=e.Data?.GetDataPresent(DataFormats.FileDrop)==true?DragDropEffects.Copy:DragDropEffects.None;
        canvas.DragDrop += (_,e) => Safe(()=>{ if(e.Data?.GetData(DataFormats.FileDrop) is string[] files && files.Length>0) SetBackground(files[0]); });
        clock.Tick += (_,_) =>
        {
            bool second=Environment.TickCount64-lastClockUpdate>=1000;
            if(second)lastClockUpdate=Environment.TickCount64;
            if(WindowState!=FormWindowState.Minimized&&Visible&&(renderQueued||renderer.HasPendingMotion||DesignRenderer.IsAnimated(design)||design.AnimationsEnabled&&renderer.HasAnimatedBackground||(second&&design.Widgets.Any(w=>w.Visible&&w.Kind is WidgetKind.Time or WidgetKind.Date))))Render();
            if(second&&pendingRevision!=null){var delivery=ScreenDelivery.Read(directory);status.Text=ScreenDelivery.Describe(delivery,pendingRevision,requestedUtc,DateTime.UtcNow);status.ForeColor=status.Text.StartsWith("Ontwerp ontvangen",StringComparison.Ordinal)?StudioTheme.Green:Color.Orange;}
        };clock.Start();
        FormClosing += (_,e) => { if(changed){var answer=MessageBox.Show(this,"Wijzigingen opslaan als concept?","Qscreen",MessageBoxButtons.YesNoCancel,MessageBoxIcon.Question);if(answer==DialogResult.Cancel)e.Cancel=true;else if(answer==DialogResult.Yes){try{SaveDraft();}catch(Exception ex){MessageBox.Show(this,ex.Message);e.Cancel=true;}}} };
        FormClosed += (_,_) => { clock.Dispose(); renderer.Dispose(); canvas.Image?.Dispose(); };
        RefreshList();
        StudioTheme.FitWindow(this,new Size(1360,900),new Size(1000,700));
    }

    void Safe(Action action) { try { action(); } catch(Exception ex) { pendingRevision=null;status.ForeColor=Color.Orange;status.Text=ex.Message; } }
    void Remember() => PushUndo(DesignSnapshot.Capture(design));
    void PushUndo(DesignSnapshot state) { undo.Add(state); if(undo.Count>20)undo.RemoveAt(0); redo.Clear(); }
    void Commit()
    {
        design=DesignSnapshot.Capture(design).Restore();
        lastSnapshot=DesignSnapshot.Capture(design); changed=true; Render();
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
        name.Text=design.Name;fit.SelectedItem=design.Fit.ToString();darken.Value=design.Darken;animation.SelectedItem=design.Animation.ToString();animationSpeed.Value=(decimal)design.AnimationSpeed;animationsEnabled.Checked=design.AnimationsEnabled; refreshing=false; Render();
    }
    void Render()
    {
        renderQueued=false;
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
        if(kind==WidgetKind.HistoryGraph){w.Height=90;w.Width=280;w.X=66;w.Y=230;w.Foreground=Color.Cyan;}
        if(kind is WidgetKind.CircularGauge or WidgetKind.AnalogClock){w.X=106;w.Y=106;w.Width=200;w.Height=200;w.Foreground=Color.DeepSkyBlue;}
        if(kind==WidgetKind.Panel){w.Width=180;w.Height=90;w.StrokeWidth=1;w.Foreground=Color.DeepSkyBlue;}
        design.Widgets.Add(w);Commit();RefreshList(null,design.Widgets.Count-1);
    }
    void Delete(){if(Selected is {} w){Remember();design.Widgets.Remove(w);Commit();RefreshList();}}
    void Duplicate(){if(Selected is {} w){if(design.Widgets.Count>=64)return;Remember();var copy=System.Text.Json.JsonSerializer.Deserialize<ScreenWidget>(System.Text.Json.JsonSerializer.Serialize(w,DesignFiles.Json),DesignFiles.Json)!;copy.Name+=" kopie";copy.X+=10;copy.Y+=10;design.Widgets.Add(copy);Commit();RefreshList(null,design.Widgets.Count-1);}}
    void MoveLayer(int delta){int i=elements.SelectedIndex,j=i+delta;if(i<0||j<0||j>=design.Widgets.Count)return;Remember();(design.Widgets[i],design.Widgets[j])=(design.Widgets[j],design.Widgets[i]);Commit();RefreshList(null,j);}
    void Undo(){if(undo.Count==0)return;redo.Add(DesignSnapshot.Capture(design));design=undo[^1].Restore();undo.RemoveAt(undo.Count-1);lastSnapshot=DesignSnapshot.Capture(design);pendingRevision=null;changed=true;RefreshList();}
    void Redo(){if(redo.Count==0)return;undo.Add(DesignSnapshot.Capture(design));design=redo[^1].Restore();redo.RemoveAt(redo.Count-1);lastSnapshot=DesignSnapshot.Capture(design);pendingRevision=null;changed=true;RefreshList();}
    void SaveDraft(){design.Name=name.Text;if(savePage!=null)savePage(design);else DesignFiles.Save(Path.Combine(directory,"custom-draft.json"),design);changed=false;status.Text=savePage!=null?"Pagina/profiel opgeslagen.":"Concept opgeslagen. Klik op Toepassen op scherm om dit ontwerp te gebruiken.";}
    void Apply()
    {
        if(savePage!=null){SaveDraft();return;}
        design.Name=name.Text; DesignFiles.Apply(directory,design); SaveDraft();
        pendingRevision=ScreenDelivery.RevisionOf(directory); requestedUtc=DateTime.UtcNow;
        requestMonitorStart();
        status.ForeColor=Color.Orange;
        status.Text=ScreenDelivery.Describe(null,pendingRevision,requestedUtc,requestedUtc);
    }
    void Open(){using var d=new OpenFileDialog { Filter="Qscreen ontwerp (*.qscreen;*.json)|*.qscreen;*.json" };if(d.ShowDialog(this)!=DialogResult.OK)return;var loaded=DesignFiles.Load(d.FileName);Remember();design=loaded;Commit();RefreshList();status.Text="Ontwerp geopend; nog niet toegepast.";}
    void Export(){using var d=new SaveFileDialog {Filter="Qscreen ontwerp (*.qscreen)|*.qscreen",FileName="Mijn ontwerp.qscreen"};if(d.ShowDialog(this)==DialogResult.OK){design.Name=name.Text;DesignFiles.Save(d.FileName,design);status.Text="Ontwerp inclusief achtergrond geëxporteerd.";}}
    void ImportBackground(){using var d=new OpenFileDialog{Filter="Afbeeldingen|*.png;*.jpg;*.jpeg;*.bmp;*.gif"};if(d.ShowDialog(this)==DialogResult.OK)SetBackground(d.FileName);}
    void SetBackground(string path)
    {
        using var image=Image.FromFile(path);
        if(image.FrameDimensionsList.Contains(FrameDimension.Time.Guid)&&image.GetFrameCount(FrameDimension.Time)>1)
        {
            int frames=image.GetFrameCount(FrameDimension.Time);if(frames>60||(long)image.Width*image.Height*frames>16_000_000||new FileInfo(path).Length>8_000_000)throw new InvalidDataException("GIF te groot: maximaal 60 frames, 16 miljoen pixels en 8 MB.");
            string gif=Convert.ToBase64String(File.ReadAllBytes(path));Change(()=>design.BackgroundPng=gif);status.Text="GIF geïmporteerd. Afspeelsnelheid op het scherm wordt begrensd door USB.";return;
        }
        if((long)image.Width*image.Height>40_000_000)throw new InvalidDataException("Afbeelding te groot (maximaal 40 megapixels).");
        float scale=Math.Min(1,1024f/Math.Max(image.Width,image.Height));
        using var reduced=new Bitmap(Math.Max(1,(int)(image.Width*scale)),Math.Max(1,(int)(image.Height*scale)));
        using(var g=Graphics.FromImage(reduced)){g.InterpolationMode=System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;g.DrawImage(image,0,0,reduced.Width,reduced.Height);}
        using var stream=new MemoryStream();reduced.Save(stream,ImageFormat.Png);string encoded=Convert.ToBase64String(stream.ToArray());
        Change(()=>design.BackgroundPng=encoded);status.Text="Achtergrond geïmporteerd en in je ontwerp opgenomen.";
    }
    void ChooseFont(){if(Selected is not {} w)return;using var d=new FontDialog{Font=new Font(w.FontFamily,w.FontSize,FontStyle.Regular,GraphicsUnit.Pixel),ShowColor=true,Color=w.Foreground};if(d.ShowDialog(this)==DialogResult.OK)Change(()=>{w.FontFamily=d.Font.FontFamily.Name;w.FontSize=d.Font.SizeInPoints*96/72;w.Bold=d.Font.Bold;w.Italic=d.Font.Italic;w.Foreground=d.Color;});}
    RectangleF ScreenBounds=>((StudioPreview)canvas).DisplayBounds;
    Point ScreenPoint(Point p){var r=ScreenBounds;return new((int)((p.X-r.X)*412/Math.Max(1,r.Width)),(int)((p.Y-r.Y)*412/Math.Max(1,r.Height)));}
    RectangleF ToCanvas(RectangleF rect){var r=ScreenBounds;float scale=r.Width/412;return new(r.X+rect.X*scale,r.Y+rect.Y*scale,rect.Width*scale,rect.Height*scale);}
    void MouseDownCanvas(object? sender,MouseEventArgs e)
    {
        if(e.Button!=MouseButtons.Left)return;var p=ScreenPoint(e.Location);
        var w=design.Widgets.LastOrDefault(w=>w.Visible&&new Rectangle(w.X,w.Y,w.Width,w.Height).Contains(p));
        if(w==null)return;elements.SelectedItem=w;dragStart=p;widgetStart=new(w.X,w.Y);dragSnapshot=DesignSnapshot.Capture(design);dragging=true;canvas.Capture=true;
    }
    void MouseMoveCanvas(object? sender,MouseEventArgs e){if(!dragging||Selected is not {} w)return;var p=ScreenPoint(e.Location);w.X=Math.Clamp(widgetStart.X+p.X-dragStart.X,-412,412);w.Y=Math.Clamp(widgetStart.Y+p.Y-dragStart.Y,-412,412);renderQueued=true;}
    void MouseUpCanvas(object? sender,MouseEventArgs e){if(!dragging)return;dragging=false;canvas.Capture=false;if(dragSnapshot!=null&&!dragSnapshot.Equivalent(DesignSnapshot.Capture(design))){int index=elements.SelectedIndex;PushUndo(dragSnapshot);Commit();RefreshList(null,index);}dragSnapshot=null;}
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

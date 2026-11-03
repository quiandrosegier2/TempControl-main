using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

sealed class SettingsWindow : Form
{
    readonly ComboBox layouts=new(){DropDownStyle=ComboBoxStyle.DropDownList,Dock=DockStyle.Top};
    readonly PictureBox preview=new StudioPreview(){Dock=DockStyle.Fill};
    readonly Label status=StudioTheme.Label("Kies een layout om te beginnen.",10,false,64);
    readonly Button play=StudioTheme.Button("Pauzeren");
    readonly Label previewTitle=StudioTheme.Label("Classic",17,true,40);
    readonly Label detail=StudioTheme.Label("",10,false,100);
    readonly Label connection=StudioTheme.Label("●  Status ophalen…",10,true,32);
    readonly System.Windows.Forms.Timer timer=new(){Interval=100};
    readonly PreviewImageCache cache=new();
    readonly DesignRenderer ownPreviewRenderer=new();
    readonly string appDirectory;
    Image? ownImage;
    ScreenDesign? ownPreview;
    bool showingCustom,paused,pngFrames;
    int frame,ticks;
    string Selected=>layouts.SelectedItem as string??"classic";
    bool Animated=>layouts.SelectedIndex>=3&&layouts.SelectedIndex<26;
    static string Title(string id)=>System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(id.Replace('-',' '));

    public SettingsWindow(string? directory=null)
    {
        appDirectory=directory??AppContext.BaseDirectory;
        Text="Qscreen — Layout Studio";Size=new Size(1220,850);MinimumSize=new Size(1040,740);
        StartPosition=FormStartPosition.CenterScreen;BackColor=StudioTheme.Background;ForeColor=StudioTheme.Text;Font=new Font("Segoe UI",10);
        string icon=Path.Combine(appDirectory,"branding","qprints.ico");if(File.Exists(icon))Icon=new Icon(icon);
        var sidebar=new Panel{Dock=DockStyle.Left,Width=240,BackColor=StudioTheme.Surface,Padding=new Padding(22)};
        var brand=StudioTheme.Label("Qscreen /",27,true,66);
        var tagline=StudioTheme.Label("YOUR DISPLAY. YOUR RULES.",8,false,42);
        var nav=StudioTheme.Label("WORKSPACE",8,true,38);
        var studio=StudioTheme.Button("Profielen & apparaten");studio.Dock=DockStyle.Top;studio.Height=46;studio.Click+=(_,_)=>Safe(()=>{using var manager=new AdvancedStudioWindow(appDirectory);manager.ShowDialog(this);LoadCustomPreview();});
        var create=StudioTheme.Button("+   Eigen ontwerp maken");create.Dock=DockStyle.Top;create.Height=48;create.Click+=(_,_)=>OpenDesigner();
        var spacer=new Panel{Dock=DockStyle.Top,Height=30};
        var tools=StudioTheme.Label("APPARAAT & BEHEER",8,true,38);
        var firmware=SideAction("Firmware bijwerken",()=>LaunchPowerShell("-NoProfile -STA -ExecutionPolicy Bypass -WindowStyle Hidden -File \""+Path.Combine(appDirectory,"flash-firmware.ps1")+"\""));
        var monitor=SideAction("Metingen starten",()=>LaunchPowerShell("-NoProfile -Command \"Start-ScheduledTask -TaskName TempMonitor\""));
        var logs=SideAction("Logboek openen",()=>Process.Start(new ProcessStartInfo("explorer.exe",Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),"TempMonitor")){UseShellExecute=true}));
        var version=StudioTheme.Label("Qscreen 2.0\nQPRINTS desktop companion",9,false,64);version.Dock=DockStyle.Bottom;
        sidebar.Controls.Add(logs);sidebar.Controls.Add(monitor);sidebar.Controls.Add(firmware);sidebar.Controls.Add(tools);sidebar.Controls.Add(spacer);sidebar.Controls.Add(create);sidebar.Controls.Add(studio);sidebar.Controls.Add(nav);sidebar.Controls.Add(tagline);sidebar.Controls.Add(brand);sidebar.Controls.Add(version);
        var workspace=new Panel{Dock=DockStyle.Fill,Padding=new Padding(28)};
        var header=new Panel{Dock=DockStyle.Top,Height=114};
        var eyebrow=StudioTheme.Label("LAYOUT STUDIO",9,true,25);eyebrow.ForeColor=StudioTheme.Accent;
        var headline=StudioTheme.Label("Maak het scherm van jou.",26,true,50);
        var subtitle=StudioTheme.Label("Ontdek de collectie of bouw je eigen ontwerp.",11,false,30);
        header.Controls.Add(subtitle);header.Controls.Add(headline);header.Controls.Add(eyebrow);
        var columns=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1};columns.ColumnStyles.Add(new(SizeType.Percent,62));columns.ColumnStyles.Add(new(SizeType.Percent,38));columns.RowStyles.Add(new(SizeType.Percent,100));
        var previewCard=new StudioCard{Dock=DockStyle.Fill,Margin=new Padding(0,0,16,0)};
        var previewLabel=StudioTheme.Label("SCHERMVOORBEELD                       412 × 412",8,true,35);
        var previewFooter=new Panel{Dock=DockStyle.Bottom,Height=109};
        var demo=StudioTheme.Label("Demo met testwaarden · geen scherm nodig",9,false,26);demo.Dock=DockStyle.Bottom;
        play.Dock=DockStyle.Bottom;play.Height=38;
        previewFooter.Controls.Add(previewTitle);previewFooter.Controls.Add(play);previewFooter.Controls.Add(demo);
        previewCard.Controls.Add(preview);previewCard.Controls.Add(previewFooter);previewCard.Controls.Add(previewLabel);
        var chooseCard=new StudioCard{Dock=DockStyle.Fill,Margin=new Padding(0)};
        var chooseTitle=StudioTheme.Label("Kies je look",19,true,45);
        var count=StudioTheme.Label("31 layouts  /  23 geanimeerd",10,false,38);
        StudioTheme.Input(layouts,false);layouts.DrawMode=DrawMode.OwnerDrawFixed;layouts.ItemHeight=32;
        layouts.DrawItem+=(_,e)=>{if(e.Index<0)return;using var brush=new SolidBrush((e.State&DrawItemState.Selected)!=0?StudioTheme.Raised:StudioTheme.Surface);e.Graphics.FillRectangle(brush,e.Bounds);TextRenderer.DrawText(e.Graphics,Title(LayoutSettings.Names[e.Index]),e.Font,e.Bounds,StudioTheme.Text,TextFormatFlags.VerticalCenter|TextFormatFlags.Left);};
        var gap=new Panel{Dock=DockStyle.Top,Height=18};
        var apply=StudioTheme.Button("Layout toepassen",true);apply.Dock=DockStyle.Bottom;apply.Height=46;apply.Click+=(_,_)=>Safe(Apply);
        var own=StudioTheme.Button("Open de ontwerpstudio");own.Dock=DockStyle.Bottom;own.Height=44;own.Click+=(_,_)=>OpenDesigner();
        var designHint=StudioTheme.Label("Liever iets unieks?\nVoeg je eigen achtergrond, kleuren en lettertypen toe in de ontwerpstudio.",10,false,116);designHint.Dock=DockStyle.Bottom;
        connection.Dock=DockStyle.Bottom;connection.ForeColor=StudioTheme.Muted;
        chooseCard.Controls.Add(detail);chooseCard.Controls.Add(gap);chooseCard.Controls.Add(layouts);chooseCard.Controls.Add(count);chooseCard.Controls.Add(chooseTitle);
        chooseCard.Controls.Add(own);chooseCard.Controls.Add(designHint);chooseCard.Controls.Add(apply);chooseCard.Controls.Add(connection);
        columns.Controls.Add(previewCard,0,0);columns.Controls.Add(chooseCard,1,0);
        status.Dock=DockStyle.Bottom;status.Padding=new Padding(4,16,4,0);
        workspace.Controls.Add(columns);workspace.Controls.Add(status);workspace.Controls.Add(header);
        Controls.Add(workspace);Controls.Add(sidebar);
        layouts.Items.AddRange(LayoutSettings.Names);
        layouts.SelectedIndexChanged+=(_,_)=>{ResetPreview();showingCustom=false;frame=0;paused=false;pngFrames=File.Exists(Path.Combine(appDirectory,"previews",Selected+"-00.png"));play.Enabled=Animated;play.Text=Animated?"Ⅱ  Pauzeren":"Statisch voorbeeld";previewTitle.Text=Title(Selected);detail.Text=Animated?"GEANIMEERD\n\nEen bewegend ontwerp met CPU, GPU en FPS. Bekijk het voorbeeld en pas je keuze toe.":"STATISCH\n\nEen rustig ontwerp met actuele CPU-, GPU- en FPS-waarden op je scherm.";status.ForeColor=StudioTheme.Muted;status.Text="Voorbeeld: "+Title(Selected)+". Klik op Layout toepassen om te wisselen.";timer.Start();ShowFrame();};
        play.Click+=(_,_)=>{paused=!paused;play.Text=paused?"▶  Afspelen":"Ⅱ  Pauzeren";};
        layouts.SelectedItem=LayoutSettings.Read(appDirectory);
        timer.Tick+=(_,_)=>
        {
            ticks++;if(ticks%10==0)UpdateConnection();
            if(WindowState==FormWindowState.Minimized||!Visible||OwnedForms.Length>0)return;
            if(showingCustom){if(ticks%10==0)ShowFrame();}
            else if(Animated&&!paused){frame=(frame+1)%40;ShowFrame();}
        };
        timer.Start();LoadCustomPreview();UpdateConnection();StudioTheme.FitWindow(this,new Size(1220,850),new Size(1000,700));
    }
    StudioButton SideAction(string title,Action action){var button=StudioTheme.Button(title);button.Dock=DockStyle.Top;button.Height=44;button.Click+=(_,_)=>Safe(action);return button;}
    void Safe(Action action){try{action();}catch(Exception ex){status.Text=ex.Message;status.ForeColor=StudioTheme.Accent;}}
    void OpenDesigner()=>Safe(()=>{using var designer=new DesignWindow(appDirectory);designer.ShowDialog(this);LoadCustomPreview();});
    protected override void OnHandleCreated(EventArgs e){base.OnHandleCreated(e);StudioTheme.DarkTitle(this);}
    void ResetPreview(){preview.Image=null;ownImage?.Dispose();ownImage=null;cache.Clear();}
    void ShowFrame()
    {
        try
        {
            if(showingCustom&&ownPreview!=null){var next=ownPreviewRenderer.Render(ownPreview,ScreenReadings.Demo);preview.Image=next;ownImage?.Dispose();ownImage=next;return;}
            string name=Animated?$"{Selected}-{frame:D2}.{(pngFrames?"png":"bmp")}":$"layout-{Selected}.png";
            preview.Image=cache.Get(Path.Combine(appDirectory,"previews",name));
        }
        catch(Exception ex) when(ex is IOException or ArgumentException or OutOfMemoryException){timer.Stop();status.Text="Voorbeeld kon niet worden geladen. Installeer Qscreen opnieuw.";}
    }
    void LoadCustomPreview()
    {
        if(!File.Exists(Path.Combine(appDirectory,"custom-active.txt")))return;
        Safe(()=>{ownPreview=DesignFiles.Load(Path.Combine(appDirectory,"custom-design.json"));ResetPreview();showingCustom=true;play.Enabled=false;play.Text="Eigen ontwerp";previewTitle.Text=ownPreview.Name;detail.Text="EIGEN ONTWERP\n\nJe persoonlijke ontwerp is actief. Open de ontwerpstudio om verder te bewerken.";status.Text="Eigen ontwerp: "+ownPreview.Name+" · voorbeeld met testwaarden.";ShowFrame();timer.Start();});
    }
    void UpdateConnection()
    {
        var delivery=ScreenDelivery.Read(appDirectory);bool fresh=delivery!=null&&DateTime.UtcNow-delivery.UpdatedUtc<TimeSpan.FromSeconds(30);
        connection.Text=!fresh?"●  Wachten op achtergrondmeting":delivery!.State switch{"received"=>"●  Scherm verbonden","preset"=>"●  USB verbonden","sending"=>"●  Ontwerp versturen…","sleeping"=>"●  Slaapstand","firmware-required"=>"●  Firmware bijwerken",_=>"●  Geen schermverbinding"};
        connection.ForeColor=fresh&&delivery!.State is "received" or "preset"?StudioTheme.Green:StudioTheme.Muted;
    }
    void Apply()
    {
        string path=Path.Combine(appDirectory,"layout.txt"),temporary=path+".new";File.WriteAllText(temporary,Selected+Environment.NewLine);File.Move(temporary,path,true);File.Delete(Path.Combine(appDirectory,"custom-active.txt"));
        StudioConfiguration.ManualLegacy(appDirectory);
        if(showingCustom)ResetPreview();showingCustom=false;play.Enabled=Animated;play.Text=Animated?"Ⅱ  Pauzeren":"Statisch voorbeeld";previewTitle.Text=Title(Selected);ShowFrame();status.ForeColor=StudioTheme.Green;status.Text=Title(Selected)+" opgeslagen. Wordt toegepast zodra je scherm verbonden is.";
    }
    protected override void Dispose(bool disposing)
    {
        if(disposing){timer.Dispose();preview.Image=null;cache.Dispose();ownImage?.Dispose();ownPreviewRenderer.Dispose();Icon?.Dispose();}
        base.Dispose(disposing);
    }
    static void LaunchPowerShell(string arguments)=>Process.Start(new ProcessStartInfo("powershell.exe",arguments){UseShellExecute=true,WindowStyle=ProcessWindowStyle.Hidden,WorkingDirectory=AppContext.BaseDirectory});
}

using System.ComponentModel;
using System.Drawing;
using System.Text.Json;
using System.Windows.Forms;

sealed class AdvancedStudioWindow : Form
{
    readonly string directory;
    readonly ProfileRepository repository;
    readonly StudioConfiguration config;
    readonly TabControl tabs=new(){Dock=DockStyle.Fill};
    public Action<string>? EditProfileRequested;
    public void ShowSection(string name)
    {
        RefreshProfiles();
        foreach(TabPage page in tabs.TabPages)if(page.Text==name){tabs.SelectedTab=page;break;}
    }
    public void SetManualProfile(string id)=>config.ManualProfileId=id;
    readonly Label status=StudioTheme.Label("Wijzig instellingen en klik op Instellingen bewaren.",10,false,50);
    readonly ListBox profiles=new(){Dock=DockStyle.Fill,DisplayMember="Name",BackColor=StudioTheme.Surface,ForeColor=StudioTheme.Text};
    readonly ComboBox defaultProfile=new(){Width=250,DropDownStyle=ComboBoxStyle.DropDownList,DisplayMember="Name",ValueMember="Id"};
    readonly ListBox devices=new(){Dock=DockStyle.Left,Width=285,BackColor=StudioTheme.Surface,ForeColor=StudioTheme.Text};
    readonly PropertyGrid deviceProperties=AdvancedUi.Inspector(null);
    readonly Label deviceStatus=StudioTheme.Label("Sluit een compatibel Qscreen-scherm aan.",10,false,100);
    readonly ComboBox deviceProfile=new(){Width=260,DropDownStyle=ComboBoxStyle.DropDownList,DisplayMember="Name",ValueMember="Id"};
    readonly TrackBar brightness=new(){Minimum=0,Maximum=100,TickFrequency=10,Width=260};
    readonly CheckBox liveBrightness=new(){Text="Helderheid direct toepassen",AutoSize=true};
    readonly HistoryPlot history=new();
    readonly Label liveValues=StudioTheme.Label("",11,true,58);
    readonly ComboBox metric=new(){Width=220,DropDownStyle=ComboBoxStyle.DropDownList};
    readonly ComboBox window=new(){Width=100,DropDownStyle=ComboBoxStyle.DropDownList};
    readonly System.Windows.Forms.Timer timer=new(){Interval=1000};
    DeviceRuntimeStatus[] runtime=[];
    DeviceAssignment? selectedDevice;
    bool updating;
    DateTime nextHistory,nextBrightness;
    readonly List<DataGridView> grids=[];
    public AdvancedStudioWindow(string directory)
    {
        this.directory=directory;repository=new(directory);config=StudioConfiguration.Load(directory);
        Text="Qscreen · Studio-beheer";Size=new(1180,820);MinimumSize=new(940,680);BackColor=StudioTheme.Background;ForeColor=StudioTheme.Text;Font=new("Segoe UI",10);StartPosition=FormStartPosition.CenterParent;
        tabs.DrawMode=TabDrawMode.OwnerDrawFixed;tabs.SizeMode=TabSizeMode.Fixed;tabs.ItemSize=new Size(148,36);
        tabs.DrawItem+=(_,e)=>{using var brush=new SolidBrush(e.Index==tabs.SelectedIndex?StudioTheme.Raised:StudioTheme.Background);e.Graphics.FillRectangle(brush,e.Bounds);TextRenderer.DrawText(e.Graphics,tabs.TabPages[e.Index].Text,Font,e.Bounds,StudioTheme.Text,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);};
        var bar=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=58,Padding=new(12)};
        AdvancedUi.Button(bar,"Instellingen bewaren",Save,Error,true);AdvancedUi.Button(bar,"Achtergrondmeting starten",()=>System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("powershell.exe","-NoProfile -Command \"Start-ScheduledTask -TaskName TempMonitor\""){UseShellExecute=false,CreateNoWindow=true}),Error);
        BuildProfiles();BuildDevices();BuildRules();BuildHistory();BuildAlerts();BuildLibrary();
        Controls.Add(tabs);Controls.Add(status);Controls.Add(bar);status.Dock=DockStyle.Bottom;
        timer.Tick+=(_,_)=>{RefreshDevices();if(tabs.SelectedTab?.Text=="Historie")RefreshHistory();if(nextBrightness!=default&&DateTime.UtcNow>=nextBrightness){nextBrightness=default;Safe(Save);}};timer.Start();RefreshProfiles();RefreshDevices();StudioTheme.FitWindow(this,new(1180,820),new(940,680));
    }
    TabPage Tab(string name){var page=new TabPage(name){BackColor=StudioTheme.Background,ForeColor=StudioTheme.Text,Padding=new(14)};tabs.TabPages.Add(page);return page;}
    void Error(Exception ex){status.ForeColor=Color.Orange;status.Text=ex.Message;}
    void Safe(Action action){try{action();}catch(Exception ex){Error(ex);}}
    void Save(){foreach(var grid in grids)grid.EndEdit();ValidateChildren();config.Save(directory);status.ForeColor=StudioTheme.Green;status.Text="Instellingen opgeslagen."+(string.IsNullOrEmpty(config.ManualProfileId)?" De achtergrondmeting neemt ze automatisch over.":" Handmatige override actief; kies Automatisch hervatten om profielregels te gebruiken.");}
    IReadOnlyList<ProfileEntry> ProfileChoices()=>new[]{new ProfileEntry("legacy","Huidig ontwerp / vaste layout","")}.Concat(repository.List()).ToList();
    ProfileEntry Profile()=>profiles.SelectedItem as ProfileEntry??throw new InvalidOperationException("Kies een profiel.");
    void RefreshProfiles()
    {
        updating=true;string defaultId=config.DefaultProfileId;string deviceId=selectedDevice?.ProfileId??"legacy";
        profiles.DataSource=null;profiles.DataSource=repository.List();
        defaultProfile.DataSource=ProfileChoices();defaultProfile.SelectedValue=defaultId;
        deviceProfile.DataSource=ProfileChoices();deviceProfile.SelectedValue=deviceId;updating=false;
    }
    void BuildProfiles()
    {
        var page=Tab("Profielen");var actions=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=118};
        AdvancedUi.Button(actions,"+ Huidig ontwerp",()=>{string file=Path.Combine(directory,"custom-draft.json");if(!File.Exists(file))file=Path.Combine(directory,"custom-design.json");repository.Add(File.Exists(file)?DesignFiles.Load(file):ScreenDesign.CreateDefault());RefreshProfiles();},Error);
        AdvancedUi.Button(actions,"Profiel bewerken",()=>{var item=Profile();if(EditProfileRequested!=null){EditProfileRequested(item.Id);return;}using var editor=new DesignWindow(directory,()=>{},DesignFiles.Load(item.File),saved=>repository.Save(item.Id,saved));editor.ShowDialog(this);RefreshProfiles();},Error);
        AdvancedUi.Button(actions,"Dupliceren",()=>{var design=DesignFiles.Load(Profile().File);design.Name+=" kopie";repository.Add(design);RefreshProfiles();},Error);
        AdvancedUi.Button(actions,"Verwijderen",()=>{var item=Profile();if(MessageBox.Show(this,"Profiel verwijderen? Geëxporteerde bestanden blijven bewaard.","Qscreen",MessageBoxButtons.YesNo)==DialogResult.Yes){repository.Delete(item.Id);RefreshProfiles();}},Error);
        AdvancedUi.Button(actions,"Handmatig activeren",()=>{config.ManualProfileId=Profile().Id;Save();status.Text="Handmatige override actief. Kies Automatisch hervatten om profielregels te gebruiken.";},Error,true);
        AdvancedUi.Button(actions,"Automatisch hervatten",()=>{config.ManualProfileId="";config.AutomaticProfiles=true;Save();},Error);
        AdvancedUi.Button(actions,"Ontwerp importeren",()=>{using var file=new OpenFileDialog{Filter="Qscreen ontwerp|*.qscreen;*.json;*.qscreenpack"};if(file.ShowDialog(this)==DialogResult.OK){var design=file.FileName.EndsWith(".qscreenpack",StringComparison.OrdinalIgnoreCase)?QscreenPackage.Import(file.FileName).Design:DesignFiles.Load(file.FileName);using var preview=new DesignPreviewWindow(design,$"{DisplayPages.Count(design)} pagina's · wordt als nieuw profiel bewaard");if(preview.ShowDialog(this)==DialogResult.OK){repository.Add(design);RefreshProfiles();}}},Error);
        var header=new FlowLayoutPanel{Dock=DockStyle.Top,Height=80};header.Controls.Add(new Label{Text="Standaardprofiel:",AutoSize=true});StudioTheme.Input(defaultProfile);header.Controls.Add(defaultProfile);
        defaultProfile.SelectedValueChanged+=(_,_)=>{if(!updating&&defaultProfile.SelectedValue is string id)config.DefaultProfileId=id;};
        var note=StudioTheme.Label("Meerdere profielen zonder opnieuw flashen. Handmatige overrides gaan vóór automatische regels; vaste apparaatprofielen kunnen onafhankelijk blijven.",10,false,60);
        page.Controls.Add(profiles);page.Controls.Add(actions);page.Controls.Add(header);page.Controls.Add(note);
    }
    void BuildRules()
    {
        var page=Tab("Automatisch wisselen");var settings=new FlowLayoutPanel{Dock=DockStyle.Top,Height=80};
        var enabled=new CheckBox{Text="Automatisch profielen wisselen",Checked=config.AutomaticProfiles,AutoSize=true};enabled.CheckedChanged+=(_,_)=>config.AutomaticProfiles=enabled.Checked;
        var delay=new NumericUpDown{Minimum=0,Maximum=120,Value=config.SwitchDelaySeconds,Width=65};delay.ValueChanged+=(_,_)=>config.SwitchDelaySeconds=(int)delay.Value;
        settings.Controls.AddRange([enabled,new Label{Text="Vertraging (s)",AutoSize=true},delay]);
        var grid=AdvancedUi.Grid(new BindingList<ProfileRule>(config.Rules));AdvancedUi.Profiles(grid,repository);grids.Add(grid);
        AdvancedUi.Button(settings,"+ Profielregel",()=>((BindingList<ProfileRule>)grid.DataSource!).Add(new(){ProfileId=config.DefaultProfileId}),Error);
        var note=StudioTheme.Label("Foreground = actief venster. Running = proces bestaat. Idle = geen invoer gedurende IdleSeconds. Hogere Priority wint. Gebruik alleen exe-namen, bijvoorbeeld code.exe of Warzone.exe.",10,false,70);
        page.Controls.Add(grid);page.Controls.Add(settings);page.Controls.Add(note);
    }
    void BuildDevices()
    {
        var page=Tab("Apparaten");devices.DisplayMember="Name";
        devices.DrawMode=DrawMode.OwnerDrawFixed;devices.ItemHeight=48;devices.BorderStyle=BorderStyle.None;
        devices.DrawItem+=(_,e)=>{if(e.Index<0)return;var original=(DeviceRuntimeStatus)devices.Items[e.Index];var item=runtime.FirstOrDefault(d=>d.DeviceId==original.DeviceId)??original;using var brush=new SolidBrush((e.State&DrawItemState.Selected)!=0?StudioTheme.Raised:StudioTheme.Surface);e.Graphics.FillRectangle(brush,e.Bounds);TextRenderer.DrawText(e.Graphics,item.Name,Font,new Rectangle(e.Bounds.X+10,e.Bounds.Y+4,e.Bounds.Width-20,22),StudioTheme.Text,TextFormatFlags.EndEllipsis);TextRenderer.DrawText(e.Graphics,item.Port+" · "+(item.Connected?"Verbonden":"Niet verbonden"),Font,new Rectangle(e.Bounds.X+10,e.Bounds.Y+25,e.Bounds.Width-20,22),item.Connected?StudioTheme.Green:StudioTheme.Muted,TextFormatFlags.EndEllipsis);};
        devices.SelectedIndexChanged+=(_,_)=>SelectDevice();
        var actions=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=110};
        AdvancedUi.Button(actions,"Vorige pagina",()=>Navigate(-1),Error);AdvancedUi.Button(actions,"Volgende pagina",()=>Navigate(1),Error);AdvancedUi.Button(actions,"Herstel paginarotatie",()=>{if(selectedDevice!=null){selectedDevice.RequestedPage=-1;selectedDevice.PageRequest=Guid.NewGuid().ToString("N");Save();}},Error);
        var controls=new FlowLayoutPanel{Dock=DockStyle.Top,Height=160,FlowDirection=FlowDirection.TopDown,WrapContents=false};StudioTheme.Input(deviceProfile);
        controls.Controls.Add(new Label{Text="Profiel voor dit apparaat",AutoSize=true});controls.Controls.Add(deviceProfile);controls.Controls.Add(new Label{Text="Handmatige helderheid (0–100%)",AutoSize=true});controls.Controls.Add(brightness);controls.Controls.Add(liveBrightness);
        deviceProfile.SelectedValueChanged+=(_,_)=>{if(!updating&&selectedDevice!=null&&deviceProfile.SelectedValue is string id)selectedDevice.ProfileId=id;};
        brightness.ValueChanged+=(_,_)=>{if(updating||selectedDevice==null)return;selectedDevice.Brightness.Manual=brightness.Value;selectedDevice.Brightness.Scheduled=false;deviceProperties.Refresh();if(liveBrightness.Checked)nextBrightness=DateTime.UtcNow.AddMilliseconds(300);};
        var inspector=new Panel{Dock=DockStyle.Fill,Padding=new(16)};inspector.Controls.Add(deviceProperties);inspector.Controls.Add(controls);inspector.Controls.Add(deviceStatus);
        page.Controls.Add(inspector);page.Controls.Add(devices);page.Controls.Add(actions);
    }
    void RefreshDevices()
    {
        try
        {
            string file=Path.Combine(directory,"devices-runtime.json");if(!File.Exists(file))return;
            runtime=(JsonSerializer.Deserialize<DeviceRuntimeStatus[]>(File.ReadAllText(file),DesignFiles.Json)??[]).Select(d=>DateTime.UtcNow-d.UpdatedUtc>TimeSpan.FromSeconds(30)?d with{Connected=false,Message="Geen actuele achtergrondmeting"}:d).ToArray();
            string[] ids=runtime.Select(d=>d.DeviceId).Distinct().ToArray();string current=SelectedRuntime()?.DeviceId??"";
            if(!devices.Items.Cast<DeviceRuntimeStatus>().Select(d=>d.DeviceId).SequenceEqual(ids))
            {
                devices.Items.Clear();foreach(string id in ids)devices.Items.Add(runtime.First(d=>d.DeviceId==id));
                int index=Array.IndexOf(ids,current);if(devices.Items.Count>0)devices.SelectedIndex=Math.Max(0,index);
            }
            var selected=SelectedRuntime();if(selected!=null)deviceStatus.Text=$"{selected.Name} · {selected.Port} · {(selected.Connected?"Verbonden":"Niet verbonden")}\nFirmware: {selected.Firmware}\nProfiel: {selected.ProfileName} · pagina {selected.Page+1}/{selected.PageCount}\n{selected.Message}";
            liveBrightness.Enabled=selected is {Connected:true,AdvancedControls:true};
            devices.Invalidate();
        }
        catch(Exception ex)when(ex is IOException or JsonException){ }
    }
    DeviceRuntimeStatus? SelectedRuntime()=>devices.SelectedItem is DeviceRuntimeStatus item?runtime.FirstOrDefault(d=>d.DeviceId==item.DeviceId):null;
    void SelectDevice()
    {
        var item=SelectedRuntime();if(item==null)return;
        selectedDevice=config.Devices.FirstOrDefault(d=>d.DeviceId==item.DeviceId||!string.IsNullOrEmpty(item.UsbId)&&d.DeviceId==item.UsbId);
        if(selectedDevice==null){selectedDevice=new(){DeviceId=item.DeviceId,Name=item.Name};config.Devices.Add(selectedDevice);}
        selectedDevice.DeviceId=item.DeviceId;
        updating=true;deviceProperties.SelectedObject=selectedDevice;deviceProfile.SelectedValue=string.IsNullOrEmpty(selectedDevice.ProfileId)?"legacy":selectedDevice.ProfileId;brightness.Value=Math.Clamp(selectedDevice.Brightness.Manual,0,100);liveBrightness.Enabled=item.Connected&&item.AdvancedControls;updating=false;
    }
    void Navigate(int direction)
    {
        var runtime=SelectedRuntime();if(runtime==null||selectedDevice==null)return;
        selectedDevice.RequestedPage=(runtime.Page+direction+Math.Max(1,runtime.PageCount))%Math.Max(1,runtime.PageCount);selectedDevice.PageRequest=Guid.NewGuid().ToString("N");Save();
    }
    void BuildHistory()
    {
        var page=Tab("Historie");var choices=new FlowLayoutPanel{Dock=DockStyle.Top,Height=66};
        metric.Items.AddRange(SensorHistory.Metrics.Cast<object>().ToArray());metric.FormattingEnabled=true;metric.Format+=(_,e)=>{if(e.ListItem is WidgetKind k)e.Value=WidgetLabels.Title(k);};metric.SelectedIndex=0;
        window.Items.AddRange([1,5,15,60]);window.SelectedItem=5;StudioTheme.Input(metric);StudioTheme.Input(window);
        var sampling=new NumericUpDown{Minimum=1,Maximum=60,Value=config.SamplingSeconds,Width=70};sampling.ValueChanged+=(_,_)=>config.SamplingSeconds=(int)sampling.Value;
        choices.Controls.AddRange([metric,window,new Label{Text="minuten · meetinterval (s)",AutoSize=true},sampling]);
        metric.SelectedIndexChanged+=(_,_)=>nextHistory=DateTime.MinValue;window.SelectedIndexChanged+=(_,_)=>nextHistory=DateTime.MinValue;
        page.Controls.Add(history);page.Controls.Add(choices);page.Controls.Add(liveValues);
    }
    void RefreshHistory()
    {
        liveValues.Text=LiveTelemetry.Summary(LiveTelemetry.Read(directory,DateTime.UtcNow));
        try
        {
            if(DateTime.UtcNow>=nextHistory){AtomicJson.Write(Path.Combine(directory,"history-request.json"),new HistoryRequest((WidgetKind)metric.SelectedItem!,(int)window.SelectedItem!,DateTime.UtcNow));nextHistory=DateTime.UtcNow.AddSeconds(5);}
            string file=Path.Combine(directory,"history-preview.json");if(File.Exists(file)){var response=JsonSerializer.Deserialize<HistoryResponse>(File.ReadAllText(file),DesignFiles.Json);history.Series=response!=null&&response.Metric==(WidgetKind)metric.SelectedItem!&&response.Minutes==(int)window.SelectedItem!&&DateTime.UtcNow-response.UpdatedUtc<TimeSpan.FromSeconds(15)?response.Series:null;history.Invalidate();}
        }
        catch(Exception ex)when(ex is IOException or JsonException){ }
    }
    void BuildAlerts()
    {
        var page=Tab("Waarschuwingen");var grid=AdvancedUi.Grid(new BindingList<AlertRule>(config.Alerts));grids.Add(grid);
        var actions=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=56};
        AdvancedUi.Button(actions,"+ Waarschuwing",()=>((BindingList<AlertRule>)grid.DataSource!).Add(new()),Error);
        AdvancedUi.Button(actions,"Voorbeeldregels CPU",()=>{var binding=(BindingList<AlertRule>)grid.DataSource!;binding.Add(new(){Name="CPU koel",Condition=AlertCondition.Below,Threshold=60,Hysteresis=0,Priority=10,Color=Color.DeepSkyBlue});binding.Add(new(){Name="CPU warm",Threshold=60,Hysteresis=2,Priority=20,Color=Color.Orange});binding.Add(new(){Name="CPU heet",Threshold=80,Hysteresis=2,Priority=30,Color=Color.OrangeRed});},Error);
        var inspector=AdvancedUi.Inspector(null);inspector.Dock=DockStyle.Right;inspector.Width=300;
        grid.SelectionChanged+=(_,_)=>inspector.SelectedObject=grid.CurrentRow?.DataBoundItem;
        var note=StudioTheme.Label("Hogere Priority wint. Above/Below gebruiken hysterese; meldingen respecteren CooldownSeconds. FullScreen is tijdelijk en wijzigt je ontwerp niet. Waarschuwingen vervangen geen hardwarebeveiliging.",10,false,76);
        page.Controls.Add(grid);page.Controls.Add(inspector);page.Controls.Add(actions);page.Controls.Add(note);
    }
    void BuildLibrary()
    {
        var page=Tab("Design Hub");var note=StudioTheme.Label("LOKALE DESIGN HUB\nZoek, bekijk en importeer pakketten als nieuwe profielen. Online delen is nog niet geconfigureerd.",16,true,110);
        var actions=new FlowLayoutPanel{Dock=DockStyle.Top,Height=64};AdvancedUi.Button(actions,"Bibliotheek openen",()=>{using var hub=new TemplateHubWindow(directory);hub.ShowDialog(this);RefreshProfiles();},Error,true);page.Controls.Add(actions);page.Controls.Add(note);
    }
    protected override void OnHandleCreated(EventArgs e){base.OnHandleCreated(e);StudioTheme.DarkTitle(this);}
    protected override void Dispose(bool disposing){if(disposing)timer.Dispose();base.Dispose(disposing);}
}

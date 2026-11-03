using System.Drawing;
using System.Text.Json;
using System.Windows.Forms;

sealed class TemplatesWindow : Form
{
    readonly string directory;readonly FlowLayoutPanel cards=new(){Dock=DockStyle.Fill,AutoScroll=true,Padding=new(16),BackColor=StudioTheme.Background};
    readonly TextBox search=new(){Width=240,PlaceholderText="Zoek templates…"};readonly ComboBox category=new(){Width=150,DropDownStyle=ComboBoxStyle.DropDownList};
    readonly CheckBox favoritesOnly=new(){Text="Favorieten",AutoSize=true};readonly CheckBox animatedOnly=new(){Text="Geanimeerd",AutoSize=true};
    readonly Label status=StudioTheme.Label("21 QPRINTS templates · previews gebruiken gelabelde demowaarden",10,false,55);
    readonly HashSet<string> favorites=[];IReadOnlyList<DesignTemplate> all=[];bool loading;
    public TemplatesWindow(string directory)
    {
        this.directory=directory;Text="Qscreen · Templates";Size=new(1240,900);MinimumSize=new(860,680);StartPosition=FormStartPosition.CenterParent;BackColor=StudioTheme.Background;ForeColor=StudioTheme.Text;Font=new("Segoe UI",10);
        string file=Path.Combine(directory,"template-favorites.json");try{if(File.Exists(file)&&new FileInfo(file).Length<64000)favorites.UnionWith(JsonSerializer.Deserialize<string[]>(File.ReadAllText(file))??[]);}catch(Exception ex)when(ex is IOException or JsonException){ }
        var toolbar=new FlowLayoutPanel{Dock=DockStyle.Top,Height=105,Padding=new(18)};StudioTheme.Input(search);StudioTheme.Input(category);category.Items.Add("Alle categorieën");category.Items.AddRange(Enum.GetNames<TemplateCategory>());category.SelectedIndex=0;
        toolbar.Controls.AddRange([search,category,favoritesOnly,animatedOnly]);AdvancedUi.Button(toolbar,"Lokale DesignHub",()=>{using var hub=new TemplateHubWindow(directory);hub.ShowDialog(this);LoadTemplates();});AdvancedUi.Button(toolbar,"Weer instellen",()=>{using var weather=new WeatherSettingsWindow(directory);weather.ShowDialog(this);});
        search.TextChanged+=(_,_)=>Rebuild();category.SelectedIndexChanged+=(_,_)=>Rebuild();favoritesOnly.CheckedChanged+=(_,_)=>Rebuild();animatedOnly.CheckedChanged+=(_,_)=>Rebuild();
        Controls.Add(cards);Controls.Add(toolbar);Controls.Add(status);status.Dock=DockStyle.Bottom;Shown+=(_,_)=>LoadTemplates();StudioTheme.FitWindow(this,new(1240,900),new(860,680));
    }
    async void LoadTemplates()
    {
        if(loading)return;loading=true;status.Text="Templates laden…";
        try{var built=await Task.Run(()=>PremiumTemplates.All);var local=await new LocalTemplateLibrary(directory).SearchAsync("",null);if(IsDisposed)return;all=built.Concat(local.Take(64)).ToArray();Rebuild();}
        catch(Exception ex){if(!IsDisposed)status.Text=ex.Message;}finally{loading=false;}
    }
    void Rebuild()
    {
        if(IsDisposed)return;cards.SuspendLayout();foreach(Control control in cards.Controls.Cast<Control>().ToArray())control.Dispose();cards.Controls.Clear();
        var found=all.Where(t=>(category.SelectedIndex<=0||t.Manifest.Category==Enum.Parse<TemplateCategory>((string)category.SelectedItem!))&&(!favoritesOnly.Checked||favorites.Contains(t.Id))&&(!animatedOnly.Checked||t.Manifest.Animated)&&(string.IsNullOrWhiteSpace(search.Text)||t.Manifest.Name.Contains(search.Text,StringComparison.CurrentCultureIgnoreCase)||t.Manifest.Description.Contains(search.Text,StringComparison.CurrentCultureIgnoreCase))).ToList();
        foreach(var item in found.Take(85))cards.Controls.Add(Card(item));cards.ResumeLayout();status.Text=$"{found.Count} templates · ingebouwde originelen blijven behouden · previews met demowaarden";
    }
    Control Card(DesignTemplate item)
    {
        var card=new StudioCard{Width=275,Height=367,Margin=new(8),Padding=new(14)};
        var picture=new StudioPreview{Dock=DockStyle.Top,Height=190};if(Builtin(item)){using var renderer=new DesignRenderer();picture.Image=renderer.Render(item.Design,ScreenReadings.Demo);}else try{picture.Image=new LocalTemplateLibrary(directory).Thumbnail(item.Id);}catch(Exception ex)when(ex is IOException or InvalidDataException or ArgumentException){ }picture.Disposed+=(_,_)=>picture.Image?.Dispose();
        var name=StudioTheme.Label(item.Manifest.Name,15,true,30);var info=StudioTheme.Label(item.Manifest.Category+"  /  "+(item.Manifest.Animated?"● Bewegend":"○ Statisch"),9,false,25);
        var actions=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=90,Margin=new(0),Padding=new(0)};
        void Button(string caption,Action action,bool primary=false){var button=StudioTheme.Button(caption,primary);button.Width=112;button.Height=34;button.Font=new("Segoe UI",9,FontStyle.Bold);button.Margin=new(2);button.Click+=(_,_)=>Safe(action);actions.Controls.Add(button);}
        Button("Voorbeeld",()=>ShowPreview(item,false));Button("Toepassen",()=>ShowPreview(item,true),true);Button("Dupliceren",()=>Duplicate(item));
        Button(favorites.Contains(item.Id)?"★ Favoriet":"☆ Favoriet",()=>{if(!favorites.Add(item.Id))favorites.Remove(item.Id);AtomicJson.Write(Path.Combine(directory,"template-favorites.json"),favorites.ToArray());Rebuild();});
        card.Controls.Add(info);card.Controls.Add(name);card.Controls.Add(picture);card.Controls.Add(actions);return card;
    }
    static bool Builtin(DesignTemplate item)=>PremiumTemplates.All.Any(t=>t.Id==item.Id);
    async Task<DesignTemplate> Full(DesignTemplate item)=>Builtin(item)?item:await new LocalTemplateLibrary(directory).DownloadAsync(item.Id);
    async void Duplicate(DesignTemplate item)
    {try{item=await Full(item);if(IsDisposed)return;var d=DesignSnapshot.Capture(item.Design).Restore();d.Name=d.Name[..Math.Min(114,d.Name.Length)]+" kopie";new LocalTemplateLibrary(directory).Add(d,item.Manifest.Category,new(){Name=d.Name,Author="Lokaal",MinimumAppVersion="2.1.0",TemplateId=Guid.NewGuid().ToString("N"),Description=item.Manifest.Description,Animated=DesignRenderer.IsAnimated(d),Category=item.Manifest.Category});LoadTemplates();}catch(Exception ex){if(!IsDisposed)status.Text=ex.Message;}}
    async void ShowPreview(DesignTemplate item,bool apply)
    {try{item=await Full(item);if(IsDisposed)return;using var dialog=new TemplatePreviewWindow(item,apply);if(dialog.ShowDialog(this)!=DialogResult.OK)return;var profiles=new ProfileRepository(directory);var design=DesignSnapshot.Capture(item.Design).Restore();string id=profiles.Add(design);
        if(dialog.Edit){using var editor=new DesignWindow(directory,()=>{},design,saved=>profiles.Save(id,saved));editor.ShowDialog(this);status.Text="Een nieuw bewerkbaar profiel is aangemaakt. Origineel en huidig ontwerp zijn behouden.";return;}
        var config=StudioConfiguration.Load(directory);config.ManualProfileId=id;config.Save(directory);
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("powershell.exe","-NoProfile -Command \"Start-ScheduledTask -TaskName TempMonitor -ErrorAction Stop\""){UseShellExecute=false,CreateNoWindow=true});
        status.Text="Nieuw profiel geactiveerd. Controleer de ontvangst onder Profielen & apparaten; het bestaande ontwerp is behouden.";}catch(Exception ex){if(!IsDisposed)status.Text=ex.Message;}
    }
    void Safe(Action action){try{action();}catch(Exception ex){status.Text=ex.Message;}}
    protected override void OnHandleCreated(EventArgs e){base.OnHandleCreated(e);StudioTheme.DarkTitle(this);}
}
sealed class TemplatePreviewWindow : Form
{
    public bool Edit {get;private set;}
    readonly DesignRenderer renderer=new();readonly PictureBox preview=new StudioPreview{Dock=DockStyle.Fill};readonly System.Windows.Forms.Timer timer=new(){Interval=100};bool paused;
    public TemplatePreviewWindow(DesignTemplate item,bool apply)
    {
        Text="Qscreen · "+item.Manifest.Name;Size=new(650,800);MinimumSize=new(540,650);BackColor=StudioTheme.Background;ForeColor=StudioTheme.Text;StartPosition=FormStartPosition.CenterParent;
        var details=StudioTheme.Label(item.Manifest.Name+"\n"+item.Manifest.Description+"\nDEMO · testwaarden, geen actuele sensormeting. Weer is niet beschikbaar in deze preview.",12,true,120);details.Padding=new(18,8,18,0);
        var controls=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=110,Padding=new(12)};
        AdvancedUi.Button(controls,apply?"Nieuw profiel toepassen":"Dit profiel toepassen",()=>{DialogResult=DialogResult.OK;Close();},primary:true);
        AdvancedUi.Button(controls,"Bewerken als kopie",()=>{Edit=true;DialogResult=DialogResult.OK;Close();});
        var play=AdvancedUi.Button(controls,"Pauzeren",()=>paused=!paused);play.Enabled=DesignRenderer.IsAnimated(item.Design);play.Click+=(_,_)=>play.Text=paused?"Afspelen":"Pauzeren";
        void Render(){var old=preview.Image;preview.Image=renderer.Render(item.Design,ScreenReadings.Demo);old?.Dispose();}
        timer.Tick+=(_,_)=>{if(!paused&&Visible&&WindowState!=FormWindowState.Minimized)Render();};Controls.Add(preview);Controls.Add(controls);Controls.Add(details);Render();if(DesignRenderer.IsAnimated(item.Design)||item.Design.Widgets.Any(w=>w.Kind is WidgetKind.Time or WidgetKind.Date))timer.Start();StudioTheme.FitWindow(this,new(650,800),new(540,650));
    }
    protected override void OnHandleCreated(EventArgs e){base.OnHandleCreated(e);StudioTheme.DarkTitle(this);}
    protected override void Dispose(bool disposing){if(disposing){timer.Dispose();renderer.Dispose();preview.Image?.Dispose();}base.Dispose(disposing);}
}

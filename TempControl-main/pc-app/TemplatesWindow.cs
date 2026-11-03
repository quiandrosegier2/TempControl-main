using System.Drawing;
using System.Text.Json;
using System.Windows.Forms;

sealed class TemplatesWindow : Form
{
    readonly string directory;readonly FlowLayoutPanel cards=new(){Dock=DockStyle.Fill,AutoScroll=true,Padding=new(16),BackColor=StudioTheme.Background};
    readonly TextBox search=new(){Width=240,PlaceholderText="Zoek templates…"};readonly ComboBox category=new(){Width=150,DropDownStyle=ComboBoxStyle.DropDownList};
    readonly CheckBox favoritesOnly=new(){Text="Favorieten",AutoSize=true};readonly CheckBox animatedOnly=new(){Text="Geanimeerd",AutoSize=true};
    readonly CheckBox staticOnly=new(){Text="Statisch",AutoSize=true};
    readonly ComboBox collection=new(){Width=175,DropDownStyle=ComboBoxStyle.DropDownList};
    readonly Dictionary<string,Bitmap> thumbnails=[];
    public Action<DesignTemplate,bool>? NavigateTemplate;
    public Action<string>? NavigateSettings;
    public int LoadedCount=>all.Count;
    public int VisibleCount=>cards.Controls.Count;
    readonly Label status=StudioTheme.Label("PREMIUM 100 · thumbnails met gelabelde demowaarden",10,false,55);
    readonly HashSet<string> favorites=[];IReadOnlyList<DesignTemplate> all=[];bool loading;
    public TemplatesWindow(string directory)
    {
        this.directory=directory;Text="Qscreen · Templates";Size=new(1240,900);MinimumSize=new(860,680);StartPosition=FormStartPosition.CenterParent;BackColor=StudioTheme.Background;ForeColor=StudioTheme.Text;Font=new("Segoe UI",10);
        string file=Path.Combine(directory,"template-favorites.json");try{if(File.Exists(file)&&new FileInfo(file).Length<64000)favorites.UnionWith(JsonSerializer.Deserialize<string[]>(File.ReadAllText(file))??[]);}catch(Exception ex)when(ex is IOException or JsonException){ }
        var toolbar=new FlowLayoutPanel{Dock=DockStyle.Top,Height=105,Padding=new(18)};StudioTheme.Input(search);StudioTheme.Input(category);category.Items.Add("Alle categorieën");category.Items.AddRange(Enum.GetNames<TemplateCategory>());category.SelectedIndex=0;
        StudioTheme.Input(collection);collection.Items.AddRange(["Premium 100","Studio Originals · 21","Lokale ontwerpen","Alle collecties","Neon 01–50","Expansion 51–100"]);collection.SelectedIndex=0;
        toolbar.Controls.AddRange([collection,search,category,favoritesOnly,animatedOnly,staticOnly]);AdvancedUi.Button(toolbar,"Lokale DesignHub",()=>{if(NavigateSettings!=null){NavigateSettings("Design Hub");return;}using var hub=new TemplateHubWindow(directory);hub.ShowDialog(this);LoadTemplates();});AdvancedUi.Button(toolbar,"Weer instellen",()=>{if(NavigateSettings!=null){NavigateSettings("Weer");return;}using var weather=new WeatherSettingsWindow(directory);weather.ShowDialog(this);});
        collection.SelectedIndexChanged+=(_,_)=>Rebuild();
        cards.SizeChanged+=(_,_)=>SizeCards();
        search.TextChanged+=(_,_)=>Rebuild();category.SelectedIndexChanged+=(_,_)=>Rebuild();favoritesOnly.CheckedChanged+=(_,_)=>Rebuild();animatedOnly.CheckedChanged+=(_,_)=>Rebuild();
        animatedOnly.CheckedChanged+=(_,_)=>{if(animatedOnly.Checked)staticOnly.Checked=false;};staticOnly.CheckedChanged+=(_,_)=>{if(staticOnly.Checked)animatedOnly.Checked=false;Rebuild();};
        Controls.Add(cards);Controls.Add(toolbar);Controls.Add(status);status.Dock=DockStyle.Bottom;Shown+=(_,_)=>LoadTemplates();StudioTheme.FitWindow(this,new(1240,900),new(860,680));
    }
    async void LoadTemplates()
    {
        if(loading)return;loading=true;status.Text="Templates laden…";
        try{var built=await Task.Run(()=>OfficialTemplates.Premium.Concat(PremiumTemplates.All).ToArray());var local=await new LocalTemplateLibrary(directory).SearchAsync("",null);if(IsDisposed)return;all=built.Concat(local.Take(64)).ToArray();Rebuild();}
        catch(Exception ex){if(!IsDisposed)status.Text=ex.Message;}finally{loading=false;}
    }
    void Rebuild()
    {
        if(IsDisposed)return;cards.SuspendLayout();foreach(Control control in cards.Controls.Cast<Control>().ToArray())control.Dispose();cards.Controls.Clear();
        var found=all.Where(t=>CollectionMatches(t)&&(category.SelectedIndex<=0||t.Manifest.Category==Enum.Parse<TemplateCategory>((string)category.SelectedItem!))&&(!favoritesOnly.Checked||favorites.Contains(t.Id))&&(!animatedOnly.Checked||t.Manifest.Animated)&&(!staticOnly.Checked||!t.Manifest.Animated)&&(string.IsNullOrWhiteSpace(search.Text)||t.Manifest.Name.Contains(search.Text,StringComparison.CurrentCultureIgnoreCase)||t.Manifest.Description.Contains(search.Text,StringComparison.CurrentCultureIgnoreCase))).ToList();
        foreach(var item in found)cards.Controls.Add(Card(item));SizeCards();cards.ResumeLayout();status.Text=$"{found.Count} templates · ingebouwde originelen blijven behouden · previews met demowaarden";
    }
    void SizeCards()
    {
        int available=Math.Max(276,cards.ClientSize.Width-cards.Padding.Horizontal-20),columns=Math.Clamp(available/276,1,6),width=Math.Max(260,available/columns-16);
        foreach(Control card in cards.Controls)if(card.Width!=width)card.Width=width;
    }
    bool CollectionMatches(DesignTemplate item)=>collection.SelectedIndex switch{0=>OfficialTemplates.IsPremium(item),1=>OfficialTemplates.IsOriginal(item),2=>!Builtin(item),4=>NeonTemplates.All.Any(t=>ReferenceEquals(t,item)),5=>ExpansionTemplates.All.Any(t=>ReferenceEquals(t,item)),_=>true};
    Control Card(DesignTemplate item)
    {
        var card=new StudioCard{Width=275,Height=367,Margin=new(8),Padding=new(14)};
        var picture=new StudioPreview{Dock=DockStyle.Top,Height=190};if(Builtin(item)){
            if(!thumbnails.TryGetValue(item.Id,out var thumb)){using var renderer=new DesignRenderer();using var frame=renderer.Render(item.Design,ScreenReadings.Demo);thumb=new Bitmap(frame,new Size(190,190));thumbnails.Add(item.Id,thumb);}picture.Image=new Bitmap(thumb);
        }else try{picture.Image=new LocalTemplateLibrary(directory).Thumbnail(item.Id);}catch(Exception ex)when(ex is IOException or InvalidDataException or ArgumentException){ }picture.Disposed+=(_,_)=>picture.Image?.Dispose();
        int number=OfficialTemplates.Number(item);string caption=number>0?$"{number:00} · {item.Manifest.Name}":item.Manifest.Name;
        var name=StudioTheme.Label(caption,15,true,30);var info=StudioTheme.Label(item.Manifest.Category+"  /  "+(item.Manifest.Animated?"● Bewegend":"○ Statisch"),9,false,25);
        var actions=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=90,Margin=new(0),Padding=new(0)};
        void Button(string caption,Action action,bool primary=false){var button=StudioTheme.Button(caption,primary);button.Width=112;button.Height=34;button.Font=new("Segoe UI",9,FontStyle.Bold);button.Margin=new(2);button.Click+=(_,_)=>Safe(action);actions.Controls.Add(button);}
        Button("Voorbeeld",()=>ShowPreview(item,false));Button("Toepassen",()=>ShowPreview(item,true),true);Button("Dupliceren",()=>Duplicate(item));
        Button(favorites.Contains(item.Id)?"★ Favoriet":"☆ Favoriet",()=>{if(!favorites.Add(item.Id))favorites.Remove(item.Id);AtomicJson.Write(Path.Combine(directory,"template-favorites.json"),favorites.ToArray());Rebuild();});
        card.Controls.Add(info);card.Controls.Add(name);card.Controls.Add(picture);card.Controls.Add(actions);return card;
    }
    static bool Builtin(DesignTemplate item)=>OfficialTemplates.Contains(item);
    async Task<DesignTemplate> Full(DesignTemplate item)=>Builtin(item)?item:await new LocalTemplateLibrary(directory).DownloadAsync(item.Id);
    async void Duplicate(DesignTemplate item)
    {try{item=await Full(item);if(IsDisposed)return;var d=DesignSnapshot.Capture(item.Design).Restore();d.Name=d.Name[..Math.Min(114,d.Name.Length)]+" kopie";new LocalTemplateLibrary(directory).Add(d,item.Manifest.Category,new(){Name=d.Name,Author="Lokaal",MinimumAppVersion="2.1.0",TemplateId=Guid.NewGuid().ToString("N"),Description=item.Manifest.Description,Animated=DesignRenderer.IsAnimated(d),Category=item.Manifest.Category});LoadTemplates();}catch(Exception ex){if(!IsDisposed)status.Text=ex.Message;}}
    async void ShowPreview(DesignTemplate item,bool apply)
    {try{item=await Full(item);if(IsDisposed)return;if(NavigateTemplate!=null){NavigateTemplate(item,apply);return;}using var dialog=new TemplatePreviewWindow(item,apply,directory);if(dialog.ShowDialog(this)!=DialogResult.OK)return;var profiles=new ProfileRepository(directory);var design=DesignSnapshot.Capture(item.Design).Restore();string id=profiles.Add(design);
        if(dialog.Edit){using var editor=new DesignWindow(directory,()=>{},design,saved=>profiles.Save(id,saved));editor.ShowDialog(this);status.Text="Een nieuw bewerkbaar profiel is aangemaakt. Origineel en huidig ontwerp zijn behouden.";return;}
        var config=StudioConfiguration.Load(directory);config.ManualProfileId=id;config.Save(directory);
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("powershell.exe","-NoProfile -Command \"Start-ScheduledTask -TaskName TempMonitor -ErrorAction Stop\""){UseShellExecute=false,CreateNoWindow=true});
        status.Text="Nieuw profiel geactiveerd. Controleer de ontvangst onder Profielen & apparaten; het bestaande ontwerp is behouden.";}catch(Exception ex){if(!IsDisposed)status.Text=ex.Message;}
    }
    void Safe(Action action){try{action();}catch(Exception ex){status.Text=ex.Message;}}
    protected override void OnHandleCreated(EventArgs e){base.OnHandleCreated(e);StudioTheme.DarkTitle(this);}
    protected override void Dispose(bool disposing){if(disposing){foreach(var thumbnail in thumbnails.Values)thumbnail.Dispose();thumbnails.Clear();}base.Dispose(disposing);}
}
sealed class TemplatePreviewWindow : Form
{
    public Action? ApplyRequested;
    public Action? EditRequested;
    public bool Edit {get;private set;}
    readonly DesignRenderer renderer=new();readonly PictureBox preview=new StudioPreview{Dock=DockStyle.Fill};readonly System.Windows.Forms.Timer timer=new(){Interval=100};bool paused;
    public TemplatePreviewWindow(DesignTemplate item,bool apply,string? directory=null)
    {
        Text="Qscreen · "+item.Manifest.Name;Size=new(650,800);MinimumSize=new(540,650);BackColor=StudioTheme.Background;ForeColor=StudioTheme.Text;StartPosition=FormStartPosition.CenterParent;
        var details=StudioTheme.Label("",12,true,120);details.Padding=new(18,8,18,0);
        var demoMode=new CheckBox{Text="Demowaarden",Checked=directory==null,AutoSize=true,Padding=new(6,12,0,0)};LiveTelemetry? snapshot=null;DateTime nextSnapshot=default;
        var still=DesignSnapshot.Capture(item.Design).Restore();still.AnimationsEnabled=false;
        var controls=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=110,Padding=new(12)};
        AdvancedUi.Button(controls,apply?"Nieuw profiel toepassen":"Dit profiel toepassen",()=>{if(ApplyRequested!=null){ApplyRequested();return;}DialogResult=DialogResult.OK;Close();},primary:true);
        AdvancedUi.Button(controls,"Bewerken als kopie",()=>{if(EditRequested!=null){EditRequested();return;}Edit=true;DialogResult=DialogResult.OK;Close();});
        var play=AdvancedUi.Button(controls,"Pauzeren",()=>{paused=!paused;Render();});play.Enabled=DesignRenderer.IsAnimated(item.Design);play.Click+=(_,_)=>play.Text=paused?"Afspelen":"Pauzeren";controls.Controls.Add(demoMode);demoMode.CheckedChanged+=(_,_)=>{nextSnapshot=default;Render();};
        void Render()
        {
            if(directory!=null&&DateTime.UtcNow>=nextSnapshot){snapshot=LiveTelemetry.Read(directory,DateTime.UtcNow);nextSnapshot=DateTime.UtcNow.AddSeconds(1);}
            var readings=demoMode.Checked?ScreenReadings.Demo:snapshot is {} live?live.Readings with{Now=DateTime.Now}:LiveTelemetry.Unavailable(DateTime.Now);
            details.Text=item.Manifest.Name+"\n"+item.Manifest.Description+"\n"+(demoMode.Checked?"DEMO · testwaarden, geen actuele sensormeting.":LiveTelemetry.Summary(snapshot));
            var old=preview.Image;preview.Image=renderer.Render(paused?still:item.Design,readings);old?.Dispose();timer.Interval=!paused&&(DesignRenderer.IsAnimated(item.Design)||renderer.HasPendingMotion||renderer.HasAnimatedBackground)?100:1000;
        }
        timer.Tick+=(_,_)=>{if(Visible&&WindowState!=FormWindowState.Minimized)Render();};Controls.Add(preview);Controls.Add(controls);Controls.Add(details);Render();timer.Start();StudioTheme.FitWindow(this,new(650,800),new(540,650));
    }
    protected override void OnHandleCreated(EventArgs e){base.OnHandleCreated(e);StudioTheme.DarkTitle(this);}
    protected override void Dispose(bool disposing){if(disposing){timer.Dispose();renderer.Dispose();preview.Image?.Dispose();}base.Dispose(disposing);}
}

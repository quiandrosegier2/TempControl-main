using System.Drawing;
using System.Windows.Forms;
using System.Security.Cryptography;

// Reuse the existing WinForms editors as internal pages; account setup is optional.
sealed class StudioShellWindow : Form
{
    readonly string directory;
    readonly Panel content=new(){Dock=DockStyle.Fill};
    readonly FlowLayoutPanel navigation=new(){Dock=DockStyle.Top,Height=66,Padding=new(12),WrapContents=false};
    readonly FlowLayoutPanel context=new(){Dock=DockStyle.Top,Height=45,Padding=new(12,2,12,2)};
    readonly Label status=StudioTheme.Label("Kies een layout of maak je eigen ontwerp.",10,false,44);
    readonly TemplatesWindow gallery;
    readonly ProfileRepository repository;
    readonly Dictionary<string,Form> pages=[];
    DesignWindow? editor;
    Form? transient;
    int deliveryRequest;
    public StudioShellWindow():this(AppContext.BaseDirectory){}
    public StudioShellWindow(string directory)
    {
        this.directory=directory;repository=new(directory);
        Text="Qscreen Studio · QPRINTS";BackColor=StudioTheme.Background;ForeColor=StudioTheme.Text;Font=new("Segoe UI",10);
        Size=new(1420,960);MinimumSize=new(1050,760);
        var branding=new Panel{Width=224,Height=46};
        var mark=new PictureBox{Image=NeonArtwork.BrandMark(),SizeMode=PictureBoxSizeMode.Zoom,Width=49,Height=43,Dock=DockStyle.Left};mark.Disposed+=(_,_)=>mark.Image?.Dispose();
        branding.Controls.Add(new Label{Text="Qscreen Studio",Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleCenter,ForeColor=StudioTheme.Text,Font=new("Segoe UI",12,FontStyle.Bold)});branding.Controls.Add(mark);navigation.Controls.Add(branding);
        Button(navigation,"Layouts",Home);Button(navigation,"Mijn ontwerpen",()=>Manage("Profielen"));
        Button(navigation,"PC Monitor",()=>Manage("Historie"));Button(navigation,"Apparaten",()=>Manage("Apparaten"));
        Button(navigation,"+ Eigen ontwerp",NewDesign,true);
        var more=new ContextMenuStrip();
        foreach(string section in new[]{"Automatisch wisselen","Waarschuwingen","Design Hub"})more.Items.Add(section,null,(_,_)=>Manage(section));
        more.Items.Add("Ingebouwde layouts",null,(_,_)=>Cached("legacy",()=>new SettingsWindow()));
        Button(navigation,"Meer",()=>more.Show(navigation,new Point(navigation.Width-180,55)));
        foreach(Button button in navigation.Controls.OfType<Button>())button.Width=button.Text=="+ Eigen ontwerp"?160:button.Text=="Meer"?72:116;
        string icon=Path.Combine(directory,"branding","qprints.ico");if(File.Exists(icon))Icon=new Icon(icon);
        StudioTheme.DarkTitle(this);
        Button(context,"← Terug naar layouts",Home);Button(context,"Verder met ontwerp",()=>{if(editor!=null)ShowPage(editor);else NewDesign();});
        status.Dock=DockStyle.Bottom;Controls.Add(content);Controls.Add(context);Controls.Add(navigation);Controls.Add(status);
        gallery=new TemplatesWindow(directory){NavigateTemplate=Preview,NavigateSettings=Settings};
        FormClosing+=(_,e)=>{if(!ProtectEditor())e.Cancel=true;};
        FormClosed+=(_,_)=>{deliveryRequest++;foreach(var page in pages.Values)page.Dispose();editor?.Dispose();transient?.Dispose();gallery.Dispose();};
        Shown+=(_,_)=>Home();StudioTheme.FitWindow(this,new(1420,960),new(1050,760));
    }
    static void Button(FlowLayoutPanel parent,string text,Action action,bool primary=false)=>AdvancedUi.Button(parent,text,action,primary:primary);
    void ShowPage(Form page)
    {
        context.Visible=ReferenceEquals(page,editor)||ReferenceEquals(page,transient);
        content.SuspendLayout();foreach(Control old in content.Controls)old.Hide();
        if(page.Parent!=content){page.TopLevel=false;page.FormBorderStyle=FormBorderStyle.None;page.MinimumSize=Size.Empty;page.Dock=DockStyle.Fill;content.Controls.Add(page);}
        page.Show();page.BringToFront();content.ResumeLayout();
    }
    void Home(){ShowPage(gallery);status.Text="Kies een layout, bekijk een voorbeeld of maak je eigen ontwerp.";}
    void Cached(string key,Func<Form> create){if(!pages.TryGetValue(key,out var page)){page=create();pages.Add(key,page);}ShowPage(page);}
    void Settings(string section){if(section=="Weer")Cached("weather",()=>new WeatherSettingsWindow(directory));else Cached("hub",()=>new TemplateHubWindow(directory));}
    void Manage(string section)
    {
        if(!pages.TryGetValue("management",out var existing)){existing=new AdvancedStudioWindow(directory){EditProfileRequested=EditProfile};pages.Add("management",existing);}
        var page=(AdvancedStudioWindow)existing;page.ShowSection(section);ShowPage(page);
    }
    bool ProtectEditor()
    {
        if(editor?.HasUnsavedChanges!=true)return true;
        var answer=MessageBox.Show(this,"Je ontwerp heeft niet-opgeslagen wijzigingen. Wil je die opslaan?","Qscreen Studio",MessageBoxButtons.YesNoCancel,MessageBoxIcon.Question);
        if(answer==DialogResult.Cancel)return false;
        try{if(answer==DialogResult.Yes)editor.SaveForNavigation();else editor.DiscardForNavigation();return true;}catch(Exception ex){status.Text=ex.Message;return false;}
    }
    void NewDesign(){if(!ProtectEditor())return;OpenDesign(repository.Add(new ScreenDesign{Name="Nieuw ontwerp"}));}
    void EditProfile(string id){if(!ProtectEditor())return;OpenDesign(id);}
    void OpenDesign(string id)
    {
        editor?.DiscardForNavigation();editor?.Dispose();
        editor=new DesignWindow(directory,null,DesignFiles.Load(repository.PathFor(id)),design=>repository.Save(id,design),()=>Apply(id));
        ShowPage(editor);status.Text="Persoonlijk ontwerp · teruggaan bewaart je werk in de editor.";
    }
    void Preview(DesignTemplate template,bool apply)
    {
        transient?.Dispose();var preview=new TemplatePreviewWindow(template,apply);
        preview.ApplyRequested=()=>Apply(repository.Add(DesignSnapshot.Capture(template.Design).Restore()));
        preview.EditRequested=()=>{if(ProtectEditor())OpenDesign(repository.Add(DesignSnapshot.Capture(template.Design).Restore()));};
        transient=preview;ShowPage(preview);if(apply)preview.ApplyRequested();
    }
    async void Apply(string id)
    {
        int request=++deliveryRequest;
        try
        {
            var config=StudioConfiguration.Load(directory);config.ManualProfileId=id;config.Save(directory);
            if(pages.TryGetValue("management",out var management))((AdvancedStudioWindow)management).SetManualProfile(id);
            string revision=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(repository.PathFor(id))));var requested=DateTime.UtcNow;
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("powershell.exe","-NoProfile -Command \"Start-ScheduledTask -TaskName TempMonitor\""){UseShellExecute=false,CreateNoWindow=true});
            for(int i=0;i<=60;i++)
            {
                if(IsDisposed||request!=deliveryRequest)return;
                var delivery=ScreenDelivery.Read(directory);status.Text=ScreenDelivery.Describe(delivery,revision,requested,DateTime.UtcNow);
                if(delivery?.UpdatedUtc>=requested&&delivery.Revision==revision&&delivery.State is "received" or "invalid-design")return;
                await Task.Delay(500);
            }
        }
        catch(Exception ex){if(!IsDisposed)status.Text="Toepassen mislukt: "+ex.Message;}
    }
}

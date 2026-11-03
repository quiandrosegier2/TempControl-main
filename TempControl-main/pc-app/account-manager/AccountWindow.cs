using System.Drawing;
using System.Net.Http;
using System.Text.Json;
using System.Windows.Forms;

namespace QscreenAccounts;
sealed class AccountWindow : Form
{
    readonly AccountClient? client;readonly TabControl tabs=new(){Dock=DockStyle.Fill,DrawMode=TabDrawMode.OwnerDrawFixed,ItemSize=new(125,36),SizeMode=TabSizeMode.Fixed};
    readonly Label status=StudioTheme.Label("",10,false,85),identity=StudioTheme.Label("Niet aangemeld",12,true,56);
    readonly TextBox email=Input(),password=Input(true),registerEmail=Input(),displayName=Input(),registerPassword=Input(true),confirmPassword=Input(true),profileName=Input(),code=Input(),secret=Input(true);
    readonly CheckBox remember=new(){Text="Sessie veilig onthouden op deze Windows-account",AutoSize=true};
    readonly CheckBox consent=new(){Text="Ik ga akkoord met de voorwaarden en het privacybeleid",AutoSize=true};
    readonly List<Button> buttons=[];readonly List<Button> accountButtons=[];readonly System.Windows.Forms.Timer timer=new(){Interval=60000};
    bool busy;Guid? factor;Button? registrationAction,resetAction;
    static TextBox Input(bool hidden=false){var b=new TextBox{Width=410,UseSystemPasswordChar=hidden,MaxLength=hidden?256:254};StudioTheme.Input(b);return b;}
    public AccountWindow(AccountClient? client,string? message=null)
    {
        this.client=client;Text="Qscreen · Account";Size=new(990,820);MinimumSize=new(850,680);StartPosition=FormStartPosition.CenterScreen;BackColor=StudioTheme.Background;ForeColor=StudioTheme.Text;Font=new("Segoe UI",10);
        var brand=new Panel{Dock=DockStyle.Left,Width=275,Padding=new(26),BackColor=StudioTheme.Surface};
        var heading=StudioTheme.Label("Qscreen /",28,true,74);var tagline=StudioTheme.Label("YOUR DISPLAY.\nYOUR ACCOUNT.",17,true,95);var details=StudioTheme.Label("Een persoonlijke plek voor je Qscreen-profiel.\n\nJe wachtwoord wordt rechtstreeks door de accountprovider verwerkt. Ontwerpen en sensormetingen worden hier niet naar de cloud gestuurd.",11,false,220);
        brand.Controls.Add(details);brand.Controls.Add(tagline);brand.Controls.Add(heading);
        var body=new Panel{Dock=DockStyle.Fill,Padding=new(24)};identity.Dock=DockStyle.Top;status.Dock=DockStyle.Bottom;
        tabs.DrawItem+=(_,e)=>{using var b=new SolidBrush(e.Index==tabs.SelectedIndex?StudioTheme.Raised:StudioTheme.Background);e.Graphics.FillRectangle(b,e.Bounds);TextRenderer.DrawText(e.Graphics,tabs.TabPages[e.Index].Text,Font,e.Bounds,StudioTheme.Text,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);};
        BuildLogin();BuildRegistration();BuildProfile();BuildSecurity();body.Controls.Add(tabs);body.Controls.Add(identity);body.Controls.Add(status);Controls.Add(body);Controls.Add(brand);
        status.Text=message??"Meld je aan met je Qscreen-account. Bestaande lokale monitoring blijft onafhankelijk werken.";
        timer.Tick+=(_,_)=>{if(client?.Identity!=null&&!busy)_=Run(async()=>{await client.Refresh();await client.ReadIdentity();UpdateIdentity();},null);};
        Shown+=async(_,_)=>{if(client!=null){await Run(async()=>{if(await client.Resume()){UpdateIdentity();status.Text="Je opgeslagen sessie is online geverifieerd.";}else status.Text="Meld je aan met je Qscreen-account. Je wachtwoord wordt niet lokaal opgeslagen.";},null);timer.Start();}};
        foreach(var b in buttons)b.Enabled=client!=null;UpdateIdentity();StudioTheme.FitWindow(this,new(990,820),new(850,680));
    }
    FlowLayoutPanel Page(string name)
    {var page=new TabPage(name){BackColor=StudioTheme.Background,ForeColor=StudioTheme.Text};var flow=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoScroll=true,Padding=new(15)};page.Controls.Add(flow);tabs.TabPages.Add(page);return flow;}
    static void Field(FlowLayoutPanel p,string title,Control input){p.Controls.Add(new Label{Text=title,AutoSize=true,Margin=new(3,12,3,4),ForeColor=StudioTheme.Muted});input.Margin=new(3,2,3,8);p.Controls.Add(input);}
    Button Action(FlowLayoutPanel panel,string title,Func<Task> action,bool primary=false,bool account=false)
    {var b=StudioTheme.Button(title,primary);b.Width=300;b.Margin=new(3,12,3,4);b.Click+=async(_,_)=>await Run(action,b);buttons.Add(b);if(account)accountButtons.Add(b);panel.Controls.Add(b);return b;}
    async Task Run(Func<Task> action,Button? source)
    {
        if(busy||client==null)return;busy=true;foreach(var b in buttons)b.Enabled=false;status.ForeColor=StudioTheme.Muted;status.Text="Even wachten…";
        try{await action();}
        catch(AccountException ex){status.ForeColor=Color.Orange;status.Text=ex.Message;}
        catch(Exception ex)when(ex is HttpRequestException or OperationCanceledException){status.ForeColor=Color.Orange;status.Text="De accountserver is niet bereikbaar. Controleer je internetverbinding. Lokale Qscreen-monitoring blijft werken.";}
        catch(Exception ex)when(ex is IOException or UnauthorizedAccessException or System.Security.Cryptography.CryptographicException or JsonException or InvalidOperationException or KeyNotFoundException or FormatException){status.ForeColor=Color.Orange;status.Text="De accountaanvraag kon niet worden afgerond. Meld je opnieuw aan of probeer later opnieuw.";}
        finally{busy=false;foreach(var b in buttons)b.Enabled=true;UpdateIdentity();}
    }
    void BuildLogin()
    {
        var p=Page("Aanmelden");Field(p,"E-mailadres",email);Field(p,"Wachtwoord",password);p.Controls.Add(remember);
        Action(p,"Aanmelden",async()=>{try{await client!.Login(email.Text.Trim(),password.Text,remember.Checked);UpdateIdentity();tabs.SelectedIndex=2;status.ForeColor=StudioTheme.Green;status.Text="Aangemeld. Je identiteit en rechten zijn door de backend bevestigd.";}finally{password.Clear();}},true);
        resetAction=Action(p,"Wachtwoord vergeten",async()=>{await client!.ResetPassword(email.Text.Trim());status.Text="Als dit adres een account heeft, ontvang je een herstelmail. Open de link om een nieuw wachtwoord te kiezen.";});
        if(client!=null&&!AccountConfiguration.ValidWebsite(client.Configuration.RedirectUrl))p.Controls.Add(new Label{Text="Herstel via e-mail wacht nog op de Qscreen-accountpagina op autoslice.be.",Width=430,Height=55,ForeColor=StudioTheme.Muted});
    }
    void BuildRegistration()
    {
        var p=Page("Registreren");Field(p,"E-mailadres",registerEmail);Field(p,"Weergavenaam",displayName);Field(p,"Wachtwoord (minstens 12 tekens)",registerPassword);Field(p,"Herhaal wachtwoord",confirmPassword);p.Controls.Add(consent);
        var links=new FlowLayoutPanel{Width=430,Height=34};foreach(var pair in new[]{("Voorwaarden",client?.Configuration.TermsUrl),("Privacy",client?.Configuration.PrivacyUrl)}){var link=new LinkLabel{Text=pair.Item1,AutoSize=true,LinkColor=Color.DeepSkyBlue,Margin=new(4,5,20,4)};link.Enabled=pair.Item2!=null&&AccountConfiguration.ValidWebsite(pair.Item2);link.LinkClicked+=(_,_)=>System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(pair.Item2!){UseShellExecute=true});links.Controls.Add(link);}p.Controls.Add(links);
        registrationAction=Action(p,"Account aanmaken",async()=>{try{await client!.Register(registerEmail.Text.Trim(),displayName.Text,registerPassword.Text,confirmPassword.Text,consent.Checked);status.Text="Controleer je e-mail en bevestig je adres. Ga daarna naar Aanmelden.";tabs.SelectedIndex=0;email.Text=registerEmail.Text.Trim();}finally{registerPassword.Clear();confirmPassword.Clear();}},true);
        if(client?.Configuration.CanRegister!=true)p.Controls.Add(new Label{Text="Registratie wordt beschikbaar zodra de Qscreen-accountpagina's en privacy-informatie op autoslice.be gepubliceerd en aangesloten zijn.",Width=430,Height=74,ForeColor=StudioTheme.Muted});
    }
    void BuildProfile()
    {
        var p=Page("Mijn profiel");Field(p,"Weergavenaam",profileName);
        Action(p,"Naam bewaren",async()=>{await client!.SaveName(profileName.Text);UpdateIdentity();status.Text="Je eigen profielnaam is opgeslagen.";},true,true);
        Action(p,"Uitloggen",async()=>{try{await client!.Logout();status.Text="Uitgelogd. De opgeslagen sessie is verwijderd.";}finally{secret.Clear();factor=null;UpdateIdentity();tabs.SelectedIndex=0;}},account:true);
        p.Controls.Add(new Label{Text="Dit venster beheert je account. Je persoonlijke ontwerpen blijven lokaal; cloudsync en de officiële online bibliotheek worden afzonderlijk aangesloten.",Width=430,Height=90,Margin=new(3,24,3,4),ForeColor=StudioTheme.Muted});
    }
    void BuildSecurity()
    {
        var p=Page("Beveiliging");p.Controls.Add(new Label{Text="AUTHENTICATOR\nBevestig je tweede factor met een authenticator-app. Beheerrechten ontstaan uitsluitend na gecontroleerde backendtoewijzing.",Width=430,Height=82,ForeColor=StudioTheme.Muted});
        Action(p,"Authenticator koppelen",async()=>{if(factor!=null&&secret.Text.Length>0){status.Text="Bevestig eerst de getoonde instelsleutel met een authenticatorcode.";return;}var existing=await client!.ExistingFactor();if(existing.HasValue){factor=existing;secret.Clear();status.Text="Vul de actuele code uit je gekoppelde authenticator in.";return;}var setup=await client.Enroll();factor=setup.Id;secret.Text=setup.Secret;status.Text="Voeg de sleutel handmatig toe aan je authenticator-app en bevestig daarna een code. Deel de sleutel niet.";},account:true);
        secret.ReadOnly=true;Field(p,"Instelsleutel (alleen tijdens koppelen)",secret);var show=new CheckBox{Text="Instelsleutel tonen",AutoSize=true};show.CheckedChanged+=(_,_)=>secret.UseSystemPasswordChar=!show.Checked;p.Controls.Add(show);code.MaxLength=6;Field(p,"Zescijferige authenticatorcode",code);
        Action(p,"Code bevestigen",async()=>{if(factor==null)factor=await client!.ExistingFactor();if(factor==null)throw new AccountException("Koppel eerst een authenticator.");try{await client!.VerifyMfa(factor.Value,code.Text.Trim());secret.Clear();status.ForeColor=StudioTheme.Green;status.Text="Tweede factor bevestigd. Je actuele rol is opnieuw bij de backend gecontroleerd.";}finally{code.Clear();UpdateIdentity();}},true,true);
    }
    void UpdateIdentity()
    {
        if(IsDisposed)return;var current=client?.Identity;identity.Text=current==null?"Qscreen Account · niet aangemeld":current.DisplayName+"  ·  "+(current.Role=="admin"?"Beheerder · MFA bevestigd":"Gebruiker");
        if(current!=null&&!profileName.Focused)profileName.Text=current.DisplayName;
        foreach(var b in accountButtons)b.Enabled=client!=null&&current!=null&&!busy;
        if(registrationAction!=null)registrationAction.Enabled=!busy&&client?.Configuration.CanRegister==true;
        if(resetAction!=null)resetAction.Enabled=!busy&&client!=null&&AccountConfiguration.ValidWebsite(client.Configuration.RedirectUrl);
        if(client==null)foreach(var b in buttons)b.Enabled=false;
    }
    protected override void OnHandleCreated(EventArgs e){base.OnHandleCreated(e);StudioTheme.DarkTitle(this);}
    protected override void Dispose(bool disposing){if(disposing){timer.Dispose();password.Clear();registerPassword.Clear();confirmPassword.Clear();secret.Clear();code.Clear();}base.Dispose(disposing);}
}

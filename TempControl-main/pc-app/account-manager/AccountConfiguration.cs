using System.Text.Json;

namespace QscreenAccounts;
sealed class AccountConfiguration
{
    public const string TrustedOrigin="https://ybmesonqfclarqigwxfe.supabase.co";
    public string ProjectUrl {get;set;}=TrustedOrigin;
    public string PublishableKey {get;set;}="";
    public string TermsUrl {get;set;}="";
    public string PrivacyUrl {get;set;}="";
    public string RedirectUrl {get;set;}="";
    public bool RegistrationEnabled {get;set;}
    public bool CanRegister=>RegistrationEnabled&&ValidWebsite(TermsUrl)&&ValidWebsite(PrivacyUrl)&&ValidWebsite(RedirectUrl);
    public static bool ValidWebsite(string url)=>Uri.TryCreate(url,UriKind.Absolute,out var u)&&u.Scheme=="https"&&u.Host=="www.autoslice.be"&&u.UserInfo.Length==0&&u.IsDefaultPort;
    public void Validate(){if(ProjectUrl.TrimEnd('/')!=TrustedOrigin||!PublishableKey.StartsWith("sb_publishable_",StringComparison.Ordinal)||PublishableKey.Length>512||PublishableKey.Any(char.IsWhiteSpace))throw new InvalidDataException("Ongeldige openbare accountconfiguratie.");}
    public static AccountConfiguration Load(string path){if(new FileInfo(path).Length>16384)throw new InvalidDataException("Configuratie te groot.");var c=JsonSerializer.Deserialize<AccountConfiguration>(File.ReadAllText(path))??throw new InvalidDataException("Lege configuratie.");c.Validate();return c;}
}

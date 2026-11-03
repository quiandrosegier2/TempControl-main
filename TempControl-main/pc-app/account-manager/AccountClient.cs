using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mail;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace QscreenAccounts;
sealed record AccountIdentity(Guid Id,string DisplayName,bool Verified,bool Disabled,string Role);
sealed record MfaEnrollment(Guid Id,string Secret);
sealed class AccountSession
{
    public string AccessToken {get;set;}="";public string RefreshToken {get;set;}="";public DateTime ExpiresUtc {get;set;}
    public override string ToString()=>"Qscreen session [redacted]";
}
sealed class AccountException(string message):Exception(message);
sealed class AccountClient : IDisposable
{
    readonly AccountConfiguration config;readonly ISessionVault vault;readonly HttpClient http;readonly SemaphoreSlim refresh=new(1,1);
    AccountSession? session;bool remember;
    public AccountIdentity? Identity {get;private set;}
    public AccountConfiguration Configuration=>config;
    public AccountClient(AccountConfiguration config,ISessionVault vault,HttpMessageHandler? handler=null){config.Validate();this.config=config;this.vault=vault;http=new(handler??new HttpClientHandler{AllowAutoRedirect=false}){Timeout=TimeSpan.FromSeconds(12)};}
    static readonly JsonSerializerOptions json=new(){PropertyNameCaseInsensitive=true};
    async Task<JsonDocument> Send(HttpMethod method,string path,object? body=null,bool authenticated=false)
    {
        using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(12));using var request=new HttpRequestMessage(method,config.ProjectUrl.TrimEnd('/')+path);request.Headers.Add("apikey",config.PublishableKey);
        if(authenticated){if(session==null)throw new AccountException("Meld je eerst aan.");request.Headers.Authorization=new AuthenticationHeaderValue("Bearer",session.AccessToken);}
        if(body!=null)request.Content=new StringContent(JsonSerializer.Serialize(body),Encoding.UTF8,"application/json");
        using var response=await http.SendAsync(request,HttpCompletionOption.ResponseHeadersRead,timeout.Token);
        if(response.Content.Headers.ContentLength>131072)throw new AccountException("De accountserver gaf een te groot antwoord.");
        using var stream=await response.Content.ReadAsStreamAsync(timeout.Token);using var buffer=new MemoryStream();var bytes=new byte[4096];int count;while((count=await stream.ReadAsync(bytes,timeout.Token))>0){if(buffer.Length+count>131072)throw new AccountException("Ongeldig serverantwoord.");buffer.Write(bytes,0,count);}
        JsonDocument result;try{result=JsonDocument.Parse(buffer.Length==0?"{}":Encoding.UTF8.GetString(buffer.ToArray()),new JsonDocumentOptions{MaxDepth=24});}catch(JsonException){throw new AccountException("De accountserver gaf geen geldig antwoord.");}
        if(!response.IsSuccessStatusCode)
        {
            string code=result.RootElement.TryGetProperty("error_code",out var c)?c.GetString()??"":"";result.Dispose();
            if(authenticated&&response.StatusCode==HttpStatusCode.Unauthorized){session=null;Identity=null;vault.Clear();throw new AccountException("Je sessie is verlopen. Meld je opnieuw aan.");}
            if(response.StatusCode==HttpStatusCode.TooManyRequests)throw new AccountException("Te veel aanvragen. Wacht even en probeer opnieuw.");
            throw new AccountException(code switch{"email_not_confirmed"=>"Bevestig eerst je e-mailadres via de ontvangen e-mail.","invalid_credentials"=>"Aanmelden mislukt. Controleer je gegevens en e-mailbevestiging.","mfa_verification_failed"=>"De verificatiecode klopt niet of is verlopen.",_=>"De accountaanvraag is geweigerd. Controleer je gegevens of probeer later opnieuw."});
        }
        return result;
    }
    static AccountSession ParseSession(JsonElement e)
    {string a=e.GetProperty("access_token").GetString()??"",r=e.GetProperty("refresh_token").GetString()??"";int seconds=e.GetProperty("expires_in").GetInt32();if(a.Length is <10 or >32768||r.Length is <5 or >32768||seconds is <1 or >86400)throw new AccountException("Ongeldige sessie van de accountserver.");return new(){AccessToken=a,RefreshToken=r,ExpiresUtc=DateTime.UtcNow.AddSeconds(seconds)};}
    void Persist(){if(remember&&session!=null)vault.Save(JsonSerializer.Serialize(session));else vault.Clear();}
    public static void ValidateEmail(string email){if(email.Length>254||!MailAddress.TryCreate(email,out var parsed)||parsed.Address!=email)throw new AccountException("Vul een geldig e-mailadres in.");}
    public async Task Login(string email,string password,bool rememberSession)
    {
        ValidateEmail(email);Identity=null;session=null;vault.Clear();remember=rememberSession;
        using var response=await Send(HttpMethod.Post,"/auth/v1/token?grant_type=password",new{email,password});session=ParseSession(response.RootElement);
        try{await ReadIdentity();Persist();}catch{session=null;Identity=null;vault.Clear();throw;}
    }
    public async Task<AccountIdentity> ReadIdentity()
    {
        using var response=await Send(HttpMethod.Post,"/rest/v1/rpc/qscreen_account_status",new{},true);var rows=response.RootElement;
        if(rows.ValueKind!=JsonValueKind.Array||rows.GetArrayLength()!=1)throw new AccountException("Je accountprofiel is niet beschikbaar.");
        var row=rows[0];var identity=new AccountIdentity(row.GetProperty("user_id").GetGuid(),row.GetProperty("display_name").GetString()??"Qscreen",row.GetProperty("email_verified").GetBoolean(),row.GetProperty("disabled").GetBoolean(),row.GetProperty("account_role").GetString()??"user");
        if(identity.Disabled||!identity.Verified){session=null;Identity=null;vault.Clear();throw new AccountException(identity.Disabled?"Dit account is uitgeschakeld.":"Bevestig eerst je e-mailadres.");}
        Identity=identity;return identity;
    }
    public async Task<bool> Resume()
    {
        string? saved=vault.Load();if(saved==null)return false;try{session=JsonSerializer.Deserialize<AccountSession>(saved,json);remember=true;if(session==null||string.IsNullOrEmpty(session.RefreshToken)){vault.Clear();return false;}await Refresh(true);return true;}
        catch(AccountException){session=null;Identity=null;vault.Clear();throw;}
    }
    public async Task Refresh(bool force=false)
    {
        await refresh.WaitAsync();try{if(session==null||!force&&session.ExpiresUtc-DateTime.UtcNow>TimeSpan.FromMinutes(2))return;string token=session.RefreshToken;using var response=await Send(HttpMethod.Post,"/auth/v1/token?grant_type=refresh_token",new{refresh_token=token});session=ParseSession(response.RootElement);Persist();await ReadIdentity();}catch(AccountException){session=null;Identity=null;vault.Clear();throw;}finally{refresh.Release();}
    }
    public async Task SaveName(string name)
    {name=name.Trim();if(name.Length is <1 or >120)throw new AccountException("Naam: 1 tot 120 tekens.");await Refresh();if(Identity==null)throw new AccountException("Meld je opnieuw aan.");using var response=await Send(HttpMethod.Patch,"/rest/v1/qscreen_profiles?user_id=eq."+Identity.Id,new{display_name=name},true);await ReadIdentity();}
    public async Task Register(string email,string name,string password,string confirmation,bool accepted)
    {if(!config.CanRegister)throw new AccountException("Registratie wacht nog op de gepubliceerde account- en privacypagina's.");ValidateEmail(email);if(!accepted||password!=confirmation||password.Length<12||name.Trim().Length is <1 or >120)throw new AccountException("Controleer je naam, bevestiging en akkoord. Gebruik minstens 12 tekens voor je wachtwoord.");using var response=await Send(HttpMethod.Post,"/auth/v1/signup?redirect_to="+Uri.EscapeDataString(config.RedirectUrl),new{email,password,data=new{display_name=name.Trim()}});}
    public async Task ResetPassword(string email)
    {if(!AccountConfiguration.ValidWebsite(config.RedirectUrl))throw new AccountException("Wachtwoordherstel wacht nog op de accountpagina op autoslice.be.");ValidateEmail(email);using var response=await Send(HttpMethod.Post,"/auth/v1/recover?redirect_to="+Uri.EscapeDataString(config.RedirectUrl),new{email});}
    public async Task<MfaEnrollment> Enroll()
    {await Refresh();using var response=await Send(HttpMethod.Post,"/auth/v1/factors",new{factor_type="totp",friendly_name="Qscreen authenticator"},true);return new(response.RootElement.GetProperty("id").GetGuid(),response.RootElement.GetProperty("totp").GetProperty("secret").GetString()??throw new AccountException("Authenticator-instelling ontbreekt."));}
    public async Task<Guid?> ExistingFactor()
    {await Refresh();using var response=await Send(HttpMethod.Get,"/auth/v1/user",authenticated:true);if(!response.RootElement.TryGetProperty("factors",out var factors))return null;foreach(var f in factors.EnumerateArray())if(f.GetProperty("factor_type").GetString()=="totp"&&f.GetProperty("status").GetString()=="verified")return f.GetProperty("id").GetGuid();return null;}
    public async Task VerifyMfa(Guid factor,string code)
    {if(code.Length!=6||code.Any(c=>c<'0'||c>'9'))throw new AccountException("Vul de zescijferige authenticatorcode in.");await Refresh();using var challenge=await Send(HttpMethod.Post,$"/auth/v1/factors/{factor}/challenge",new{},true);string id=challenge.RootElement.GetProperty("id").GetString()??throw new AccountException("Verificatie kon niet starten.");using var response=await Send(HttpMethod.Post,$"/auth/v1/factors/{factor}/verify",new{challenge_id=id,code},true);session=ParseSession(response.RootElement);await ReadIdentity();Persist();}
    public async Task Logout()
    {try{if(session!=null){using var response=await Send(HttpMethod.Post,"/auth/v1/logout?scope=local",new{},true);}}finally{session=null;Identity=null;vault.Clear();remember=false;}}
    public void Dispose(){session=null;Identity=null;http.Dispose();refresh.Dispose();}
}

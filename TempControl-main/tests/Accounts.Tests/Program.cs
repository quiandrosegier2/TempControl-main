using System.Drawing;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows.Forms;
using QscreenAccounts;

static class Program
{
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static async Task Reject(Func<Task> action,string text){try{await action();}catch(AccountException){return;}throw new Exception(text);}
    [STAThread]static void Main(string[] args)
    {
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);string output=Path.GetFullPath(args[0]);Directory.CreateDirectory(output);
        Engines().GetAwaiter().GetResult();
        var vault=new WindowsSessionVault(Path.Combine(output,"test-session.dpapi"));string secret="fake-refresh-sentinel-for-encryption-test";vault.Save(secret);Check(!Encoding.UTF8.GetString(File.ReadAllBytes(Path.Combine(output,"test-session.dpapi"))).Contains(secret),"Refresh token stored as plaintext");Check(vault.Load()==secret,"DPAPI session did not roundtrip");File.WriteAllText(Path.Combine(output,"test-session.dpapi"),"corrupt");Check(vault.Load()==null,"Corrupted session accepted");
        var handler=new Provider();using var client=new AccountClient(Config(),new MemoryVault(),handler);using var window=new AccountWindow(client);window.Show();for(int i=0;i<8;i++){Application.DoEvents();Thread.Sleep(20);}using var image=new Bitmap(window.Width,window.Height);window.DrawToBitmap(image,new Rectangle(Point.Empty,image.Size));image.Save(Path.Combine(output,"qscreen-account-login.png"));window.Close();
        Console.WriteLine("PASS: login/RPC identity, email does not grant admin, bounded errors, refresh single-flight, DPAPI, logout, MFA, configuration gates and account UI.");
    }
    static AccountConfiguration Config()=>new(){PublishableKey="sb_publishable_fixture_not_a_real_key"};
    static async Task Engines()
    {
        var config=Config();Check(!config.CanRegister,"Unconfigured signup enabled");config.ProjectUrl="https://example.invalid";try{config.Validate();throw new Exception("Untrusted auth endpoint accepted");}catch(InvalidDataException){ }config=Config();
        var vault=new MemoryVault();var provider=new Provider{ExpireImmediately=true};using var client=new AccountClient(config,vault,provider);
        await client.Login("Qprints@outlook.be","fake-password",true);Check(client.Identity?.Role=="user","Admin email text granted admin");Check(vault.Value!=null,"Remember session not saved");await Task.WhenAll(Enumerable.Range(0,8).Select(_=>client.Refresh()));Check(provider.Refreshes==1,"Concurrent refresh reused rotating token");
        await client.SaveName("Own name");Check(provider.LastName=="Own name","Profile name update failed");
        var factor=await client.Enroll();Check(factor.Secret=="FAKE_SETUP_SECRET","MFA setup parsing failed");await client.VerifyMfa(factor.Id,"123456");Check(provider.MfaVerified,"MFA provider challenge not verified");Check(client.Identity?.Role=="user","MFA alone granted administrator");
        await client.Logout();Check(client.Identity==null&&vault.Value==null,"Logout retained credentials");Check(provider.Logout,"Provider logout not called");
        await Reject(()=>client.Register("a@example.invalid","Name","long-fake-password","long-fake-password",true),"Signup bypassed policy gate");await Reject(()=>client.ResetPassword("a@example.invalid"),"Recovery used missing callback");
        provider.Disabled=true;await Reject(()=>client.Login("a@example.invalid","fake-password",true),"Disabled account admitted");Check(client.Identity==null&&vault.Value==null,"Disabled session retained");provider.Disabled=false;
        provider.InvalidLogin=true;await Reject(()=>client.Login("a@example.invalid","fake-password",false),"Invalid credentials accepted");provider.InvalidLogin=false;
        await client.Login("a@example.invalid","fake-password",false);Check(vault.Value==null,"Session saved without remember opt-in");provider.Unauthorized=true;await Reject(async()=>{await client.ReadIdentity();},"Expired access token accepted");Check(client.Identity==null&&vault.Value==null,"Unauthorized request retained account identity");
        Check(!new AccountSession{AccessToken="secret-token",RefreshToken="secret-refresh"}.ToString().Contains("secret"),"Session ToString leaked secrets");
    }
    sealed class MemoryVault:ISessionVault{public string? Value;public string? Load()=>Value;public void Save(string v)=>Value=v;public void Clear()=>Value=null;}
    sealed class Provider:HttpMessageHandler
    {
        public bool Disabled,InvalidLogin,Unauthorized,ExpireImmediately,MfaVerified,Logout;public int Refreshes;public string? LastName;
        static HttpResponseMessage Response(object value,HttpStatusCode code=HttpStatusCode.OK)=>new(code){Content=new StringContent(JsonSerializer.Serialize(value),Encoding.UTF8,"application/json")};
        object Token()=>new{access_token="fake_access_token_not_real",refresh_token="fake_refresh_token_not_real",expires_in=ExpireImmediately?1:3600};
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage r,CancellationToken token)
        {
            Check(r.RequestUri!.Host=="ybmesonqfclarqigwxfe.supabase.co","Token sent outside trusted project");Check(r.Headers.Contains("apikey"),"Missing public key");string path=r.RequestUri.AbsolutePath;
            if(path.EndsWith("/token")){if(InvalidLogin)return Response(new{error_code="invalid_credentials",message="sensitive provider content must never be surfaced"},HttpStatusCode.BadRequest);if(r.RequestUri.Query.Contains("refresh_token")){Refreshes++;ExpireImmediately=false;await Task.Delay(25,token);}return Response(Token());}
            Check(r.Headers.Authorization?.Scheme=="Bearer","Protected endpoint missing bearer");
            if(Unauthorized)return Response(new{error_code="bad_jwt"},HttpStatusCode.Unauthorized);
            if(path.EndsWith("qscreen_account_status"))return Response(new[]{new{user_id="b028bc4e-8df0-4b29-959d-000000000001",display_name="Fixture User",email_verified=true,disabled=Disabled,account_role="user"}});
            if(path.EndsWith("qscreen_profiles")){using var d=JsonDocument.Parse(await r.Content!.ReadAsStringAsync(token));LastName=d.RootElement.GetProperty("display_name").GetString();return new(HttpStatusCode.NoContent);}
            if(path.EndsWith("/logout")){Logout=true;return new(HttpStatusCode.NoContent);}
            if(path.EndsWith("/factors"))return Response(new{id="b028bc4e-8df0-4b29-959d-000000000005",totp=new{secret="FAKE_SETUP_SECRET"}});
            if(path.EndsWith("/challenge"))return Response(new{id="b028bc4e-8df0-4b29-959d-000000000006"});
            if(path.EndsWith("/verify")){MfaVerified=true;return Response(Token());}
            throw new Exception("Unexpected provider call: "+path);
        }
    }
}

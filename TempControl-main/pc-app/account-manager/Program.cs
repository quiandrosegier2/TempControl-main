using System.Security.Principal;
using System.Windows.Forms;

namespace QscreenAccounts;
static class Program
{
    [STAThread]static void Main()
    {
        Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
        using var identity=WindowsIdentity.GetCurrent();
        if(new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator))
        {Application.Run(new AccountWindow(null,"Open Qscreen Account via Start zonder 'Als administrator uitvoeren'. Het accountvenster verwerkt geen wachtwoorden met beheerdersrechten."));return;}
        using var instance=new Mutex(false,@"Global\Qscreen.Accounts."+identity.User?.Value,out bool first);
        if(!first)return;
        try{var config=AccountConfiguration.Load(Path.Combine(AppContext.BaseDirectory,"account-client.json"));using var client=new AccountClient(config,new WindowsSessionVault());Application.Run(new AccountWindow(client));}
        catch(Exception ex)when(ex is IOException or InvalidDataException or System.Text.Json.JsonException or UnauthorizedAccessException){Application.Run(new AccountWindow(null,"De openbare accountconfiguratie ontbreekt of is ongeldig. Installeer Qscreen Account opnieuw."));}
    }
}

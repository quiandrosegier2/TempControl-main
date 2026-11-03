using System.Runtime.InteropServices;

static class AccountLauncher
{
    public static void Open(string appDirectory)
    {
        string executable=Path.Combine(Path.GetFullPath(appDirectory),"accounts","Qscreen.Accounts.exe");
        if(!File.Exists(executable))throw new FileNotFoundException("Qscreen Account ontbreekt. Installeer de nieuwe Qscreen-versie.");
        // Use the desktop shell to launch in the interactive user's context.
        // The account executable independently rejects elevated execution.
        object? shell=null;
        try{var type=Type.GetTypeFromProgID("Shell.Application")??throw new InvalidOperationException("Open Qscreen Account via Start.");shell=Activator.CreateInstance(type);((dynamic)shell!).ShellExecute(executable,"","","open",1);}
        finally{if(shell!=null&&Marshal.IsComObject(shell))Marshal.FinalReleaseComObject(shell);}
    }
}

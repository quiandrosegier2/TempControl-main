using System.Collections.Concurrent;
using System.Drawing;
using System.Windows.Forms;

sealed class DesktopAlerts : IDisposable
{
    readonly ConcurrentQueue<string> messages=new();
    readonly Thread thread;
    volatile bool stopping;
    public DesktopAlerts()
    {
        thread=new Thread(()=>
        {
            try{
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException,true);
            using var context=new ApplicationContext();using var icon=new NotifyIcon{Icon=SystemIcons.Warning,Text="Qscreen meldingen",Visible=false};DateTime shownUntil=DateTime.MinValue;
            using var timer=new System.Windows.Forms.Timer{Interval=500};
            timer.Tick+=(_,_)=>{if(stopping){context.ExitThread();return;}if(messages.TryDequeue(out var text)){icon.Visible=true;shownUntil=DateTime.UtcNow.AddSeconds(8);icon.BalloonTipTitle="Qscreen · systeemmelding";icon.BalloonTipText=text;icon.BalloonTipIcon=ToolTipIcon.Warning;icon.ShowBalloonTip(5000);}else if(DateTime.UtcNow>shownUntil)icon.Visible=false;};
            timer.Start();Application.Run(context);icon.Visible=false;
            }catch(Exception ex)when(ex is System.ComponentModel.Win32Exception or InvalidOperationException){Log.Write("Windowsmeldingen niet beschikbaar: "+ex.Message);}
        }){IsBackground=true,Name="Qscreen desktop alerts"};thread.SetApartmentState(ApartmentState.STA);thread.Start();
    }
    public void Notify(string text){if(messages.Count<16)messages.Enqueue(text[..Math.Min(text.Length,200)]);}
    public void Dispose(){stopping=true;thread.Join(1500);}
}

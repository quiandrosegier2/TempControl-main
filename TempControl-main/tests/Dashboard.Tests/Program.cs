using System.Drawing;
using System.Reflection;
using System.Text.Json;
using System.Windows.Forms;

static class Program
{
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    [STAThread]static void Main(string[] args){try{Run(args);}catch(Exception ex){Console.Error.WriteLine(ex);Environment.ExitCode=1;}}
    static void Run(string[] args)
    {
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);Application.EnableVisualStyles();string output=Path.GetFullPath(args[0]);Directory.CreateDirectory(output);
        var now=DateTime.Now;var hardware=new HardwareTelemetry{CpuModel="TEST CPU — synthetic fixture",GpuModel="TEST GPU — synthetic fixture",CpuPowerWatts=73,GpuPowerWatts=145,CpuSessionMax=68,GpuMemoryClockMhz=7000,VramUsedGb=5.4f,VramTotalGb=12,RamInstalledGb=32,RamUsedGb=18,RamAvailableGb=13.2f,RamConfiguredClockMhz=3200,AverageFps=141,FrameTimeMs=7.1f,FrameProcess="test-fixture",UptimeSeconds=12345,FreeDisks=[new("TEST C:",123,"GiB vrij")],Fans=[new("TEST CPU fan",1100,"RPM")]};
        var readings=new ScreenReadings(57,63,144,42,77,58,now,4200,1800,Hardware:hardware);LiveTelemetry.Write(output,readings);
        var actual=LiveTelemetry.Read(output,DateTime.UtcNow);Check(actual?.Readings.Hardware?.RamUsedGb==18&&actual.Readings.Cpu==57,"Enriched snapshot does not retain shared display readings");
        var history=new SensorHistory();for(int i=0;i<3600;i++)history.Add(readings with{Now=now.AddSeconds(i-3600),Cpu=40+i%20,Fps=i%10==0?null:144});
        AtomicJson.Write(Path.Combine(output,"dashboard-history.json"),new DashboardHistorySnapshot(DateTime.UtcNow,5,SensorHistory.Metrics.ToDictionary(m=>m,m=>history.Window(m,5,now,240)),SensorHistory.Metrics.ToDictionary(m=>m,m=>history.Window(m,1,now,120))));
        Check(history.Window(WidgetKind.FrameTime,5,now).Average==7.1f,"Frame time history missing");
        using var form=new Form{Size=new(1440,980),StartPosition=FormStartPosition.Manual,Location=new(0,0)};using var dashboard=new MonitorDashboard(output,Path.Combine(output,"preferences.json"));form.Controls.Add(dashboard);form.Show();Application.DoEvents();dashboard.RefreshNow();
        var flags=BindingFlags.Instance|BindingFlags.NonPublic;var cards=(Dictionary<string,MonitorCard>)typeof(MonitorDashboard).GetField("cards",flags)!.GetValue(dashboard)!;
        Check(cards["cpu"].Content.Rows.Any(r=>r.Value=="4,20 GHz"||r.Value=="4.20 GHz"),"CPU MHz to GHz conversion incorrect");Check(cards["gaming"].Content.Value=="144","FPS changed in dashboard");Check(!cards["network"].Visible,"Unsupported optional network card is shown");
        foreach(var test in new[]{(Width:1440,Columns:4),(Width:820,Columns:2),(Width:480,Columns:1)})
        {
            form.ClientSize=new(test.Width,950);Application.DoEvents();dashboard.RefreshNow();var main=new[]{"cpu","gpu","ram","gaming"}.Select(id=>cards[id]).ToArray();Check(main.Select(c=>c.Top).Distinct().Count()==4/test.Columns,"Responsive columns incorrect at "+test.Width);Check(main.All(c=>c.Width>0&&c.Left>=0),"Invalid card bounds");
            using var capture=new Bitmap(form.Width,form.Height);form.DrawToBitmap(capture,new Rectangle(0,0,form.Width,form.Height));capture.Save(Path.Combine(output,$"dashboard-{test.Width}.png"));
        }
        LiveTelemetry.Write(output,readings with{Hardware=hardware with{FrameProcess=null}});dashboard.RefreshNow();Check(cards["gaming"].Content.Model=="Procesnaam niet beschikbaar","Unknown process mislabeled as no FPS capture");
        LiveTelemetry.Write(output,readings with{Fps=null,Hardware=null});dashboard.RefreshNow();Check(cards["gaming"].Content.Value=="Niet beschikbaar"&&cards["gaming"].Content.Model=="Geen game gedetecteerd","Missing FPS falsely retained");
        AtomicJson.Write(Path.Combine(output,"live-telemetry.json"),new LiveTelemetry(DateTime.UtcNow.AddMinutes(-1),readings));dashboard.RefreshNow();Check(cards["cpu"].Content.Value=="Niet beschikbaar","Stale sensor values shown as live");
        Check(FpsMonitor.CsvFields("\"game, test.exe\",42,\"a\"\"b\"").SequenceEqual(new[]{"game, test.exe","42","a\"b"}),"PresentMon quoted CSV parsing failed");
        typeof(MonitorDashboard).GetMethod("MoveCard",flags)!.Invoke(dashboard,new object[]{"gpu",-1});
        var preferences=JsonSerializer.Deserialize<MonitorPreferences>(File.ReadAllText(Path.Combine(output,"preferences.json")),DesignFiles.Json)!;Check(preferences.Order[0]=="gpu","Reordered cards were not persisted");
        var roundtrip=DesignFiles.Parse(DesignFiles.Serialize(ScreenDesign.CreateDefault()));Check(roundtrip.Widgets.Count>0,"Existing designs no longer load");
        Console.WriteLine("PASS: same-source snapshots, GHz conversion, frame-time history, unsupported/stale values, responsive 4/2/1 cards, old projects; synthetic screenshots saved.");
    }
}

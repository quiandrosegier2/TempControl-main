using System.Collections.Concurrent;
using System.Drawing;
using System.IO.Compression;
using System.Windows.Forms;

static class Program
{
    static void Assert(bool value,string message){if(!value)throw new Exception(message);}
    static void Reject(Action action,string message){try{action();}catch(Exception ex)when(ex is IOException or InvalidDataException or ArgumentException or System.Text.Json.JsonException){return;}throw new Exception(message);}
    [STAThread]
    static void Main(string[] args)
    {
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);
        string directory=Path.Combine(Path.GetFullPath(args[0]),"run-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);Console.WriteLine("Artifacts: "+directory);
        DateTime now=new(2026,11,3,12,0,0,DateTimeKind.Utc);
        bool sensorFailure=true;var sensorLogs=new List<string>();
        var sampler=new SensorSampler(()=>sensorFailure?throw new IOException("Temporary driver failure"):ScreenReadings.Demo,sensorLogs.Add);
        Assert(sampler.Read(now).Cpu==null&&sampler.Read(now).Fps==null,"Failed sampler reused stale readings");
        Assert(sensorLogs.Count==1,"Repeated driver failure flooded the log");
        sensorFailure=false;Assert(sampler.Read(now).Cpu==42&&sensorLogs.Count==2,"Sampler did not recover");
        sampler.Read(now);Assert(sensorLogs.Count==2,"Healthy sampling logged repeatedly");
        sensorFailure=true;sampler.Read(now);Assert(sensorLogs.Count==3,"New failure was not logged after recovery");
        bool Exists(string id)=>id is "legacy" or "gaming" or "clock";
        var config=new StudioConfiguration{AutomaticProfiles=true,SwitchDelaySeconds=3,Rules=[new(){Trigger=ProfileTrigger.Running,Executable="Warzone.exe",ProfileId="gaming"},new(){Trigger=ProfileTrigger.Idle,IdleSeconds=60,ProfileId="clock",Priority=20}]};
        var selector=new ProfileEngine();var normal=new ActivitySnapshot("code",new HashSet<string>(),TimeSpan.Zero);
        Assert(selector.Select(config,normal,now,Exists)=="legacy","Default profile not selected");
        var game=normal with{Running=new HashSet<string>{"warzone"}};
        Assert(selector.Select(config,game,now.AddSeconds(1),Exists)=="legacy","Switch delay ignored");
        Assert(selector.Select(config,game,now.AddSeconds(4),Exists)=="gaming","Running executable not detected");
        Assert(selector.Select(config,normal,now.AddSeconds(5),Exists)=="gaming","Return delay ignored");
        Assert(selector.Select(config,normal,now.AddSeconds(8),Exists)=="legacy","Did not return to default after exit");
        config.ManualProfileId="clock";Assert(selector.Select(config,game,now.AddSeconds(9),Exists)=="clock","Manual override ignored");config.ManualProfileId="";
        var idle=normal with{Idle=TimeSpan.FromMinutes(5)};selector.Select(config,idle,now.AddSeconds(10),Exists);Assert(selector.Select(config,idle,now.AddSeconds(13),Exists)=="clock","Idle rule ignored");
        config.Rules.Add(new(){Trigger=ProfileTrigger.Foreground,Executable="CODE.EXE",ProfileId="gaming",Priority=50});config.SwitchDelaySeconds=0;Assert(selector.Select(config,normal,now,Exists)=="gaming","Foreground rule is not case-insensitive");

        var brightness=new BrightnessPolicy{Scheduled=true,DayStartHour=22,NightStartHour=6,DayBrightness=80,NightBrightness=20};
        Assert(brightness.Evaluate(now.Date.AddHours(23),TimeSpan.Zero)==80&&brightness.Evaluate(now.Date.AddHours(3),TimeSpan.Zero)==80&&brightness.Evaluate(now.Date.AddHours(12),TimeSpan.Zero)==20,"Overnight schedule incorrect");
        brightness.DimWhenIdle=true;brightness.Minimum=20;brightness.SleepWhenIdle=true;Assert(brightness.Evaluate(now,TimeSpan.FromHours(1))==0,"Sleep did not override minimum brightness");
        Assert(brightness.Evaluate(now.Date.AddHours(23),TimeSpan.Zero)==80,"Activity did not restore scheduled brightness");

        var history=new SensorHistory();
        for(int i=0;i<7200;i++)history.Add(new(i%100,50,144,20,30,60,now.AddSeconds(i),4500,2100));
        Assert(history.Count==3601,"History buffer is not bounded to one hour");
        var series=history.Window(WidgetKind.CpuClock,60,now.AddSeconds(7199));Assert(series.Points.Count<=120&&series.Minimum==4500&&series.Average==4500,"History downsampling or statistics failed");
        history.Add(new(null,null,null,null,null,null,now.AddSeconds(7200)));Assert(history.Window(WidgetKind.CpuTemperature,1,now.AddSeconds(7200)).Points.Any(p=>p.Value==null),"Unavailable readings did not leave graph gaps");

        int notifications=0;var alerts=new AlertEngine{Notify=_=>notifications++};var rule=new AlertRule{Threshold=80,Hysteresis=3,CooldownSeconds=60,DesktopNotification=true};
        ScreenReadings R(float? cpu)=>new(cpu,50,100,20,30,60,now);
        Assert(alerts.Evaluate([rule],R(81),true,now).Count==1&&notifications==1,"Alert did not activate");
        Assert(alerts.Evaluate([rule],R(79),true,now.AddSeconds(2)).Count==1&&notifications==1,"Alert hysteresis or cooldown failed");
        Assert(alerts.Evaluate([rule],R(76),true,now.AddSeconds(20)).Count==0,"Alert did not clear below hysteresis threshold");
        alerts.Evaluate([rule],R(90),true,now.AddSeconds(30));Assert(notifications==1,"Alert repeated during cooldown");
        alerts.Evaluate([rule],R(90),true,now.AddSeconds(61));Assert(notifications==2,"Alert cooldown never expired");
        rule.Condition=AlertCondition.Unavailable;Assert(alerts.Evaluate([rule],R(null),true,now.AddSeconds(62)).Count>0,"Unavailable sensor alert missing");

        var design=ScreenDesign.CreateDefault();design.Name="Root";var second=ScreenDesign.CreateDefault();second.Name="Second";design.Pages.Add(DisplayPage.From(second));design.TouchZones.Add(new(){Action=TouchAction.GoToPage,Page=1});
        var roundtrip=DesignFiles.Parse(DesignFiles.Serialize(design));Assert(roundtrip.Pages.Count==1&&roundtrip.TouchZones.Count==1,"Multipage project failed roundtrip");
        byte[] gif=[71,73,70,56,57,97,1,0,1,0,128,0,0,0,0,0,255,255,255,33,249,4,0,10,0,0,0,44,0,0,0,0,1,0,1,0,0,2,2,68,1,0,33,249,4,0,10,0,0,0,44,0,0,0,0,1,0,1,0,0,2,2,76,1,0,59];
        var animated=DesignFiles.Parse(DesignFiles.Serialize(new ScreenDesign{BackgroundPng=Convert.ToBase64String(gif),Darken=0}));
        using(var renderer=new DesignRenderer()){using var firstFrame=renderer.Render(animated,new(null,null,null,null,null,null,now));using var nextFrame=renderer.Render(animated,new(null,null,null,null,null,null,now.AddMilliseconds(150)));Assert(renderer.HasAnimatedBackground&&firstFrame.GetPixel(206,206)!=nextFrame.GetPixel(206,206),"Animated GIF was flattened");}
        Assert(DesignFiles.Parse("{\"Widgets\":[]}").Pages.Count==0,"Legacy single-page project no longer opens");
        var pages=new PageController();design.RotatePages=true;design.RotationSeconds=3;pages.Tick(design,now);pages.Tick(design,now.AddSeconds(3));Assert(pages.Index==1,"Page rotation failed");pages.Go(-1,2,now);Assert(pages.Index==1,"Previous-page wrap failed");
        DisplayPages.Reorder(roundtrip,1,0);Assert(roundtrip.Name=="Root"&&DisplayPages.At(roundtrip,0).Name=="Second"&&roundtrip.Pages[0].Name=="Root","Page reorder changed profile name or lost content");Assert(roundtrip.Pages[0].TouchZones[0].Page==0,"Page reorder broke a touch-zone target");
        string package=Path.Combine(directory,"example.qscreenpack");QscreenPackage.Export(package,design,new(){Name="Example",Category=TemplateCategory.Gaming});var imported=QscreenPackage.Import(package);Assert(imported.Design.Pages.Count==1,"Package lost pages");
        string malicious=Path.Combine(directory,"bad.qscreenpack");using(var zip=ZipFile.Open(malicious,ZipArchiveMode.Create)){using var writer=new StreamWriter(zip.CreateEntry("../escaped.txt").Open());writer.Write("unsafe");}
        Reject(()=>QscreenPackage.Import(malicious),"Path traversal package accepted");Assert(!File.Exists(Path.Combine(directory,"..","escaped.txt")),"Package wrote outside library");
        var repository=new ProfileRepository(directory);string first=repository.Add(design),duplicate=repository.Add(design);Assert(first!=duplicate&&repository.List().Count==2,"Import overwrote an existing profile");Reject(()=>repository.PathFor("../bad"),"Unsafe profile path accepted");
        var library=new LocalTemplateLibrary(directory);library.EnsureExamples();Assert(library.SearchAsync("",TemplateCategory.Clock).Result.Count==1,"Local category filter failed");Assert(library.SearchAsync("Cyberpunk",null).Result.Count==1,"Local search failed");
        WorkerTests(directory,design,now).GetAwaiter().GetResult();Console.WriteLine("PASS: advanced engines, packages and independent workers");

        Application.EnableVisualStyles();config=new StudioConfiguration();config.Save(directory);
        using(var manager=new AdvancedStudioWindow(directory))
        {
            manager.Show();Application.DoEvents();using var image=new Bitmap(manager.Width,manager.Height);manager.DrawToBitmap(image,new Rectangle(Point.Empty,manager.Size));image.Save(Path.Combine(directory,"advanced-manager.png"));
            var tab=(TabControl)typeof(AdvancedStudioWindow).GetField("tabs",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.GetValue(manager)!;
            for(int i=0;i<tab.TabCount;i++){tab.SelectedIndex=i;Application.DoEvents();using var shot=new Bitmap(manager.Width,manager.Height);manager.DrawToBitmap(shot,new Rectangle(Point.Empty,manager.Size));shot.Save(Path.Combine(directory,$"manager-tab-{i}.png"));}
            var unchanged=(StudioConfiguration)typeof(AdvancedStudioWindow).GetField("config",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.GetValue(manager)!;
            Assert(unchanged.Alerts.Count==0&&unchanged.Rules.Count==0,"Visiting a tab created active rules without user action");
            manager.Close();
        }
        using(var manager=new PagesWindow(directory,design)){manager.Show();Application.DoEvents();using var image=new Bitmap(manager.Width,manager.Height);manager.DrawToBitmap(image,new Rectangle(Point.Empty,manager.Size));image.Save(Path.Combine(directory,"pages.png"));manager.Close();}
        using(var hub=new TemplateHubWindow(directory)){hub.Show();var watch=System.Diagnostics.Stopwatch.StartNew();var full=typeof(TemplateHubWindow).GetField("fullTemplate",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!;while(full.GetValue(hub)==null&&watch.ElapsedMilliseconds<4000){Application.DoEvents();Thread.Sleep(10);}Assert(full.GetValue(hub)!=null,"Asynchronous library preview did not load");using var image=new Bitmap(hub.Width,hub.Height);hub.DrawToBitmap(image,new Rectangle(Point.Empty,hub.Size));image.Save(Path.Combine(directory,"library.png"));hub.Close();}
        Console.WriteLine("PASS: delayed/foreground/running/idle profiles; overrides; overnight brightness/sleep; bounded history/statistics/gaps; alert hysteresis/cooldown; legacy/multipage projects; safe packages; independent workers/dedup/reconnect; local library and management UI.");
    }
    static async Task WorkerTests(string directory,ScreenDesign design,DateTime now)
    {
        design=DesignSnapshot.Capture(design).Restore();design.RotatePages=false;design.Pages[0].TouchZones.Add(new(){Action=TouchAction.NextPage});
        var reports=new ConcurrentQueue<DeviceRuntimeStatus>();using var gate=new ManualResetEventSlim(false);var slow=new FakeSession("one",gate);var fast=new FakeSession("two",null);
        using var a=new DeviceWorker(new("COM90","one","Slow"),directory,false,reports.Enqueue,_=>{},(_,_)=>slow);
        using var b=new DeviceWorker(new("COM91","two","Fast"),directory,false,reports.Enqueue,_=>{},(_,events)=>{fast.OnEvent=events;return fast;});
        var readings=new ScreenReadings(42,55,144,20,30,50,now);var job=new DisplayJob(new(),"test",design,"revision",readings,new("",new HashSet<string>(),TimeSpan.Zero),"CPU=42;GPU=55;FPS=144");
        a.Post(job);b.Post(job);await Wait(()=>fast.Sent>0);Assert(slow.Sent==0,"Blocked transfer unexpectedly completed");gate.Set();await Wait(()=>slow.Sent>0);
        int sent=fast.Sent;b.Post(job);await Wait(()=>fast.Alive>0);Assert(fast.Sent==sent,"Unchanged design caused unnecessary USB frame");
        fast.Events.Enqueue("EVENT;NEXT;300;200");b.Post(job);await Wait(()=>reports.Any(r=>r.DeviceId=="two"&&r.Page==1));
        var sleepConfig=new StudioConfiguration{Devices=[new(){DeviceId="two",Brightness=new(){DimWhenIdle=true,SleepWhenIdle=true,IdleSeconds=1}}]};
        var sleeping=job with{Config=sleepConfig,Activity=job.Activity with{Idle=TimeSpan.FromMinutes(10)}};
        await Task.Delay(1100);b.Post(sleeping);await Wait(()=>fast.LastControl.Contains("B=0;",StringComparison.Ordinal));
        fast.Events.Enqueue("EVENT;TAP;320;200");b.Post(sleeping);await Wait(()=>fast.LastControl.Contains("B=100;",StringComparison.Ordinal));Assert(reports.Last(r=>r.DeviceId=="two").Page==1,"Wake tap also navigated a page");
        int beforeAnimation=fast.Sent;b.Post(job with{ProfileId="animated",Design=new(){Animation=PageAnimation.Orbit}});await Wait(()=>fast.Sent>=beforeAnimation+2);
        a.Dispose();b.Dispose();await Task.WhenAll(a.Completion,b.Completion).WaitAsync(TimeSpan.FromSeconds(3));
        int attempts=0;var recovered=new FakeSession("retry",null);using var retry=new DeviceWorker(new("COM92","retry","Retry"),directory,false,reports.Enqueue,_=>{},(_,_)=>Interlocked.Increment(ref attempts)==1?throw new IOException("SIMULATED_DISCONNECT"):recovered);
        retry.Post(job);await Wait(()=>reports.Any(r=>r.Message=="SIMULATED_DISCONNECT"));await Task.Delay(12100);retry.Post(job);await Wait(()=>recovered.Sent>0);retry.Dispose();await retry.Completion.WaitAsync(TimeSpan.FromSeconds(3));
    }
    static async Task Wait(Func<bool> condition){for(int i=0;i<150&&!condition();i++)await Task.Delay(20);Assert(condition(),"Timed out waiting for worker");}
    sealed class FakeSession(string id,ManualResetEventSlim? gate) : IDisplaySession
    {
        public int Sent,Alive;public DeviceInformation Information=>new(id,"2.0.0",true,true);
        public ConcurrentQueue<string> Events=new();public Action<string>? OnEvent;public string LastControl="";
        public bool Send(byte[] frame){gate?.Wait(TimeSpan.FromSeconds(10));Interlocked.Increment(ref Sent);return true;}
        public void Native(string packet){ }
        public void Configure(string command)=>LastControl=command;
        public void KeepAlive()=>Interlocked.Increment(ref Alive);
        public void Poll(){while(Events.TryDequeue(out var packet))OnEvent?.Invoke(packet);}
        public void Dispose()=>gate?.Set();
    }
}

using System.Collections.Concurrent;
using System.Drawing;
using System.Globalization;
using System.Text.Json;
using System.Threading.Channels;
using System.Management;
using System.Diagnostics;

sealed record HistoryRequest(WidgetKind Metric,int Minutes,DateTime UpdatedUtc);
sealed record HistoryResponse(WidgetKind Metric,int Minutes,DateTime UpdatedUtc,HistorySeries Series);
sealed record DisplayJob(StudioConfiguration Config,string ProfileId,ScreenDesign? Design,string Revision,ScreenReadings Readings,ActivitySnapshot Activity,string NativePacket);
sealed class DeviceWorker : IDisposable
{
    readonly DeviceCandidate candidate;
    readonly string directory;
    volatile bool primary;
    public bool Primary { get=>primary;set=>primary=value; }
    readonly Func<DeviceCandidate,Action<string>,IDisplaySession> open;
    readonly Action<DeviceRuntimeStatus> report;
    readonly Action<string> notify;
    readonly Channel<DisplayJob> jobs=Channel.CreateBounded<DisplayJob>(new BoundedChannelOptions(1){FullMode=BoundedChannelFullMode.DropOldest,SingleReader=true,SingleWriter=true});
    readonly CancellationTokenSource stopped=new();
    readonly PageController pages=new();readonly AlertEngine alerts=new();readonly AnimationComposer composer=new();
    IDisplaySession? session;
    DisplayJob? current;
    string profile="",lastConfig="",pageRequest="";
    uint lastHash;
    DateTime lastSent=DateTime.MinValue,retryAfter,quickMenuUntil;
    int lastPage=-1;
    bool dimmed,receivedFrame,everConnected;
    int lastBrightness=100;
    DateTime lastTouch=DateTime.MinValue;
    Bitmap? previous;
    long replacementBaseline;
    AnimationStreamMeter streamMeter=new();string measuredRevision="",progressRevision="";
    public Task Completion { get; }
    public DeviceWorker(DeviceCandidate candidate,string directory,bool primary,Action<DeviceRuntimeStatus> report,Action<string> notify,Func<DeviceCandidate,Action<string>,IDisplaySession>? open=null)
    {
        this.candidate=candidate;this.directory=directory;this.primary=primary;this.report=report;this.notify=notify;this.open=open??((c,e)=>new DisplaySession(c,e));
        alerts.Notify=n=>notify(candidate.Label+": "+n.Name);Completion=Task.Run(RunAsync);
    }
    public void Post(DisplayJob job)=>jobs.Writer.TryWrite(job);
    async Task RunAsync()
    {
        try
        {
            DisplayJob? pending=null;
            while(!stopped.IsCancellationRequested)
            {
                pending??=await jobs.Reader.ReadAsync(stopped.Token);
                while(jobs.Reader.TryRead(out var newest))pending=newest;
                var job=pending;pending=null;bool repeatAnimation=false;var frameClock=Stopwatch.StartNew();
                current=job;if(DateTime.UtcNow<retryAfter)continue;
                try
                {
                    if(session==null){session=open(candidate,TouchEvent);everConnected=true;receivedFrame=false;progressRevision="";measuredRevision="";streamMeter=new();if(stopped.IsCancellationRequested){session.Dispose();session=null;break;}}
                    session.Poll();
                    var assignment=job.Config.Devices.FirstOrDefault(d=>d.DeviceId==session.Information.Id||d.DeviceId==candidate.UsbId)??new DeviceAssignment{DeviceId=session.Information.Id,Name=candidate.Label};
                    TimeSpan idle=job.Activity.Idle,touchIdle=DateTime.UtcNow-lastTouch;if(touchIdle<idle)idle=touchIdle;
                    int brightness=assignment.Brightness.Evaluate(job.Readings.Now,idle);if(dimmed)brightness=Math.Min(brightness,15);lastBrightness=brightness;
                    var t=assignment.Touch;
                    string command=$"QSET;B={brightness};T={(t.Enabled?1:0)};SW={t.SwipePixels};HOLD={t.LongPressMs};MIN={t.MinimumWeight};ROT={t.Rotation};FB={(t.Feedback?1:0)};MENU={(t.QuickMenu?1:0)}";
                    if(command!=lastConfig){session.Configure(command);lastConfig=command;}
                    if(profile!=job.ProfileId){profile=job.ProfileId;pages.Go(0,job.Design==null?1:DisplayPages.Count(job.Design),DateTime.UtcNow);lastPage=-1;}
                    int count=job.Design==null?1:DisplayPages.Count(job.Design);
                    if(assignment.PageRequest!=pageRequest){pageRequest=assignment.PageRequest;if(assignment.RequestedPage>=0)pages.Go(assignment.RequestedPage,count,DateTime.UtcNow);}
                    var notices=alerts.Evaluate(job.Config.Alerts,job.Readings,true,DateTime.UtcNow);
                    if(brightness==0&&session.Information.Advanced)
                    {
                        composer.Pause();session.KeepAlive();if(primary)ScreenDelivery.Write(directory,job.Revision,"sleeping",assignment.Name);
                        report(new(session.Information.Id,assignment.Name,candidate.Port,true,session.Information.Firmware,job.ProfileId,job.Design?.Name??"Ingebouwde layout",pages.Index,count,0,"Slaapstand · tik op het scherm of gebruik de pc om te wekken",DateTime.UtcNow,candidate.UsbId,session.Information.Touch,session.Information.Advanced));continue;
                    }
                    var readings=job.Readings with{Alerts=notices};
                    var renderProject=job.Design;
                    if(renderProject==null&&notices.Any(n=>n.FullScreen))renderProject=new ScreenDesign{Name="Systeemwaarschuwing",Darken=0};
                    if(renderProject!=null)
                    {
                        pages.Tick(renderProject,DateTime.UtcNow);int renderedPage=pages.Index;var page=DisplayPages.At(renderProject,renderedPage);
                        if(measuredRevision!=job.Revision){streamMeter=new();replacementBaseline=composer.ReplacedFrames;measuredRevision=job.Revision;}
                        long composeRequest=composer.Submit(job.Revision+":"+renderedPage,page,readings);
                        using var composed=await composer.Next(composeRequest,stopped.Token);
                        var image=composed.Image;
                        repeatAnimation=composed.Animated;
                        if(DateTime.UtcNow<quickMenuUntil)DrawQuickMenu(image);
                        byte[] bytes=DateTime.UtcNow<quickMenuUntil?DesignRenderer.Rgb565(image):composed.Bytes;uint hash=CustomScreenSender.Hash(bytes);
                        bool changedPage=lastPage>=0&&lastPage!=renderedPage;
                        if(changedPage&&hash!=lastHash&&renderProject.Transition!=PageTransition.Cut&&session.Information.Advanced&&previous!=null)
                        {
                            using var transition=Transition(previous,image,renderProject.Transition);session.Send(DesignRenderer.Rgb565(transition));
                        }
                        bool sent=true;
                        if(!receivedFrame||hash!=lastHash||!session.Information.Advanced&&DateTime.UtcNow-lastSent>TimeSpan.FromSeconds(2))
                        {
                            if(primary)ScreenDelivery.Write(directory,job.Revision,"sending",assignment.Name);
                            var transferClock=Stopwatch.StartNew();
                            if(primary&&progressRevision!=job.Revision&&session is IProgressDisplaySession progressSession)
                            {int published=-10;sent=progressSession.SendWithProgress(bytes,percent=>{if(percent>=published+10||percent==100){published=percent;ScreenDelivery.Write(directory,job.Revision,"sending",percent+"% van dit beeld bevestigd · "+assignment.Name);}});if(sent)progressRevision=job.Revision;}
                            else sent=session.Send(bytes);
                            transferClock.Stop();if(sent)streamMeter.Received(composed.RenderMs,transferClock.Elapsed.TotalMilliseconds,session is DisplaySession physical?physical.LastPayloadBytes:bytes.Length,page.StreamingQuality,composed.GifFrame,hash,composed.LayerFrames,composed.DecodeMs,Math.Max(0,composer.ReplacedFrames-replacementBaseline));
                            receivedFrame=sent;lastHash=hash;lastSent=DateTime.UtcNow;
                        }
                        else if(session.Information.Advanced)session.KeepAlive();
                        if(!sent)session.Native(job.NativePacket);
                        if(!sent)repeatAnimation=false;
                        if(primary)ScreenDelivery.Write(directory,job.Revision,sent?"received":"firmware-required",assignment.Name);
                        previous?.Dispose();previous=renderProject.Transition==PageTransition.Cut?null:new Bitmap(image);lastPage=renderedPage;
                        string message=!sent?"Werk firmware bij":!session.Information.Advanced?"Ontwerp ontvangen · voor touch/helderheid is firmware 2.0 nodig":session.Information.Touch?"Ontwerp ontvangen":"Ontwerp ontvangen · touchcontroller niet gedetecteerd";
                        report(new(session.Information.Id,assignment.Name,candidate.Port,true,session.Information.Firmware,job.ProfileId,job.Design?.Name??renderProject.Name,renderedPage,count,session.Information.Advanced?brightness:100,message,DateTime.UtcNow,candidate.UsbId,session.Information.Touch,session.Information.Advanced,streamMeter.Latest is {} metrics?metrics with{Device=(session as DisplaySession)?.Measurements}:null));
                    }
                    else
                    {
                        composer.Pause();receivedFrame=false;session.Native(job.NativePacket);if(primary)ScreenDelivery.Write(directory,"","preset",assignment.Name);
                        report(new(session.Information.Id,assignment.Name,candidate.Port,true,session.Information.Firmware,job.ProfileId,"Ingebouwde layout",0,1,session.Information.Advanced?brightness:100,"USB verbonden",DateTime.UtcNow,candidate.UsbId,session.Information.Touch,session.Information.Advanced));
                    }
                }
                catch(Exception ex)when(ex is IOException or UnauthorizedAccessException or InvalidOperationException or TimeoutException or System.ComponentModel.Win32Exception or ArgumentException or System.Runtime.InteropServices.ExternalException)
                {
                    if(everConnected)alerts.Evaluate(job.Config.Alerts,job.Readings,false,DateTime.UtcNow);
                    report(new(session?.Information.Id??candidate.UsbId,candidate.Label,candidate.Port,false,session?.Information.Firmware??"onbekend",job.ProfileId,"",0,1,0,ex.Message,DateTime.UtcNow));
                    if(primary)ScreenDelivery.Write(directory,job.Revision,"disconnected",ex.Message);
                    session?.Dispose();session=null;lastConfig="";lastHash=0;retryAfter=DateTime.UtcNow.AddSeconds(12);
                }
                if(repeatAnimation&&session!=null){pending=job with{Readings=job.Readings with{Now=DateTime.Now}};}
            }
        }
        catch(Exception ex)when(ex is OperationCanceledException or ChannelClosedException){ }
        finally{if(current!=null&&everConnected)alerts.Evaluate(current.Config.Alerts,current.Readings,false,DateTime.UtcNow);session?.Dispose();composer.Dispose();previous?.Dispose();}
    }
    void TouchEvent(string packet)
    {
        var parts=packet.Split(';');if(parts.Length<4||!int.TryParse(parts[2],out int x)||!int.TryParse(parts[3],out int y))return;
        lastTouch=DateTime.UtcNow;if(lastBrightness==0||current?.Design==null)return;
        int count=DisplayPages.Count(current.Design);DateTime now=DateTime.UtcNow;
        if(parts[1]=="NEXT")pages.Go(pages.Index+1,count,now);
        else if(parts[1]=="PREVIOUS")pages.Go(pages.Index-1,count,now);
        else if(parts[1]=="MENU")quickMenuUntil=now.AddSeconds(8);
        else if(parts[1]=="TAP")
        {
            if(now<quickMenuUntil)
            {
                if(y is >=160 and <=255){if(x<145)pages.Go(pages.Index-1,count,now);else if(x>265)pages.Go(pages.Index+1,count,now);else dimmed=!dimmed;quickMenuUntil=DateTime.MinValue;}
                return;
            }
            var zone=DisplayPages.At(current.Design,pages.Index).TouchZones.LastOrDefault(z=>new Rectangle(z.X,z.Y,z.Width,z.Height).Contains(x,y));
            if(zone!=null)switch(zone.Action){case TouchAction.NextPage:pages.Go(pages.Index+1,count,now);break;case TouchAction.PreviousPage:pages.Go(pages.Index-1,count,now);break;case TouchAction.GoToPage:pages.Go(zone.Page,count,now);break;case TouchAction.ToggleBrightness:dimmed=!dimmed;break;}
        }
    }
    static void DrawQuickMenu(Bitmap image)
    {
        using var g=Graphics.FromImage(image);using var shade=new SolidBrush(Color.FromArgb(230,15,20,30));g.FillEllipse(shade,0,0,412,412);
        using var font=new Font("Segoe UI",20,FontStyle.Bold,GraphicsUnit.Pixel);using var small=new Font("Segoe UI",16,FontStyle.Bold,GraphicsUnit.Pixel);g.DrawString("SNELMENU",font,Brushes.White,140,115);g.DrawString("Vorige     Dimmen     Volgende",small,Brushes.White,60,193);
    }
    static Bitmap Transition(Bitmap before,Bitmap after,PageTransition type)
    {
        var image=new Bitmap(412,412);using var g=Graphics.FromImage(image);g.DrawImage(before,0,0,412,412);
        if(type==PageTransition.Slide)g.DrawImage(after,206,0,412,412);
        else if(type==PageTransition.Zoom)g.DrawImage(after,62,62,288,288);
        else{using var attributes=new System.Drawing.Imaging.ImageAttributes();var matrix=new System.Drawing.Imaging.ColorMatrix{Matrix33=.5f};attributes.SetColorMatrix(matrix);g.DrawImage(after,new Rectangle(0,0,412,412),0,0,412,412,GraphicsUnit.Pixel,attributes);}
        return image;
    }
    public void Dispose()
    {
        stopped.Cancel();jobs.Writer.TryComplete();session?.Dispose();
    }
}
static class AdvancedMonitor
{
    public static async Task RunAsync(string directory,Func<ScreenReadings> readSensors,string? fixedPort=null,CancellationToken cancellation=default)
    {
        var profiles=new ProfileRepository(directory);var selector=new ProfileEngine();var history=new SensorHistory();var sampler=new SensorSampler(readSensors,Log.Write);using var weather=new WeatherService(directory);
        var workers=new Dictionary<string,DeviceWorker>();var states=new ConcurrentDictionary<string,DeviceRuntimeStatus>();
        var config=new StudioConfiguration();DateTime configTime=DateTime.MinValue,nextDiscovery=DateTime.MinValue,nextHistory=DateTime.MinValue,nextStatus=DateTime.MinValue,lastSample=DateTime.MinValue;
        var designs=new Dictionary<string,(DateTime Time,ScreenDesign Design,string Revision)>();using var notifications=new DesktopAlerts();
        try
        {
            while(!cancellation.IsCancellationRequested)
            {
                DateTime now=DateTime.UtcNow;
                try{var modified=File.GetLastWriteTimeUtc(StudioConfiguration.PathIn(directory));if(modified!=configTime){config=StudioConfiguration.Load(directory);configTime=modified;}}
                catch(Exception ex)when(ex is IOException or InvalidDataException or JsonException or ArgumentException){Log.Write("Studio-instellingen: "+ex.Message);}
                var activity=ActivitySnapshot.Capture();var readings=sampler.Read(DateTime.Now) with{History=history,Weather=weather.Poll(now)};
                LiveTelemetry.Write(directory,readings);
                if(now-lastSample>=TimeSpan.FromSeconds(config.SamplingSeconds)){history.Add(readings);lastSample=now;}
                string automatic=selector.Select(config,activity,now,profiles.Exists);
                if(now>=nextDiscovery)
                {
                    nextDiscovery=now.AddSeconds(5);
                    try
                    {
                        var candidates=DeviceDiscovery.Scan(fixedPort);
                        foreach(string removed in workers.Keys.Where(id=>!candidates.Any(c=>c.UsbId==id)).ToArray()){workers[removed].Dispose();workers.Remove(removed);if(states.TryGetValue(removed,out var old))states[removed]=old with{Connected=false,Message="USB losgekoppeld",UpdatedUtc=now};}
                        foreach(var candidate in candidates)if(!workers.ContainsKey(candidate.UsbId)){string id=candidate.UsbId;workers[id]=new DeviceWorker(candidate,directory,workers.Count==0,status=>states[id]=status,notifications.Notify);}
                    }
                    catch(Exception ex)when(ex is ManagementException or UnauthorizedAccessException or System.Runtime.InteropServices.COMException){Log.Write("Apparaten zoeken: "+ex.Message);}
                }
                foreach(var pair in workers)
                {
                    string? primaryId=workers.Keys.FirstOrDefault(id=>states.TryGetValue(id,out var known)&&known.Connected)??workers.Keys.FirstOrDefault();
                    pair.Value.Primary=pair.Key==primaryId;
                    var assignment=config.Devices.FirstOrDefault(d=>d.DeviceId==pair.Key||states.TryGetValue(pair.Key,out var state)&&state.DeviceId==d.DeviceId);
                    string selected=automatic;
                    if(assignment!=null&&profiles.Exists(assignment.ProfileId)&&(!assignment.FollowAutomaticProfiles||automatic==config.DefaultProfileId||automatic=="legacy"))selected=assignment.ProfileId;
                    ScreenDesign? design=null;string revision="";
                    bool custom=selected!="legacy"||File.Exists(Path.Combine(directory,"custom-active.txt"));
                    if(custom)
                    {
                        try
                        {
                            string path=profiles.PathFor(selected);DateTime modified=File.GetLastWriteTimeUtc(path);
                            if(!designs.TryGetValue(selected,out var cached)||cached.Time!=modified){design=DesignFiles.Load(path);revision=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(path)));designs[selected]=(modified,design,revision);}
                            else{design=cached.Design;revision=cached.Revision;}
                        }
                        catch(Exception ex)when(ex is IOException or InvalidDataException or JsonException or ArgumentException){ScreenDelivery.Write(directory,"","invalid-design",ex.Message);}
                    }
                    string F(float? value)=>value?.ToString("0.0",CultureInfo.InvariantCulture)??"nan";
                    string packet=$"CPU={F(readings.Cpu)};GPU={F(readings.Gpu)};FPS={F(readings.Fps)};LAYOUT={LayoutSettings.Read(directory)}";
                    pair.Value.Post(new(config,selected,design,revision,readings,activity,packet));
                }
                if(workers.Count==0)ScreenDelivery.Write(directory,"","disconnected","USB-scherm niet gevonden.");
                if(now>=nextStatus){AtomicJson.Write(Path.Combine(directory,"devices-runtime.json"),states.Values.ToArray());nextStatus=now.AddSeconds(2);}
                if(now>=nextHistory)
                {
                    nextHistory=now.AddSeconds(5);
                    try{string path=Path.Combine(directory,"history-request.json");if(File.Exists(path)){var request=JsonSerializer.Deserialize<HistoryRequest>(File.ReadAllText(path),DesignFiles.Json);if(request!=null&&now-request.UpdatedUtc<TimeSpan.FromSeconds(15))AtomicJson.Write(Path.Combine(directory,"history-preview.json"),new HistoryResponse(request.Metric,request.Minutes,now,history.Window(request.Metric,request.Minutes,readings.Now)));}}catch(Exception ex)when(ex is IOException or JsonException){ }
                }
                await Task.Delay(1000,cancellation);
            }
        }
        finally{foreach(var worker in workers.Values)worker.Dispose();await Task.WhenAll(workers.Values.Select(w=>w.Completion));}
    }
}

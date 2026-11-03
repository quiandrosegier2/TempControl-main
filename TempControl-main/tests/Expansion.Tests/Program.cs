using System.Drawing;
using System.Drawing.Imaging;
using System.Reflection;
using System.Diagnostics;
using System.IO.Ports;
using System.Text.Json;
using System.Windows.Forms;

static class Program
{
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    [STAThread]static void Main(string[] args)
    {try{Run(args);}catch(Exception ex){Console.Error.WriteLine(ex);Environment.ExitCode=1;}}
    static void Run(string[] args)
    {
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);
        if(args.Length>=3&&args[1]=="--physical"){Physical(Path.GetFullPath(args[0]),args[2],args.Length>3?args[3]:"");return;}
        string output=Path.GetFullPath(args[0]);Directory.CreateDirectory(output);int batch=args.Length>1?int.Parse(args[1]):0;
        var templates=batch==0?ExpansionTemplates.All:ExpansionTemplates.Batch(batch);
        Check(templates.Count==(batch==0?50:10),"Wrong expansion count");Check(templates.Select(t=>t.Id).Distinct().Count()==templates.Count,"Duplicate identifiers");
        var demo=ScreenReadings.Demo with{Now=new DateTime(2026,11,3,19,45,0)};var hashes=new HashSet<uint>();
        using var sheet=new Bitmap(5*280,((templates.Count+4)/5)*300);using var g=Graphics.FromImage(sheet);g.Clear(StudioTheme.Background);using var font=new Font("Segoe UI",12,FontStyle.Bold);
        int index=0;long bytes=0;var timings=new List<double>();
        foreach(var template in templates)
        {
            var d=template.Design;Check(Guid.TryParseExact(template.Id,"N",out _),"Invalid identifier");
            foreach(var required in new[]{WidgetKind.Time,WidgetKind.Date,WidgetKind.CpuTemperature,WidgetKind.GpuTemperature,WidgetKind.MemoryLoad,WidgetKind.Fps})Check(d.Widgets.Any(w=>w.Kind==required),"Missing binding "+required+": "+d.Name);
            foreach(var source in new[]{WidgetKind.CpuLoad,WidgetKind.GpuLoad})Check(d.Widgets.Any(w=>w.Kind==WidgetKind.ProgressBar&&w.BarSource==source),"Missing load indicator: "+d.Name);
            Check(d.Widgets.All(w=>w.X>=0&&w.Y>=0&&w.X+w.Width<=412&&w.Y+w.Height<=412),"Out-of-bounds element: "+d.Name);
            byte[] art=Convert.FromBase64String(d.BackgroundPng!);bytes+=art.Length;using(var stream=new MemoryStream(art))using(var image=Image.FromStream(stream))Check(image.Size==new Size(412,412),"Artwork not optimized: "+d.Name);
            var restored=DesignFiles.Parse(DesignFiles.Serialize(d));Check(restored.BackgroundPng==d.BackgroundPng&&restored.Widgets.Count==d.Widgets.Count,"Serialization lost data");
            var copy=DesignSnapshot.Capture(d).Restore();copy.Widgets[0].X++;copy.BackgroundPng=null;Check(copy.Widgets[0].X!=d.Widgets[0].X&&copy.Widgets.Count==d.Widgets.Count,"Copy/art replacement damaged original");
            using var renderer=new DesignRenderer();var watch=Stopwatch.StartNew();using var frame=renderer.Render(d,demo);watch.Stop();timings.Add(watch.Elapsed.TotalMilliseconds);uint hash=CustomScreenSender.Hash(DesignRenderer.Rgb565(frame));Check(hashes.Add(hash),"Duplicate appearance");Check(frame.GetPixel(0,0).ToArgb()==Color.Black.ToArgb(),"Circular crop broken");
            using var missing=renderer.Render(d,LiveTelemetry.Unavailable(demo.Now));Check(CustomScreenSender.Hash(DesignRenderer.Rgb565(missing))!=hash,"Missing values hidden");
            if(DesignRenderer.IsAnimated(d)){using var later=renderer.Render(d,demo with{Now=demo.Now.AddSeconds(2.1)});Check(CustomScreenSender.Hash(DesignRenderer.Rgb565(later))!=hash,"Animation not moving: "+d.Name);}
            var still=DesignSnapshot.Capture(d).Restore();still.AnimationsEnabled=false;using var staticRenderer=new DesignRenderer();using var a=staticRenderer.Render(still,demo);using var b=staticRenderer.Render(still,demo with{Now=demo.Now.AddMilliseconds(300)});Check(CustomScreenSender.Hash(DesignRenderer.Rgb565(a))==CustomScreenSender.Hash(DesignRenderer.Rgb565(b)),"No static fallback");
            string file=Path.Combine(output,template.Id+".qscreenpack");QscreenPackage.Export(file,d,template.Manifest);var package=QscreenPackage.Import(file);Check(package.Design.BackgroundPng==d.BackgroundPng&&package.Design.Widgets.Count==d.Widgets.Count,"Package not portable");
            using var thumb=new Bitmap(frame,new Size(190,190));thumb.Save(Path.Combine(output,$"{index+1:00}-thumbnail.png"));frame.Save(Path.Combine(output,d.Name.Replace(" ","-")+".png"));
            g.DrawImage(frame,index%5*280+14,index/5*300+4,252,252);g.DrawString(template.Manifest.Description.Split(" / ")[0]+" · "+d.Name,font,Brushes.White,index%5*280+12,index/5*300+264);index++;
            Console.WriteLine("PASS "+d.Name+" / complete bindings, 412 artwork, editable copy, package, animation/static");
        }
        sheet.Save(Path.Combine(output,"contact-sheet.png"),ImageFormat.Png);
        using(var preview=new TemplatePreviewWindow(templates[0],false)){preview.Show();Application.DoEvents();preview.Close();}
        // Central readings must survive UI rendering unchanged, with explicit stale/missing states.
        var live=demo with{Cpu=63,Gpu=71,Fps=97,MemoryLoad=41};LiveTelemetry.Write(output,live);var snapshot=LiveTelemetry.Read(output,DateTime.UtcNow);Check(snapshot is not null&&snapshot.Readings.Cpu==live.Cpu&&snapshot.Readings.Fps==live.Fps,"Central telemetry lost values");Check(LiveTelemetry.Read(output,DateTime.UtcNow.AddMinutes(1))==null,"Stale data passed as live");
        using(var editor=new DesignWindow(output,null,templates[0].Design))
        {var flags=BindingFlags.NonPublic|BindingFlags.Instance;var readings=(ScreenReadings)typeof(DesignWindow).GetMethod("PreviewReadings",flags)!.Invoke(editor,null)!;Check(readings.Cpu==live.Cpu&&readings.Gpu==live.Gpu&&readings.Fps==live.Fps,"Designer does not use central measurements");var demoMode=(CheckBox)typeof(DesignWindow).GetField("demoMode",flags)!.GetValue(editor)!;demoMode.Checked=true;Check(((ScreenReadings)typeof(DesignWindow).GetMethod("PreviewReadings",flags)!.Invoke(editor,null)!).Cpu==42,"Explicit demo unavailable");}
        using(var monitor=new AdvancedStudioWindow(output)){var flags=BindingFlags.NonPublic|BindingFlags.Instance;typeof(AdvancedStudioWindow).GetMethod("RefreshHistory",flags)!.Invoke(monitor,null);var label=(Label)typeof(AdvancedStudioWindow).GetField("liveValues",flags)!.GetValue(monitor)!;Check(label.Text.Contains("63")&&label.Text.Contains("97"),"PC Monitor does not show the same snapshot");}
        using(var livePreview=new TemplatePreviewWindow(templates[0],false,output))
        {
            livePreview.Show();Application.DoEvents();
            var details=livePreview.Controls.OfType<Label>().Single();Check(details.Text.Contains("63")&&!details.Text.Contains("DEMO"),"Preview does not default to live values");
            var bar=livePreview.Controls.OfType<FlowLayoutPanel>().Single();var pause=bar.Controls.OfType<Button>().First(b=>b.Text=="Pauzeren");if(pause.Enabled){pause.PerformClick();Check(pause.Text=="Afspelen","Animation pause button did not work");}
            LiveTelemetry.Write(output,live with{Cpu=64,Fps=98});var toggle=bar.Controls.OfType<CheckBox>().Single();toggle.Checked=true;toggle.Checked=false;Check(details.Text.Contains("64")&&details.Text.Contains("98"),"Paused animation stopped live metric updates");
        }
        LiveTelemetry.Write(output,live with{Cpu=float.NaN,Gpu=float.PositiveInfinity});Check(LiveTelemetry.Read(output,DateTime.UtcNow)?.Readings is{Cpu:null,Gpu:null},"Invalid sensor numbers were not made unavailable");
        AtomicJson.Write(Path.Combine(output,"live-telemetry.json"),new LiveTelemetry(DateTime.UtcNow.AddMinutes(5),live));Check(LiveTelemetry.Read(output,DateTime.UtcNow)==null,"Future snapshot treated as current");
        File.WriteAllText(Path.Combine(output,"live-telemetry.json"),"{invalid");Check(LiveTelemetry.Read(output,DateTime.UtcNow)==null,"Corrupt snapshot did not fail closed");
        using(var offline=new DesignWindow(output,null,templates[0].Design)){var readings=(ScreenReadings)typeof(DesignWindow).GetMethod("PreviewReadings",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(offline,null)!;Check(readings.Cpu==null&&readings.Fps==null,"Offline editor silently used fake demo values");}
        if(batch==0)
        {
            Check(OfficialTemplates.Premium.Count==100&&PremiumTemplates.All.Count==21,"Existing collection removed");
            Check(OfficialTemplates.Premium.Select(t=>t.Id).Distinct().Count()==100&&!OfficialTemplates.Premium.Select(t=>t.Id).Intersect(PremiumTemplates.All.Select(t=>t.Id)).Any(),"Existing IDs overwritten");
            var spoof=new DesignTemplate(templates[0].Id,new(){Author="QPRINTS",Name="Local copy"},templates[0].Design);Check(!OfficialTemplates.Contains(spoof),"Local metadata impersonated a shipped template");
            var blade=DesignSnapshot.Capture(templates[11].Design).Restore();blade.BackgroundPng=null;blade.Animation=PageAnimation.None;blade.Widgets=blade.Widgets.Where(w=>w.Kind==WidgetKind.NeonLine).ToList();Check(blade.Widgets.Count==1,"Katana glow is not separately editable");using(var renderer=new DesignRenderer()){using var a=renderer.Render(blade,demo);using var b=renderer.Render(blade,demo with{Now=demo.Now.AddSeconds(1.2)});Check(CustomScreenSender.Hash(DesignRenderer.Rgb565(a))!=CustomScreenSender.Hash(DesignRenderer.Rgb565(b)),"Katana glow not animated");}
            var infinity=templates[49].Design.Widgets.Single(w=>w.Kind==WidgetKind.InfinityRibbon);var properties=new WidgetInspector(infinity).GetProperties();Check(properties.Find("StrokeWidth",false)!=null&&properties.Find("Rainbow",false)!=null,"Infinity appearance cannot be edited");
            var glow=DesignSnapshot.Capture(templates[25].Design).Restore();glow.BackgroundPng=null;glow.Animation=PageAnimation.None;glow.Widgets=glow.Widgets.Where(w=>w.DecorativeRing).ToList();Check(glow.Widgets.Count>0,"No independent decoration");using(var renderer=new DesignRenderer()){using var a=renderer.Render(glow,LiveTelemetry.Unavailable(demo.Now));using var b=renderer.Render(glow,LiveTelemetry.Unavailable(demo.Now.AddSeconds(1.2)));Check(CustomScreenSender.Hash(DesignRenderer.Rgb565(a))!=CustomScreenSender.Hash(DesignRenderer.Rgb565(b)),"Missing sensors stopped decorative pulse");}
            using var gallery=new TemplatesWindow(output);gallery.Show();for(int i=0;i<500&&gallery.LoadedCount<121;i++){Application.DoEvents();Thread.Sleep(25);}Check(gallery.LoadedCount==121&&gallery.VisibleCount==100,"Gallery must start with 100 premium designs");
            var flags=BindingFlags.NonPublic|BindingFlags.Instance;var search=(TextBox)typeof(TemplatesWindow).GetField("search",flags)!.GetValue(gallery)!;search.Text="RGB Dream";Application.DoEvents();Check(gallery.VisibleCount==1,"Layout 100 not searchable");search.Clear();var collection=(ComboBox)typeof(TemplatesWindow).GetField("collection",flags)!.GetValue(gallery)!;collection.SelectedIndex=5;Application.DoEvents();Check(gallery.VisibleCount==50,"Expansion filter incomplete");collection.SelectedIndex=4;Application.DoEvents();Check(gallery.VisibleCount==50,"Original Neon 50 filter incomplete");collection.SelectedIndex=1;Application.DoEvents();Check(gallery.VisibleCount==21,"Studio originals inaccessible");collection.SelectedIndex=0;
            using var capture=new Bitmap(gallery.Width,gallery.Height);gallery.DrawToBitmap(capture,new Rectangle(Point.Empty,capture.Size));capture.Save(Path.Combine(output,"gallery-100.png"));gallery.Close();
        }
        Console.WriteLine($"PASS batch {batch}: {templates.Count} templates; background bytes={bytes}; median first render={timings.Order().ElementAt(timings.Count/2):0.0} ms. Physical ESP32 and missing reference poster not verified.");
    }
    static void Physical(string output,string portName,string installedDirectory)
    {
        if(!System.Text.RegularExpressions.Regex.IsMatch(portName,@"^COM\d{1,3}$"))throw new ArgumentException("Invalid COM port");
        Directory.CreateDirectory(output);var results=new List<object>();int frames=0;
        using var port=new SerialPort(portName,115200){NewLine="\n",DtrEnable=false,RtsEnable=false,WriteTimeout=5000,ReadTimeout=12000};port.Open();Thread.Sleep(600);port.DiscardInBuffer();
        var sender=new CustomScreenSender(new SerialFrameConnection(port));using var renderer=new DesignRenderer();
        foreach(var template in ExpansionTemplates.All)
        {
            int count=DesignRenderer.IsAnimated(template.Design)?2:1;var elapsed=Stopwatch.StartNew();
            for(int i=0;i<count;i++)
            {
                var now=DateTime.Now.AddSeconds(i*1.2);var snapshot=LiveTelemetry.Read(installedDirectory,DateTime.UtcNow);var readings=snapshot is{} live?live.Readings with{Now=now}:LiveTelemetry.Unavailable(now);
                using var frame=renderer.Render(template.Design,readings);if(!sender.Send(DesignRenderer.Rgb565(frame)))throw new IOException("Screen does not support acknowledged FRAME2");frames++;Thread.Sleep(180);
            }
            elapsed.Stop();results.Add(new{Name=template.Manifest.Name,Frames=count,Milliseconds=elapsed.ElapsedMilliseconds});Console.WriteLine("FRAMEOK: "+template.Manifest.Name+" / "+count);Console.Out.Flush();
        }
        File.WriteAllText(Path.Combine(output,"physical-report.json"),JsonSerializer.Serialize(new{UpdatedUtc=DateTime.UtcNow,Port=portName,Templates=results.Count,AcknowledgedFrames=frames,Results=results,VisualInspection=false,FirmwareFlashed=false},new JsonSerializerOptions{WriteIndented=true}));
        Console.WriteLine("PASS: physical USB acknowledgements for "+results.Count+" templates / "+frames+" frames; manual visual inspection remains separate.");
    }
}

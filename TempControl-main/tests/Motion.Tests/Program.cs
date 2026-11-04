using System.Drawing;
using System.Drawing.Imaging;
using System.Reflection;
using System.Windows.Forms;

static class Program
{
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    [STAThread]static void Main(string[] args){try{Run(args);}catch(Exception ex){Console.Error.WriteLine(ex);Environment.ExitCode=1;}}
    static void Run(string[] args)
    {
        string output=Path.GetFullPath(args[0]);Directory.CreateDirectory(output);byte[] gif=GhostFixture.Create();File.WriteAllBytes(Path.Combine(output,"ghost-cute.gif"),gif);
        if(args.Contains("--prepare-suite")){SmoothBenchmarks.Prepare(output,gif);return;}
        var info=GifAnimation.Inspect(gif);Check(info.FrameCount==12&&info.DurationMs==1440&&info.Transparent&&info.Width==64,"GIF metadata/timing incorrect");
        using(var stream=new MemoryStream(gif))using(var image=Image.FromStream(stream)){Check(image.GetFrameCount(FrameDimension.Time)==12,"Windows cannot decode fixture");}
        var design=new ScreenDesign{Name="Ghost Cute · GIF stream test",BackgroundArgb=Color.FromArgb(8,13,28).ToArgb(),BackgroundPng=Convert.ToBase64String(gif),Darken=0,StreamingQuality=AnimationQuality.Maximum,Widgets=[new(){Name="Klok",Kind=WidgetKind.Time,Format="HH:mm:ss",X=90,Y=50,Width=232,Height=40,FontSize=32},new(){Name="CPU",Kind=WidgetKind.CpuTemperature,Format="CPU {value}°C",X=72,Y=325,Width=130,Height=30,FontSize=18},new(){Name="GPU",Kind=WidgetKind.GpuTemperature,Format="GPU {value}°C",X=210,Y=325,Width=130,Height=30,FontSize=18},new(){Name="FPS",Kind=WidgetKind.Fps,Format="{value} FPS",X=116,Y=363,Width=180,Height=26,FontSize=17}]};
        design.Widgets.Insert(0,new(){Name="Leesbare klok",Kind=WidgetKind.Panel,X=84,Y=45,Width=244,Height=47,Foreground=Color.FromArgb(50,160,195),Fill=Color.FromArgb(5,10,24),Opacity=85,StrokeWidth=1});design.Widgets.Insert(1,new(){Name="Leesbare temperaturen",Kind=WidgetKind.Panel,X=64,Y=318,Width=284,Height=41,Foreground=Color.FromArgb(50,160,195),Fill=Color.FromArgb(5,10,24),Opacity=85,StrokeWidth=1});
        if(args.Contains("--layered")){design.BackgroundPng=null;design.MotionLayers=[new(){Name="Ghost Cute sprite",ImageData=Convert.ToBase64String(gif),Duration=1.44,Width=220,Height=220}];}
        if(args.Contains("--prepare")){string id=new ProfileRepository(output).Add(design);new StudioConfiguration{ManualProfileId=id}.Save(output);Console.WriteLine("Prepared GIF benchmark profile: "+id);return;}
        var start=new DateTime(2026,11,4,12,0,0);var readings=new ScreenReadings(63,71,97,45,60,50,start);
        using var renderer=new DesignRenderer();var hashes=new HashSet<uint>();using var sheet=new Bitmap(4*206,3*206);using var graphics=Graphics.FromImage(sheet);
        for(int i=0;i<12;i++){using var frame=renderer.Render(design,readings with{Now=start.AddMilliseconds(i*120)});hashes.Add(CustomScreenSender.Hash(DesignRenderer.Rgb565(frame)));graphics.DrawImage(frame,i%4*206,i/4*206,206,206);Check(renderer.CurrentGifFrameIndex==i,"GIF timing does not select the expected frame");Check(frame.GetPixel(0,0).ToArgb()==Color.Black.ToArgb(),"Circular clip missing");}
        Check(hashes.Count>=10,"Character is not genuinely moving");sheet.Save(Path.Combine(output,"gif-frames.png"));
        using(var frame=renderer.Render(design,readings)){Check(frame.GetPixel(90,120).ToArgb()==Color.FromArgb(8,13,28).ToArgb(),"Transparency was flattened");}
        var copy=DesignFiles.Parse(DesignFiles.Serialize(design));Check(copy.BackgroundPng==design.BackgroundPng&&copy.StreamingQuality==AnimationQuality.Maximum,"Project lost GIF/quality");Check(DesignSnapshot.Capture(copy).Restore().StreamingQuality==AnimationQuality.Maximum,"Undo snapshot lost quality");
        string first=GifAnimation.CachePersonal(gif,Path.Combine(output,"library"));string second=GifAnimation.CachePersonal(gif,Path.Combine(output,"library"));Check(first==second&&Directory.GetFiles(Path.Combine(output,"library")).Length==1,"Asset cache retransferred duplicates");
        foreach(var bad in new[]{gif[..^1],gif[..20],new byte[]{1,2,3}}){bool rejected=false;try{GifAnimation.Inspect(bad);}catch(InvalidDataException){rejected=true;}Check(rejected,"Malformed GIF accepted");}
        var layer=new MotionLayer{Name="Floating ghost",ImageData=Convert.ToBase64String(gif),Duration=2,Width=150,Height=150,Keyframes=[new(){Seconds=0,X=130,Y=170},new(){Seconds=2,X=280,Y=230,Rotation=30,Scale=1.2f}]};
        var layered=new ScreenDesign{Darken=0,MotionLayers=[layer]};
        MotionLayer.Validate(layered.MotionLayers);Check(Math.Abs(layer.PoseAt(1).X-205)<.01,"Movement path interpolation failed");
        using(var lr=new DesignRenderer()){lr.MotionTimeSeconds=0;using var a=lr.Render(layered,readings);lr.MotionTimeSeconds=1;using var b=lr.Render(layered,readings);Check(CustomScreenSender.Hash(DesignRenderer.Rgb565(a))!=CustomScreenSender.Hash(DesignRenderer.Rgb565(b)),"Layer keyframes did not move rendered character");b.Save(Path.Combine(output,"motion-layer.png"));}
        layered.Pages.Add(DisplayPage.From(layered));var restored=DesignSnapshot.Capture(layered).Restore();Check(restored.MotionLayers[0].ImageData==layer.ImageData&&restored.Pages[0].MotionLayers[0].ImageData==layer.ImageData,"Layer image lost in undo/page snapshot");
        Check(DisplayPages.At(restored,1).MotionLayers.Count==1,"Page selection lost motion layers");
        var portable=DesignFiles.Parse(DesignFiles.Serialize(layered));Check(portable.MotionLayers[0].Keyframes.Count==2,"Saved project lost keyframes");
        string pack=Path.Combine(output,"motion.qscreenpack");QscreenPackage.Export(pack,layered,new TemplateManifest{Name="Motion test"});Check(QscreenPackage.Import(pack).Design.MotionLayers.Count==1,"Template package lost motion layers");
        layer.Keyframes.Add(new(){Seconds=double.NaN});bool invalid=false;try{MotionLayer.Validate([layer]);}catch(InvalidDataException){invalid=true;}Check(invalid,"Non-finite keyframe accepted");layer.Keyframes.RemoveAt(2);
        var before=new byte[CustomScreenSender.FrameBytes];var after=(byte[])before.Clone();after[(55*412+70)*2]=123;var patch=FramePatch.Create(before,after);Check(patch!=null&&patch.Length<1000,"Dirty rectangle optimization failed");
        var receiver=new PatchReceiver();var sender=new CustomScreenSender(receiver,null,true);sender.Send(before);sender.Send(after);Check(receiver.Commands[^1].StartsWith("PATCH1=")&&receiver.Pixels.SequenceEqual(after),"Delta transfer did not reconstruct frame");receiver.RejectNext=true;bool rejection=false;try{sender.Send(before);}catch(IOException){rejection=true;}Check(rejection,"Corrupt delta was accepted");sender.Send(after);Check(receiver.Commands[^1].StartsWith("FRAME2="),"Rejected delta did not invalidate baseline");
        using(var native=new Bitmap(64,64)){native.SetPixel(20,20,Color.FromArgb(80,240,100,60));native.Save(Path.Combine(output,"alpha.png"),ImageFormat.Png);}
        byte[] packed=MotionAsset.Pack([Path.Combine(output,"alpha.png"),Path.Combine(output,"alpha.png")],100);Check(MotionAsset.Inspect(packed).DelaysMs.Length==2,"Native animation metadata lost");
        var nativeDesign=new ScreenDesign{MotionLayers=[new(){ImageData=Convert.ToBase64String(packed)}]};using(var nr=new DesignRenderer()){nr.MotionTimeSeconds=.1;using var frame=nr.Render(nativeDesign,readings);frame.Save(Path.Combine(output,"native-alpha.png"));}
        if(args.Length>1&&File.Exists(args[1]))
        {
            string converter=Path.GetFullPath(args[1]);
            foreach(var ext in new[]{"mp4","webm","webp","apng"})
            {
                string input=Path.Combine(output,"fixture."+ext);var psi=new System.Diagnostics.ProcessStartInfo(converter){UseShellExecute=false,CreateNoWindow=true};
                foreach(var arg in new[]{"-y","-v","error","-i",Path.Combine(output,"ghost-cute.gif"),"-an"})psi.ArgumentList.Add(arg);
                if(ext=="mp4"){psi.ArgumentList.Add("-c:v");psi.ArgumentList.Add("mpeg4");psi.ArgumentList.Add("-pix_fmt");psi.ArgumentList.Add("yuv420p");}
                if(ext=="webm"){psi.ArgumentList.Add("-c:v");psi.ArgumentList.Add("libvpx-vp9");psi.ArgumentList.Add("-pix_fmt");psi.ArgumentList.Add("yuva420p");}
                psi.ArgumentList.Add(input);using(var process=System.Diagnostics.Process.Start(psi)!){process.WaitForExit();Check(process.ExitCode==0,"Fixture encoding failed: "+ext);}
                byte[] converted=MotionAsset.ConvertVideo(input,converter,2,6,CancellationToken.None).GetAwaiter().GetResult();var meta=MotionAsset.Inspect(converted);Check(meta.DelaysMs.Length>1&&meta.Width==412,"Conversion failed: "+ext);File.WriteAllBytes(Path.Combine(output,ext+".qanim"),converted);
            }
        }
        var meter=new AnimationStreamMeter();meter.Received(5,495,339488,AnimationQuality.Maximum);Check(meter.Latest!.AcknowledgedFrames==1&&meter.EffectiveTarget(AnimationQuality.Maximum)<2&&meter.Latest.SkippedUpdates>0,"Transport backpressure ignored");
        layer.Reactive=new(){Enabled=true,Threshold=80,Hysteresis=3,CooldownSeconds=3};using(var mr=new MotionLayerRenderer())using(var surface=new Bitmap(412,412))using(var g=Graphics.FromImage(surface))
        {
            var reactive=new ScreenDesign{MotionLayers=[layer]};
            void Tick(int seconds,float? cpu){mr.Draw(g,reactive,readings with{Now=start.AddSeconds(seconds),Cpu=cpu},null);}
            Tick(0,59);Check(mr.ReactiveActive(layer)==false,"Reactive rule activated below threshold");Tick(1,81);Check(mr.ReactiveActive(layer)==true,"Reactive threshold did not activate");Tick(2,79);Check(mr.ReactiveActive(layer)==true,"Hysteresis did not retain state");Tick(3,70);Check(mr.ReactiveActive(layer)==true,"Cooldown was ignored");Tick(5,70);Check(mr.ReactiveActive(layer)==false,"Reactive state never reset");Tick(9,81);Tick(10,null);Check(mr.ReactiveActive(layer)==false,"Missing sensor did not fall back safely");
        }
        using(var studio=new MotionStudioWindow(layered,()=>readings)){studio.Show();Application.DoEvents();using var capture=new Bitmap(studio.Width,studio.Height);studio.DrawToBitmap(capture,new Rectangle(0,0,studio.Width,studio.Height));capture.Save(Path.Combine(output,"motion-studio.png"));}
        using var editor=new DesignWindow(output,null,design);var flags=BindingFlags.Instance|BindingFlags.NonPublic;var actual=(ScreenDesign)typeof(DesignWindow).GetField("design",flags)!.GetValue(editor)!;Check(actual.BackgroundPng==design.BackgroundPng,"Editor flattened imported GIF");
        using(var composer=new AnimationComposer())using(var limit=new CancellationTokenSource(TimeSpan.FromSeconds(5)))
        {
            long firstId=composer.Submit("same-project",layered,readings);using var firstFrame=composer.Next(firstId,limit.Token).GetAwaiter().GetResult();
            Thread.Sleep(180);long secondId=composer.Submit("same-project",layered,readings with{Cpu=90,Now=start.AddYears(-10)});using var secondFrame=composer.Next(secondId,limit.Token).GetAwaiter().GetResult();
            Check(secondFrame.PlaybackSeconds>firstFrame.PlaybackSeconds+.1,"Sensor update restarted monotonic animation clock");
            Thread.Sleep(400);Check(composer.ReplacedFrames>0,"Slow transport accumulated an unbounded queue instead of replacing outdated frames");
        }
        Console.WriteLine("PASS: GIF timing/transparency and character movement; native alpha frame assets; project/page/undo/package preservation; timeline UI; keyframe interpolation; sensor hysteresis/cooldown/fallback; delta transport/rejection recovery; actual Windows conversion when converter supplied. Hardware verification is reported separately.");
    }
}

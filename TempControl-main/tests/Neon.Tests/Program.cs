using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

static class Program
{
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    [STAThread]static void Main(string[] args)
    {
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);
        string output=Path.GetFullPath(args[0]);Directory.CreateDirectory(output);int batch=args.Length>1?int.Parse(args[1]):0;
        var templates=batch==0?NeonTemplates.All:NeonTemplates.Batch(batch);
        Check(templates.Count==(batch==0?50:10),"Wrong template count");Check(templates.Select(t=>t.Id).Distinct().Count()==templates.Count,"Duplicate stable IDs");
        var demo=ScreenReadings.Demo with{Now=new DateTime(2026,11,3,19,45,0),CpuClock=4200,GpuClock=2300};
        var hashes=new HashSet<uint>();var times=new List<double>();long bytes=0;
        using var sheet=new Bitmap(5*280,((templates.Count+4)/5)*300);using var graphics=Graphics.FromImage(sheet);graphics.Clear(StudioTheme.Background);using var label=new Font("Segoe UI",12,FontStyle.Bold);
        int index=0;
        foreach(var template in templates)
        {
            var d=template.Design;Check(Guid.TryParseExact(template.Id,"N",out _),"Invalid ID: "+d.Name);
            Check(d.Widgets.Count is >=2 and <=64,"Not editable: "+d.Name);Check(d.Widgets.All(w=>w.X>=0&&w.Y>=0&&w.X+w.Width<=412&&w.Y+w.Height<=412),"Widget outside canvas: "+d.Name);
            Check(d.Widgets.All(w=>!w.Format.Contains("42°C")&&!w.Format.Contains("165")&&!w.Format.Contains("19:45")),"Hardcoded readings: "+d.Name);
            Check(!string.IsNullOrEmpty(d.BackgroundPng),"Missing decorative artwork: "+d.Name);
            byte[] background=Convert.FromBase64String(d.BackgroundPng!);bytes+=background.Length;
            using(var stream=new MemoryStream(background))using(var art=Image.FromStream(stream)){Check(art.Width<=1254&&art.Height<=1254,"Oversized background: "+d.Name);}
            string serialized=DesignFiles.Serialize(d);var restored=DesignFiles.Parse(serialized);Check(restored.Widgets.Count==d.Widgets.Count&&restored.BackgroundPng==d.BackgroundPng,"Roundtrip lost artwork/widgets: "+d.Name);
            var copy=DesignSnapshot.Capture(d).Restore();copy.Widgets[0].X++;Check(copy.Widgets[0].X!=d.Widgets[0].X,"Personal copy changed original: "+d.Name);
            copy.BackgroundPng=null;Check(copy.Widgets.Count==d.Widgets.Count,"Replacing artwork lost widgets: "+d.Name);
            using var renderer=new DesignRenderer();var watch=Stopwatch.StartNew();using var frame=renderer.Render(d,demo);watch.Stop();times.Add(watch.Elapsed.TotalMilliseconds);
            Check(frame.Size==new Size(412,412)&&frame.GetPixel(0,0).ToArgb()==Color.Black.ToArgb(),"Invalid circular framebuffer");
            uint hash=CustomScreenSender.Hash(DesignRenderer.Rgb565(frame));Check(hashes.Add(hash),"Visually duplicated layout: "+d.Name);
            using var unavailable=renderer.Render(d,new(null,null,null,null,null,null,demo.Now));
            if(d.Widgets.Any(w=>w.Kind is WidgetKind.CpuTemperature or WidgetKind.GpuTemperature or WidgetKind.Fps or WidgetKind.MemoryLoad or WidgetKind.CpuLoad))Check(CustomScreenSender.Hash(DesignRenderer.Rgb565(unavailable))!=hash,"Unavailable sensor ignored: "+d.Name);
            if(DesignRenderer.IsAnimated(d)){using var later=renderer.Render(d,demo with{Now=demo.Now.AddSeconds(2)});Check(CustomScreenSender.Hash(DesignRenderer.Rgb565(later))!=hash,"Animation not moving: "+d.Name);}
            var still=DesignSnapshot.Capture(d).Restore();still.AnimationsEnabled=false;using var staticRenderer=new DesignRenderer();using var a=staticRenderer.Render(still,demo);using var b=staticRenderer.Render(still,demo with{Now=demo.Now.AddMilliseconds(300)});Check(CustomScreenSender.Hash(DesignRenderer.Rgb565(a))==CustomScreenSender.Hash(DesignRenderer.Rgb565(b)),"Static alternative changed: "+d.Name);
            string file=Path.Combine(output,template.Id+".qscreenpack");QscreenPackage.Export(file,d,template.Manifest);var package=QscreenPackage.Import(file);Check(package.Design.BackgroundPng==d.BackgroundPng&&package.Design.Widgets.Count==d.Widgets.Count,"Nonportable package: "+d.Name);
            frame.Save(Path.Combine(output,d.Name.Replace(" + ","-").Replace(" ","-")+".png"),ImageFormat.Png);
            graphics.DrawImage(frame,index%5*280+14,index/5*300+4,252,252);graphics.DrawString(template.Manifest.Description[..2]+" · "+d.Name,label,Brushes.White,index%5*280+12,index/5*300+264);index++;
            Console.WriteLine("PASS "+template.Manifest.Description[..2]+" "+d.Name+" / render, live/unavailable, editable copy, package, static fallback");
        }
        sheet.Save(Path.Combine(output,"contact-sheet.png"),ImageFormat.Png);
        if(batch==0)
        {
            Check(PremiumTemplates.All.Count==21,"Original catalog removed");Check(!PremiumTemplates.All.Select(t=>t.Id).Intersect(templates.Select(t=>t.Id)).Any(),"Old template ID overwritten");
            var angry=DesignSnapshot.Capture(templates[25].Design).Restore();angry.Widgets=angry.Widgets.Where(w=>w.Kind==WidgetKind.ThermalFace).ToList();angry.BackgroundPng=null;
            using var renderer=new DesignRenderer();using var cold=renderer.Render(angry,demo with{Cpu=40});using var hot=renderer.Render(angry,demo with{Cpu=90});Check(CustomScreenSender.Hash(DesignRenderer.Rgb565(cold))!=CustomScreenSender.Hash(DesignRenderer.Rgb565(hot)),"CPU expression did not respond to temperature");
            using var gallery=new TemplatesWindow(output);gallery.Show();for(int i=0;i<400&&gallery.LoadedCount<71;i++){Application.DoEvents();Thread.Sleep(25);}Check(gallery.LoadedCount==71&&gallery.VisibleCount==50,"Gallery must show Neon 50 by default and preserve original 21");
            var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            T Field<T>(string name)=>(T)typeof(TemplatesWindow).GetField(name,flags)!.GetValue(gallery)!;
            var search=Field<TextBox>("search");search.Text="Robot Buddy";Application.DoEvents();Check(gallery.VisibleCount==1,"Search failed");search.Clear();
            var category=Field<ComboBox>("category");category.SelectedItem="Nature";Application.DoEvents();Check(gallery.VisibleCount==templates.Count(t=>t.Manifest.Category==TemplateCategory.Nature),"Category filter failed");category.SelectedIndex=0;
            var animated=Field<CheckBox>("animatedOnly");animated.Checked=true;Application.DoEvents();Check(gallery.VisibleCount==templates.Count(t=>t.Manifest.Animated),"Animated filter failed");var stillOnly=Field<CheckBox>("staticOnly");stillOnly.Checked=true;Application.DoEvents();Check(!animated.Checked&&gallery.VisibleCount==templates.Count(t=>!t.Manifest.Animated),"Static filter failed");stillOnly.Checked=false;
            var collection=Field<ComboBox>("collection");collection.SelectedIndex=1;Application.DoEvents();Check(gallery.VisibleCount==21,"Original catalog not accessible");collection.SelectedIndex=0;Application.DoEvents();
            using var capture=new Bitmap(gallery.Width,gallery.Height);gallery.DrawToBitmap(capture,new Rectangle(Point.Empty,capture.Size));capture.Save(Path.Combine(output,"gallery-50.png"));gallery.Close();
        }
        Console.WriteLine($"PASS batch {batch}: {templates.Count} distinct layouts. Background bytes={bytes}; median first render={times.Order().ElementAt(times.Count/2):0.0} ms. Physical ESP32 not tested.");
    }
}

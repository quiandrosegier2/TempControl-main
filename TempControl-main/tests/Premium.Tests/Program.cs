using System.Drawing;
using System.Drawing.Imaging;
using System.Text;
using System.Windows.Forms;

static class Program
{
    static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
    [STAThread]static void Main(string[] args)
    {
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);
        string output=Path.GetFullPath(args[0]);Directory.CreateDirectory(output);int limit=args.Length>1?int.Parse(args[1]):21;
        Check(PremiumTemplates.All.Count==21,"Missing premium templates");Check(PremiumTemplates.All.Select(t=>t.Id).Distinct().Count()==21,"Duplicate IDs");
        using var sheet=new Bitmap(5*260,((limit+4)/5)*292);using var g=Graphics.FromImage(sheet);g.Clear(Color.FromArgb(12,15,24));using var font=new Font("Segoe UI",13,FontStyle.Bold);
        var hashes=new HashSet<uint>();int i=0;
        foreach(var template in PremiumTemplates.All.Take(limit))
        {
            var design=DesignFiles.Parse(DesignFiles.Serialize(template.Design));Check(design.Widgets.Count is >=5 and <=64,"Invalid editable elements: "+design.Name);
            Check(design.Widgets.Any(w=>w.Kind==WidgetKind.CpuTemperature||w.BarSource==WidgetKind.CpuLoad),"CPU binding missing: "+design.Name);
            using var renderer=new DesignRenderer();var demo=ScreenReadings.Demo with{Now=new DateTime(2026,11,3,21,48,35,125),CpuClock=4100,GpuClock=2300};
            using var image=renderer.Render(design,demo);Check(image.GetPixel(0,0).ToArgb()==Color.Black.ToArgb(),"Circular crop missing");hashes.Add(CustomScreenSender.Hash(DesignRenderer.Rgb565(image)));
            image.Save(Path.Combine(output,$"{i+1:D2}-{design.Name.Replace(" ","-")}.png"),ImageFormat.Png);g.DrawImage(image,i%5*260+20,i/5*292+6,220,220);g.DrawString(design.Name,font,Brushes.White,i%5*260+14,i/5*292+235);
            using var missing=renderer.Render(design,new(null,null,null,null,null,null,demo.Now));Check(CustomScreenSender.Hash(DesignRenderer.Rgb565(missing))!=CustomScreenSender.Hash(DesignRenderer.Rgb565(image)),"Template ignores actual values: "+design.Name);
            string package=Path.Combine(output,"template-"+(i+1)+".qscreenpack");QscreenPackage.Export(package,design,template.Manifest);var restored=QscreenPackage.Import(package);Check(restored.Manifest.TemplateId==template.Id&&restored.Design.Widgets.Count==design.Widgets.Count,"Package roundtrip lost metadata/widgets");
            var snapshot=DesignSnapshot.Capture(design).Restore();snapshot.Widgets[0].X+=3;Check(snapshot.Widgets[0].X!=design.Widgets[0].X,"Editing changed original template");
            Check(snapshot.AnimationSpeed==design.AnimationSpeed&&snapshot.AnimationsEnabled==design.AnimationsEnabled,"Animation options lost in history");
            if(DesignRenderer.IsAnimated(design)){using var later=renderer.Render(design,demo with{Now=demo.Now.AddSeconds(2)});Check(CustomScreenSender.Hash(DesignRenderer.Rgb565(later))!=CustomScreenSender.Hash(DesignRenderer.Rgb565(image)),"Animation does not move: "+design.Name);}
            Console.WriteLine("PASS: "+design.Name+" / editable, bindings, missing data, package, preview");i++;
        }
        Check(hashes.Count==limit,"Templates are not visually distinct");sheet.Save(Path.Combine(output,"contact-sheet.png"),ImageFormat.Png);
        var widget=new ScreenWidget{Opacity=50,ColorRules=[new(){Minimum=60,Color=Color.Orange},new(){Minimum=80,Color=Color.Red}]};
        Check(PremiumDrawing.ColorFor(widget,65,Color.Blue,DateTime.Now,false).R==Color.Orange.R,"Color thresholds failed");Check(PremiumDrawing.ColorFor(widget,null,Color.Blue,DateTime.Now,false).B==255,"Missing sensor applied threshold");Check(PremiumDrawing.ColorFor(widget,85,Color.Blue,DateTime.Now,false).A==127,"Opacity failed");
        var meterDesign=new ScreenDesign{Widgets=[new(){Kind=WidgetKind.CircularGauge,BarSource=WidgetKind.CpuLoad,X=100,Y=100,Width=200,Height=200}]};using(var renderer=new DesignRenderer())
        {var r=ScreenReadings.Demo with{CpuLoad=0,Now=new DateTime(2026,11,3,12,0,0)};using var start=renderer.Render(meterDesign,r);using var changed=renderer.Render(meterDesign,r with{CpuLoad=100});Check(renderer.HasPendingMotion,"Gauge changed abruptly instead of interpolating");using var end=renderer.Render(meterDesign,r with{CpuLoad=100,Now=r.Now.AddSeconds(1)});Check(!renderer.HasPendingMotion,"Gauge animation did not settle");}
        string liveWeather=Path.Combine(output,"live-weather.json");if(File.Exists(liveWeather))Check(float.IsFinite(WeatherService.Parse(File.ReadAllBytes(liveWeather),DateTime.UtcNow).Temperature),"Live provider schema incompatible");
        var weather=WeatherService.Parse(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(new{current=new{temperature_2m=12.5,weather_code=3,is_day=0,time=DateTime.UtcNow.ToString("s")}}),DateTime.UtcNow);Check(weather.Temperature==12.5f&&!weather.Day,"Weather provider response failed");using var service=new WeatherService(output);Check(service.Poll(DateTime.UtcNow)==null,"Unconfigured weather invented data");
        if(limit==21)
        {
            using var gallery=new TemplatesWindow(output);gallery.Show();for(int n=0;n<160;n++){Application.DoEvents();Thread.Sleep(25);}using var capture=new Bitmap(gallery.Width,gallery.Height);gallery.DrawToBitmap(capture,new Rectangle(Point.Empty,capture.Size));capture.Save(Path.Combine(output,"gallery.png"));var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            var cards=(FlowLayoutPanel)typeof(TemplatesWindow).GetField("cards",flags)!.GetValue(gallery)!;
            Check(cards.Controls.Count>=21,"Gallery did not load the full catalog");
            Button Favorite(Control control)=>control.Controls.Cast<Control>().OfType<FlowLayoutPanel>().SelectMany(p=>p.Controls.Cast<Control>()).OfType<Button>().First(b=>b.Text.Contains("Favoriet"));
            Favorite(cards.Controls[0]).PerformClick();var only=(CheckBox)typeof(TemplatesWindow).GetField("favoritesOnly",flags)!.GetValue(gallery)!;only.Checked=true;Application.DoEvents();Check(cards.Controls.Count==1,"Favorite filter failed");Favorite(cards.Controls[0]).PerformClick();Check(cards.Controls.Count==0,"Unfavoriting left a stale card");
            Check(File.Exists(Path.Combine(output,"template-favorites.json")),"Favorites were not persisted");gallery.Close();
            using var editor=new DesignWindow(output,()=>{},PremiumTemplates.All[0].Design,_=>{});editor.Show();Application.DoEvents();using var capture2=new Bitmap(editor.Width,editor.Height);editor.DrawToBitmap(capture2,new Rectangle(Point.Empty,capture2.Size));capture2.Save(Path.Combine(output,"editor.png"));editor.Close();
            string profile=new ProfileRepository(output).Add(PremiumTemplates.All[20].Design);new StudioConfiguration{ManualProfileId=profile}.Save(output);using var main=new SettingsWindow(output);var active=(ScreenDesign?)typeof(SettingsWindow).GetField("ownPreview",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.GetValue(main);Check(active?.Name=="Tactical FPS","Main window did not preview the chosen template profile");
        }
        if(limit==21)
        {
            using var shell=new StudioShellWindow(output);shell.Show();
            for(int n=0;n<160;n++){Application.DoEvents();Thread.Sleep(25);}
            var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            var home=(TemplatesWindow)typeof(StudioShellWindow).GetField("gallery",flags)!.GetValue(shell)!;
            Check(home.Visible&&!home.TopLevel&&home.LoadedCount>=21,"Shell must start with embedded gallery without login");
            typeof(StudioShellWindow).GetMethod("NewDesign",flags)!.Invoke(shell,null);Application.DoEvents();
            var designer=(DesignWindow)typeof(StudioShellWindow).GetField("editor",flags)!.GetValue(shell)!;
            Check(designer.Visible&&!designer.TopLevel,"Designer opened an independent window");
            var name=(TextBox)typeof(DesignWindow).GetField("name",flags)!.GetValue(designer)!;name.Text="Niet opgeslagen ontwerp";Check(designer.HasUnsavedChanges,"A pending name edit was not protected");
            typeof(StudioShellWindow).GetMethod("Home",flags)!.Invoke(shell,null);Application.DoEvents();
            Check(!designer.IsDisposed&&ReferenceEquals(designer,typeof(StudioShellWindow).GetField("editor",flags)!.GetValue(shell)),"Back navigation discarded the editor");
            Check(designer.HasUnsavedChanges&&name.Text=="Niet opgeslagen ontwerp","Back lost unsaved work");typeof(StudioShellWindow).GetMethod("ResumeDesign",flags)!.Invoke(shell,null);Application.DoEvents();Check(designer.Visible&&designer.HasUnsavedChanges,"Resume did not restore unsaved editor");designer.SaveForNavigation();
            var template=NeonTemplates.All[22];int originalX=template.Design.Widgets[0].X;
            typeof(StudioShellWindow).GetMethod("Preview",flags)!.Invoke(shell,[template,false]);Application.DoEvents();
            var preview=(TemplatePreviewWindow)typeof(StudioShellWindow).GetField("transient",flags)!.GetValue(shell)!;Check(preview.Visible&&!preview.TopLevel,"Preview opened outside the shell");preview.EditRequested!();Application.DoEvents();
            var copyEditor=(DesignWindow)typeof(StudioShellWindow).GetField("editor",flags)!.GetValue(shell)!;var copy=(ScreenDesign)typeof(DesignWindow).GetField("design",flags)!.GetValue(copyEditor)!;copy.Widgets[0].X++;copyEditor.SaveForNavigation();Check(template.Design.Widgets[0].X==originalX,"Editing a copy changed the official template");
            typeof(StudioShellWindow).GetMethod("Home",flags)!.Invoke(shell,null);Application.DoEvents();
            using var capture=new Bitmap(shell.Width,shell.Height);shell.DrawToBitmap(capture,new Rectangle(Point.Empty,capture.Size));capture.Save(Path.Combine(output,"studio-shell.png"));
            shell.Close();Console.WriteLine("PASS: shell opens gallery without login, internal designer and back preserves editor.");
        }
        Console.WriteLine("PASS: premium batch "+limit+", original artwork, color rules, opacity, weather fallback and UI.");
    }
}

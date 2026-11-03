using System.Reflection;
using System.Windows.Forms;
using System.Drawing;

internal static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        string source = Path.GetFullPath(args[0]);
        string root = Path.Combine(source, "tool-cache", "settings-test");
        Directory.CreateDirectory(Path.Combine(root, "previews"));
        Directory.CreateDirectory(Path.Combine(root, "branding"));
        foreach (string file in Directory.GetFiles(Path.Combine(source, "branding")))
            File.Copy(file, Path.Combine(root, "branding", Path.GetFileName(file)), true);
        for (int i = 0; i < LayoutSettings.Names.Length; i++)
        {
            string id = LayoutSettings.Names[i];
            if (i < 3 || i >= 26)
                File.Copy(Path.Combine(source, "layout-previews", $"layout-{id}.png"), Path.Combine(root, "previews", $"layout-{id}.png"), true);
            else for (int frame = 0; frame < 2; frame++)
            {
                File.Copy(Path.Combine(source, "preview-animation", $"{id}-{frame:D2}.bmp"), Path.Combine(root, "previews", $"{id}-{frame:D2}.bmp"), true);
                string hd=Path.Combine(source,"app-previews",$"{id}-{frame:D2}.png");
                if(File.Exists(hd))File.Copy(hd,Path.Combine(root,"previews",Path.GetFileName(hd)),true);
            }
        }
        using(var cache=new PreviewImageCache(2))
        {
            string file=Path.Combine(root,"previews","layout-classic.png");
            var first=cache.Get(file);File.Move(file,file+".backup",true);
            try{if(!ReferenceEquals(first,cache.Get(file)))throw new Exception("Cached image reopened the file");}
            finally{File.Move(file+".backup",file,true);}
            cache.Get(Path.Combine(root,"previews","layout-gauges.png"));cache.Get(Path.Combine(root,"previews","layout-minimal.png"));
            if(cache.Count!=2)throw new Exception("Preview cache exceeded its memory bound");
        }
        File.WriteAllText(Path.Combine(root, "layout.txt"), "minimal");
        Application.EnableVisualStyles();
        using var window = new SettingsWindow(root);
        T Field<T>(string name) => (T)typeof(SettingsWindow).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(window)!;
        void Call(string name) => typeof(SettingsWindow).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(window, null);
        var choices = Field<ComboBox>("layouts");
        var image = Field<PictureBox>("preview");
        if ((string)choices.SelectedItem! != "minimal") throw new Exception("Saved layout not restored");
        for (int i = 0; i < 31; i++)
        {
            choices.SelectedIndex = i;
            if (image.Image == null) throw new Exception("Missing preview");
            bool animated = i >= 3 && i < 26;
            if (Field<Button>("play").Enabled != animated) throw new Exception("Wrong animation state");
            Call("Apply");
            if (LayoutSettings.Read(root) != LayoutSettings.Names[i]) throw new Exception("Layout save failed");
            if (animated)
            {
                typeof(SettingsWindow).GetField("frame", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(window, 1);
                Call("ShowFrame");
                if (image.Image == null) throw new Exception("Second frame missing");
            }
        }
        choices.SelectedItem = "drone-swarm";
        window.Show();
        var playback = Field<System.Windows.Forms.Timer>("timer");
        playback.Stop();
        typeof(SettingsWindow).GetField("frame", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(window, 0);
        playback.Start();
        var elapsed = System.Diagnostics.Stopwatch.StartNew();
        while (Field<int>("frame") == 0 && elapsed.ElapsedMilliseconds < 2000) { Application.DoEvents(); Thread.Sleep(5); }
        if (Field<int>("frame") != 1) throw new Exception("Real UI timer did not advance");
        Field<Button>("play").PerformClick();
        elapsed.Restart();
        while (elapsed.ElapsedMilliseconds < 140) { Application.DoEvents(); Thread.Sleep(5); }
        if (Field<int>("frame") != 1 || !Field<bool>("paused")) throw new Exception("Pause did not freeze playback");
        Field<Button>("play").PerformClick();
        if (Field<bool>("paused")) throw new Exception("Resume did not clear pause");
        playback.Stop();
        // Render the real WinForms control tree without changing installation or hardware.
        using var screenshot = new Bitmap(window.Width, window.Height);
        window.DrawToBitmap(screenshot, new Rectangle(Point.Empty, window.Size));
        screenshot.Save(Path.Combine(root, "layout-studio.png"));
        window.Close();
        DesignFiles.Apply(root,ScreenDesign.CreateDefault());
        using(var customWindow=new SettingsWindow(root))
        {
            bool custom=(bool)typeof(SettingsWindow).GetField("showingCustom",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(customWindow)!;
            if(!custom)throw new Exception("Active custom design not restored in main window");
            typeof(SettingsWindow).GetMethod("Apply",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(customWindow,null);
            if(File.Exists(Path.Combine(root,"custom-active.txt")))throw new Exception("Preset did not deactivate custom mode");
            if((bool)typeof(SettingsWindow).GetField("showingCustom",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(customWindow)!)throw new Exception("Main preview did not return to preset");
        }
        Console.WriteLine("PASS: 31 previews, 23 animations, 8 static layouts, saved choice, configuration writes, real timer playback/pause/resume; UI rendered.");
    }
}

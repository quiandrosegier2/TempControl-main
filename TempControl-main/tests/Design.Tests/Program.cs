using System.Drawing;
using System.Drawing.Imaging;
using System.Reflection;
using System.Windows.Forms;

static class Program
{
    static void Assert(bool condition,string message){if(!condition)throw new Exception(message);}
    static void Reject(Action action,string message){try{action();}catch(Exception ex) when(ex is InvalidDataException or IOException or ArgumentException){return;}throw new Exception(message);}
    [STAThread]
    static void Main(string[] args)
    {
        string output=Path.GetFullPath(args[0]);Directory.CreateDirectory(output);
        string work=Path.Combine(output,"design-test");Directory.CreateDirectory(work);
        var design=ScreenDesign.CreateDefault();
        design.Name="Saved ✓";
        using(var background=new Bitmap(600,400))
        {
            using(var g=Graphics.FromImage(background))
            using(var gradient=new System.Drawing.Drawing2D.LinearGradientBrush(new Point(0,0),new Point(600,400),Color.MidnightBlue,Color.DarkRed))
                g.FillRectangle(gradient,0,0,600,400);
            using var png=new MemoryStream();background.Save(png,ImageFormat.Png);design.BackgroundPng=Convert.ToBase64String(png.ToArray());
        }
        design.Widgets.Add(new(){Name="Datum",Kind=WidgetKind.Date,Format="ddd dd MMM",X=106,Y=90,FontSize=18});
        design.Widgets.Add(new(){Name="CPU belasting",Kind=WidgetKind.ProgressBar,X=76,Y=267,Width=260,Height=8,Foreground=Color.Cyan});
        DesignFiles.Apply(work,design);
        var loaded=DesignFiles.Load(Path.Combine(work,"custom-design.json"));
        Assert(loaded.Name==design.Name&&loaded.BackgroundPng==design.BackgroundPng,"Portable background or Unicode did not roundtrip");
        Assert(File.Exists(Path.Combine(work,"custom-active.txt")),"Apply did not activate custom design");
        using var renderer=new DesignRenderer();
        using var frame=renderer.Render(loaded,new(42,58,165,35,76,52,new DateTime(2026,11,2,13,14,15)));
        frame.Save(Path.Combine(output,"Qscreen-Designer-Preview.png"));
        Assert(frame.GetPixel(0,0).ToArgb()==Color.Black.ToArgb(),"Circular clipping failed");
        var bytes=DesignRenderer.Rgb565(frame);
        Assert(bytes.Length==339488,"Incorrect RGB565 length");
        using(var red=new Bitmap(412,412)){using(var g=Graphics.FromImage(red))g.Clear(Color.Red);var raw=DesignRenderer.Rgb565(red);Assert(raw[0]==0&&raw[1]==248,"RGB565 byte order incorrect");}
        Assert(CustomScreenSender.Hash("hello"u8.ToArray())==0x4f9f2cab,"FNV-1a test vector failed");
        var connection=new FakeConnection(new[]{"QSCREEN_FRAME2","FRAMEREADY"}.Concat(Enumerable.Repeat("FRAMECHUNK",(bytes.Length-1)/1024)).Append("FRAMEOK"));
        Assert(new CustomScreenSender(connection).Send(bytes),"Transfer failed");
        Assert(connection.Commands[0]=="CAPS"&&connection.Commands[1]==$"FRAME2=339488,{CustomScreenSender.Hash(bytes)}","Handshake or checksum header incorrect");
        Assert(connection.Payload.SequenceEqual(bytes),"Chunked transfer corrupted payload");
        var old=new FakeConnection([]);var sender=new CustomScreenSender(old);
        Assert(!sender.Send(bytes)&&!sender.Send(bytes)&&old.Commands.Count==1&&old.Payload.Count==0,"Old firmware was sent binary data");
        Reject(()=>new CustomScreenSender(new FakeConnection(["QSCREEN_FRAME2","FRAMEERR memory"])).Send(bytes),"Receiver rejection ignored");
        var previous=new FakeConnection(["QSCREEN_FRAME1"]);
        Assert(!new CustomScreenSender(previous).Send(bytes)&&previous.Payload.Count==0,"Old frame protocol received binary data");
        DateTime now=DateTime.UtcNow;
        Assert(!ScreenDelivery.Describe(new(now,"other","received",""),"mine",now,now).Contains("weergegeven"),"Another design was marked received");
        Assert(!ScreenDelivery.Describe(new(now.AddSeconds(-2),"mine","received",""),"mine",now,now).Contains("weergegeven"),"Stale acknowledgment was accepted");
        Assert(ScreenDelivery.Describe(new(now,"mine","received",""),"mine",now,now).Contains("weergegeven"),"Matching frame acknowledgment missing");
        Assert(ScreenDelivery.Describe(new(now,"mine","firmware-required",""),"mine",now,now).Contains("Firmware bijwerken"),"Firmware update instruction missing");
        Assert(ScreenDelivery.Describe(null,"mine",now.AddMinutes(-1),now).Contains("geen bevestiging"),"Stopped monitor falsely reported success");
        ScreenDelivery.Write(work,"mine","received","FRAMEOK");
        Assert(ScreenDelivery.Read(work)?.Revision=="mine","Delivery status did not roundtrip");
        Reject(()=>DesignFiles.Parse("{\"Version\":2}"),"Unknown schema accepted");
        Reject(()=>DesignFiles.Parse("{\"Widgets\":null}"),"Null widget collection accepted");
        Reject(()=>DesignFiles.Parse("{\"BackgroundPng\":\"invalid\"}"),"Bad background accepted");
        Reject(()=>DesignFiles.Parse("{\"Widgets\":[{\"Kind\":999}]}"),"Unknown widget accepted");
        var clamp=DesignFiles.Parse("{\"Darken\":400,\"Widgets\":[{\"X\":9999,\"Width\":-1,\"FontSize\":500}]}");
        Assert(clamp.Darken==100&&clamp.Widgets[0].X==412&&clamp.Widgets[0].Width==1&&clamp.Widgets[0].FontSize==160,"Unsafe geometry not clamped");
        // Exercise all widget renderers, unknown fonts, missing sensors and bad clock formats.
        foreach(var kind in Enum.GetValues<WidgetKind>())
        {
            var test=new ScreenDesign { Widgets=[new(){Kind=kind,Format=kind==WidgetKind.Time?"%":"{value}",FontFamily="Missing font family 123"}] };
            using var rendered=renderer.Render(test,new(null,null,null,null,null,null,DateTime.Now));
            Assert(rendered.Width==412,"Widget rendering failed: "+kind);
        }
        design.Name="Qscreen / Night Ops";
        DesignFiles.Save(Path.Combine(work,"custom-draft.json"),design);
        Application.EnableVisualStyles();
        using var window=new DesignWindow(work,()=>{});
        T Field<T>(string field)=>(T)typeof(DesignWindow).GetField(field,BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(window)!;
        void Call(string method,params object[] values)=>typeof(DesignWindow).GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(window,values);
        int count=Field<ScreenDesign>("design").Widgets.Count;
        Call("AddWidget",WidgetKind.MemoryLoad);Assert(Field<ScreenDesign>("design").Widgets.Count==count+1,"Add failed");
        Call("Undo");Assert(Field<ScreenDesign>("design").Widgets.Count==count,"Undo failed");
        Call("Redo");Assert(Field<ScreenDesign>("design").Widgets.Count==count+1,"Redo failed");
        Field<ListBox>("elements").SelectedIndex=count;
        Call("Delete");Assert(Field<ScreenDesign>("design").Widgets.Count==count,"Delete failed");
        var elements=Field<ListBox>("elements");elements.SelectedIndex=1;
        Call("Duplicate");Assert(Field<ScreenDesign>("design").Widgets.Count==count+1,"Duplicate failed");
        Call("Undo");
        Call("Apply");Assert(File.Exists(Path.Combine(work,"custom-active.txt")),"UI apply failed");
        window.Show();Application.DoEvents();
        var canvas=Field<PictureBox>("canvas");
        var bounds=(RectangleF)typeof(DesignWindow).GetProperty("ScreenBounds",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(window)!;
        float scale=bounds.Width/412;
        int sx=(int)(bounds.X+80*scale),sy=(int)(bounds.Y+300*scale);
        int before=Field<ScreenDesign>("design").Widgets.First(w=>w.Name=="CPU").X;
        Call("MouseDownCanvas",canvas,new MouseEventArgs(MouseButtons.Left,1,sx,sy,0));
        Call("MouseMoveCanvas",canvas,new MouseEventArgs(MouseButtons.Left,1,sx+(int)(20*scale),sy,0));
        Call("MouseUpCanvas",canvas,new MouseEventArgs(MouseButtons.Left,1,sx+(int)(20*scale),sy,0));
        Assert(Field<ScreenDesign>("design").Widgets.First(w=>w.Name=="CPU").X>before+17,"Drag failed at preview scale");
        Call("Undo");Assert(Field<ScreenDesign>("design").Widgets.First(w=>w.Name=="CPU").X==before,"Drag undo failed");
        using var screenshot=new Bitmap(window.Width,window.Height);window.DrawToBitmap(screenshot,new Rectangle(Point.Empty,window.Size));screenshot.Save(Path.Combine(output,"Qscreen-Designer.png"));
        Call("SaveDraft");window.Close();
        Console.WriteLine("PASS: portable designs/backgrounds; RGB565/checksum/chunking; old firmware fallback and rejection; schema/geometry validation; 12 widget types; designer add/delete/duplicate/undo/redo/apply; UI rendered.");
    }
    sealed class FakeConnection(IEnumerable<string> replies) : IFrameConnection
    {
        readonly Queue<string> responses=new(replies);
        bool mayWrite;
        public List<string> Commands=[];
        public List<byte> Payload=[];
        public void WriteLine(string line)=>Commands.Add(line);
        public void Write(byte[] bytes,int offset,int count)
        {
            Assert(count<=1024,"Transfer block exceeds safe RX budget");
            Assert(mayWrite,"Sender skipped chunk acknowledgment"); mayWrite=false;
            Payload.AddRange(bytes.Skip(offset).Take(count));
        }
        public string ReadLine()
        {
            string reply=responses.Count>0?responses.Dequeue():throw new TimeoutException();
            if(reply is "FRAMEREADY" or "FRAMECHUNK")mayWrite=true;
            return reply;
        }
    }
}

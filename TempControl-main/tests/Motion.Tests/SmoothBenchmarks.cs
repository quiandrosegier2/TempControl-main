using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Text.Json;

static class SmoothBenchmarks
{
    public static void Prepare(string output,byte[] gif)
    {
        var source=Path.Combine(output,"benchmark-fixtures");Directory.CreateDirectory(source);var frames=new List<string>();
        for(int i=0;i<12;i++){using var image=new Bitmap(412,412);using(var g=Graphics.FromImage(image)){using var brush=new LinearGradientBrush(new Rectangle(0,0,412,412),Color.FromArgb(10+i*6,12,45+i*8),Color.FromArgb(10,90+i*9,120),i*30);g.FillRectangle(brush,0,0,412,412);using var pen=new Pen(Color.FromArgb(130,255,30+i*12,100),8);for(int x=-412;x<824;x+=70)g.DrawLine(pen,x+i*5,0,x+300+i*5,412);}string path=Path.Combine(source,$"{i:D4}.png");image.Save(path,ImageFormat.Png);frames.Add(path);}
        string background=Convert.ToBase64String(MotionAsset.Pack(frames,120)),ghost=Convert.ToBase64String(gif);
        MotionLayer Character(int x=206,int y=206,int size=220)=>new(){Name="Character",ImageData=ghost,Width=size,Height=size,X=x,Y=y,Duration=1.44};
        MotionLayer Background()=>new(){Name="Full background",ImageData=background,Width=412,Height=412,Duration=1.44};
        List<ScreenWidget> Widgets()=>[new(){Name="Clock",Kind=WidgetKind.Time,Format="HH:mm:ss",X=100,Y=45,Width=212,Height=44},new(){Name="CPU",Kind=WidgetKind.CpuTemperature,Format="CPU {value}",X=56,Y=320,Width=150,FontSize=20},new(){Name="GPU",Kind=WidgetKind.GpuTemperature,Format="GPU {value}",X=206,Y=320,Width=150,FontSize=20},new(){Name="FPS",Kind=WidgetKind.Fps,Format="{value} FPS",X=130,Y=270,Width=152,FontSize=24}];
        var loop=Character();loop.Duration=2.88;loop.Keyframes=[new(){Seconds=0,X=160,Y=200},new(){Seconds=1.44,X=250,Y=200},new(){Seconds=2.88,X=160,Y=200}];
        var cases=new (string Name,ScreenDesign Design)[]{("small-character",new(){MotionLayers=[Character()]}),("full-background",new(){MotionLayers=[Background()]}),("background-widgets",new(){MotionLayers=[Background()],Widgets=Widgets()}),("multiple-sprites",new(){MotionLayers=[Character(140,150,150),Character(270,160,150),Character(206,260,150)],Widgets=Widgets()}),("seamless-loop",new(){MotionLayers=[loop],Widgets=Widgets()}),("static-layout",ScreenDesign.CreateDefault())};
        var repository=new ProfileRepository(output);var manifest=new List<object>();
        foreach(var item in cases){item.Design.Name="Smooth benchmark / "+item.Name;item.Design.Darken=0;item.Design.StreamingQuality=AnimationQuality.Maximum;manifest.Add(new{Name=item.Name,ProfileId=repository.Add(item.Design)});}
        File.WriteAllText(Path.Combine(output,"benchmark-cases.json"),JsonSerializer.Serialize(manifest,DesignFiles.Json));
        Console.WriteLine("Prepared six repeatable physical benchmarks with original fixtures; telemetry remains live.");
    }
}

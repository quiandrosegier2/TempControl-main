using System.Drawing;

static class PremiumTemplates
{
    static readonly Color Blue=Color.FromArgb(66,193,255),Pink=Color.FromArgb(255,67,157),Purple=Color.FromArgb(165,100,255),Muted=Color.FromArgb(148,165,187);
    static readonly Lazy<IReadOnlyList<DesignTemplate>> catalog=new(Build);
    public static IReadOnlyList<DesignTemplate> All=>catalog.Value;
    static ScreenWidget Text(ScreenDesign d,string name,string text,int x,int y,int width,int height,float size,Color color,WidgetKind kind=WidgetKind.Text)
    {var w=new ScreenWidget{Name=name,Kind=kind,Format=text,X=x,Y=y,Width=width,Height=height,FontSize=size,Foreground=color,TemperatureColors=false};d.Widgets.Add(w);return w;}
    static void Title(ScreenDesign d,string title,Color color,int y=52)=>Text(d,"Titel",title,70,y,272,25,13,color);
    static void Clock(ScreenDesign d,int y=118,float size=54)=>Text(d,"Digitale klok","HH:mm",46,y,320,80,size,Color.White,WidgetKind.Time);
    static void Fps(ScreenDesign d,int y=123,float size=80){Text(d,"FPS","{value}",76,y,260,96,size,Color.White,WidgetKind.Fps);Text(d,"FPS label","FRAMES / SEC",100,y+85,212,24,12,Muted);}
    static void Sensor(ScreenDesign d,WidgetKind source,string label,int x,int y,int width=128,Color? color=null,int size=22)
    {Text(d,label+" label",label,x,y,width,20,11,Muted);var w=Text(d,label,"{value}"+(source is WidgetKind.CpuTemperature or WidgetKind.GpuTemperature?" °C":source is WidgetKind.CpuClock or WidgetKind.GpuClock?" MHz":source==WidgetKind.Fps?"":" %"),x,y+18,width,35,size,color??Blue,source);if(source is WidgetKind.CpuTemperature or WidgetKind.GpuTemperature)w.ColorRules=[new(){Minimum=-100,ColorArgb=(color??Blue).ToArgb()},new(){Minimum=60,ColorArgb=Color.Orange.ToArgb()},new(){Minimum=80,ColorArgb=Color.OrangeRed.ToArgb(),Pulse=true}];}
    static ScreenWidget Ring(ScreenDesign d,string name,WidgetKind source,int x,int y,int size,Color color,int stroke=8,int max=100,float start=-90,float sweep=360)
    {var w=new ScreenWidget{Name=name,Kind=WidgetKind.CircularGauge,BarSource=source,X=x,Y=y,Width=size,Height=size,Foreground=color,Fill=Color.FromArgb(35,40,52),TemperatureColors=false,StrokeWidth=stroke,Maximum=max,StartAngle=start,SweepAngle=sweep};d.Widgets.Add(w);return w;}
    static void Bar(ScreenDesign d,string name,WidgetKind source,int x,int y,int width,Color color)
    {d.Widgets.Add(new(){Name=name,Kind=WidgetKind.ProgressBar,BarSource=source,X=x,Y=y,Width=width,Height=5,Foreground=color,Fill=Color.FromArgb(40,45,57),TemperatureColors=false});}
    static void Panel(ScreenDesign d,int x,int y,int width,int height,Color color,bool angular=false,int opacity=85)
    {d.Widgets.Add(new(){Name="Paneel",Kind=WidgetKind.Panel,X=x,Y=y,Width=width,Height=height,Foreground=Color.FromArgb(100,color),Fill=Color.FromArgb(25,32,49),StrokeWidth=1,PanelShape=angular?PanelShape.Angular:PanelShape.Rounded,Opacity=opacity,TemperatureColors=false});}
    static void Pair(ScreenDesign d,int y=266){Sensor(d,WidgetKind.CpuTemperature,"CPU",66,y,132,Blue);Sensor(d,WidgetKind.GpuTemperature,"GPU",214,y,132,Pink);}
    static void Ram(ScreenDesign d,int y=335){Text(d,"RAM","RAM  {value} %",106,y,200,25,13,Muted,WidgetKind.MemoryLoad);Bar(d,"RAM balk",WidgetKind.MemoryLoad,126,y+26,160,Purple);}
    static IReadOnlyList<DesignTemplate> Build()
    {
        var result=new List<DesignTemplate>();
        void Add(int number,string name,TemplateCategory category,string description,Action<ScreenDesign> build,string? scene=null,PageAnimation animation=PageAnimation.None)
        {
            var d=new ScreenDesign{Name=name,BackgroundArgb=Color.Black.ToArgb(),Darken=0,Animation=animation};if(scene!=null)d.BackgroundPng=TemplateArtwork.Create(scene);build(d);
            var id=number.ToString("x32");result.Add(new(id,new(){TemplateId=id,Name=name,Category=category,Author="QPRINTS",Description=description,MinimumAppVersion="2.1.0",Animated=DesignRenderer.IsAnimated(d),RequiredAssets=scene==null?[]:["embedded:background"],CreatedUtc=new DateTime(2026,11,3,0,0,0,DateTimeKind.Utc)},d));
        }
        // Batch 1: five complete, editable compositions.
        Add(1,"Gaming Core",TemplateCategory.Gaming,"Neon HUD met een grote FPS-kern en losse sensorkaarten.",d=>{
            var a=Ring(d,"Neon buitenring",WidgetKind.MemoryLoad,17,17,378,Pink,3);a.Maximum=1;a.Motion=WidgetMotion.Breathe;
            var b=Ring(d,"Neon boog",WidgetKind.CpuLoad,28,28,356,Blue,2,100,-25,230);b.Motion=WidgetMotion.Rotate;
            Title(d,"QPRINTS / GAMING CORE",Pink);Fps(d);Panel(d,62,263,137,64,Blue);Panel(d,213,263,137,64,Pink);Pair(d,266);Ram(d);
        });
        Add(2,"Cyberpunk City",TemplateCategory.Cyberpunk,"Originele neonstad, digitale klok en roze sensorgloed.",d=>{
            Title(d,"NIGHT CITY / 02",Pink,63);Clock(d,112,58);Text(d,"Datum","ddd dd MMM",90,183,232,30,14,Color.Plum,WidgetKind.Date);Panel(d,72,258,268,63,Purple);Pair(d,260);Text(d,"FPS","{value} FPS",126,332,160,28,18,Pink,WidgetKind.Fps);
        },"city",PageAnimation.CityLights);
        Add(3,"Minimal Pro",TemplateCategory.Minimal,"Zwarte wijzerplaat, heldere typografie en twee dunne meetbogen.",d=>{
            Ring(d,"CPU boog",WidgetKind.CpuTemperature,32,32,348,Blue,3,100,130,110);Ring(d,"GPU boog",WidgetKind.GpuTemperature,32,32,348,Pink,3,100,-50,110);Title(d,"MINIMAL / PRO",Muted,72);Clock(d,128,58);Ram(d,224);Pair(d,292);
        });
        Add(4,"System Rings",TemplateCategory.Hardware,"Concentrische CPU-, GPU- en RAM-meters rondom FPS.",d=>{
            Ring(d,"CPU usage",WidgetKind.CpuLoad,28,28,356,Blue,10,100,-90,300);Ring(d,"GPU usage",WidgetKind.GpuLoad,48,48,316,Purple,10,100,-90,300);Ring(d,"RAM usage",WidgetKind.MemoryLoad,68,68,276,Pink,7,100,-90,300);Fps(d,134,68);Sensor(d,WidgetKind.CpuLoad,"CPU",93,264,108,Blue,18);Sensor(d,WidgetKind.GpuLoad,"GPU",211,264,108,Purple,18);Text(d,"RAM","RAM {value}%",122,321,168,25,13,Pink,WidgetKind.MemoryLoad);
        });
        Add(5,"Frost Glass",TemplateCategory.Nature,"IJsblauwe berglagen met transparante glazen informatiepanelen.",d=>{
            Title(d,"FROST / 05",Color.LightCyan,66);Panel(d,57,107,298,115,Color.LightBlue,false,65);Clock(d,114,57);Text(d,"Datum","dddd dd MMM",75,181,262,28,14,Color.LightCyan,WidgetKind.Date);Panel(d,60,256,292,71,Blue,false,65);Pair(d,263);Ram(d,337);
        },"frost");
        // Remaining batches extend the same project format; no flattened sensor values.
        Add(6,"Animated Waves",TemplateCategory.Animated,"Rustige vloeiende cyaan/paarse golven met instelbaar tempo.",d=>{Title(d,"FLOW / STUDIO",Purple,65);Clock(d,97,52);Panel(d,61,268,290,68,Blue);Sensor(d,WidgetKind.CpuTemperature,"CPU",69,274,88,Blue,18);Sensor(d,WidgetKind.GpuTemperature,"GPU",162,274,88,Purple,18);Sensor(d,WidgetKind.Fps,"FPS",255,274,88,Pink,18);},animation:PageAnimation.Waves);
        Add(7,"Hardware Pro",TemplateCategory.Hardware,"GPU-koeling, technische lijnen en temperatuur-, belasting- en klokdata.",d=>{Title(d,"HARDWARE / TELEMETRY",Blue,58);Text(d,"FPS","{value} FPS",116,92,180,49,32,Color.White,WidgetKind.Fps);Panel(d,55,155,302,144,Purple,true);Sensor(d,WidgetKind.CpuTemperature,"CPU CORE",65,163,136,Blue);Sensor(d,WidgetKind.GpuTemperature,"GPU CORE",211,163,136,Purple);Sensor(d,WidgetKind.CpuClock,"CPU CLOCK",65,222,136,Blue,16);Sensor(d,WidgetKind.GpuClock,"GPU CLOCK",211,222,136,Purple,16);Bar(d,"CPU load",WidgetKind.CpuLoad,70,290,122,Blue);Bar(d,"GPU load",WidgetKind.GpuLoad,220,290,122,Purple);Ram(d,330);},"hardware");
        Add(8,"Retro Pixel",TemplateCategory.Retro,"Eigen pixelzon, bergsilhouetten en terminaltypografie.",d=>{Title(d,"PIXEL / 1986",Color.FromArgb(255,189,102),65);Clock(d,111,51);Pair(d,274);Text(d,"FPS","{value} FPS",126,341,160,25,18,Color.LightPink,WidgetKind.Fps);foreach(var w in d.Widgets)w.FontFamily="Consolas";},"retro",PageAnimation.PixelStars);
        Add(9,"Analog Hybrid",TemplateCategory.Clock,"Analoge horlogewijzerplaat met vloeiende secondewijzer.",d=>{d.Widgets.Add(new(){Name="Analoge klok",Kind=WidgetKind.AnalogClock,X=61,Y=45,Width=290,Height=290,Foreground=Color.White,Fill=Color.FromArgb(10,13,19),Accent=Pink});Text(d,"Merk","QPRINTS",136,108,140,22,12,Muted);Text(d,"FPS","{value} FPS",147,240,118,32,18,Pink,WidgetKind.Fps);Sensor(d,WidgetKind.CpuTemperature,"CPU",83,315,112,Blue,18);Sensor(d,WidgetKind.GpuTemperature,"GPU",217,315,112,Pink,18);});
        Add(10,"Anime Style",TemplateCategory.Nature,"Originele reizigerssilhouet en een warme zonsondergang.",d=>{Title(d,"AFTERGLOW / JOURNEY",Color.PeachPuff,64);Clock(d,112,56);Pair(d,270);Text(d,"FPS","{value} FPS",126,338,160,28,19,Color.Orange,WidgetKind.Fps);},"anime",PageAnimation.Smoke);
        Add(11,"Space Explorer",TemplateCategory.SciFi,"Planeten, orbitale horizon en een originele astronauthelm.",d=>{Title(d,"ORBITAL / EXPLORER",Color.LightBlue,63);Clock(d,114,54);Panel(d,65,261,282,65,Blue,true);Pair(d,265);Ram(d,337);},"space",PageAnimation.Stars);
        Add(12,"Matrix Terminal",TemplateCategory.Animated,"Groene digitale regen met een duidelijk terminalvenster.",d=>{Title(d,"> SYSTEM ONLINE_",Color.LimeGreen,71);Panel(d,60,119,292,119,Color.LimeGreen,true,90);Clock(d,126,50);Text(d,"Datum","yyyy.MM.dd",96,197,220,24,15,Color.LimeGreen,WidgetKind.Date);Pair(d,272);foreach(var w in d.Widgets){w.FontFamily="Consolas";w.Foreground=Color.LimeGreen;w.ColorRules=[];}},animation:PageAnimation.DigitalRain);
        Add(13,"Performance Gauge",TemplateCategory.Hardware,"Grote gesegmenteerde halve FPS-meter met wijzer en belastingbalken.",d=>{var ring=Ring(d,"FPS meter",WidgetKind.Fps,39,60,334,Blue,14,240,180,180);ring.Segments=32;ring.Needle=true;ring.Accent=Pink;Fps(d,157,67);Sensor(d,WidgetKind.CpuLoad,"CPU",81,274,115,Blue,19);Sensor(d,WidgetKind.GpuLoad,"GPU",216,274,115,Pink,19);Ram(d,335);});
        Add(14,"Minimal Clock",TemplateCategory.Minimal,"Monochrome bergkam, luchtige klok en dunne scheidingslijnen.",d=>{Clock(d,91,61);Text(d,"Datum","dddd dd MMMM",76,166,260,28,15,Color.Silver,WidgetKind.Date);Sensor(d,WidgetKind.CpuTemperature,"CPU",73,280,84,Color.White,16);Sensor(d,WidgetKind.GpuTemperature,"GPU",164,280,84,Color.White,16);Sensor(d,WidgetKind.MemoryLoad,"RAM",255,280,84,Color.White,16);},"mono");
        Add(15,"RGB Pulse",TemplateCategory.Animated,"Kleurwisselende ring met een echte FPS-historiegrafiek.",d=>{var ring=Ring(d,"RGB ring",WidgetKind.MemoryLoad,17,17,378,Blue,7,1);ring.Motion=WidgetMotion.RgbCycle;ring.Accent=Pink;Clock(d,70,43);Pair(d,176);Text(d,"FPS","{value} FPS",116,242,180,33,26,Color.White,WidgetKind.Fps);d.Widgets.Add(new(){Name="FPS historie",Kind=WidgetKind.HistoryGraph,BarSource=WidgetKind.Fps,X=105,Y=294,Width=202,Height=55,Foreground=Purple,Fill=Color.Black});});
        Add(16,"Car Dashboard",TemplateCategory.Gaming,"Rode toerentellerschaal met centrale FPS-snelheidsmeter.",d=>{var ring=Ring(d,"Toerenteller",WidgetKind.Fps,31,31,350,Color.OrangeRed,8,240,140,260);ring.Segments=48;ring.Needle=true;ring.Accent=Pink;Title(d,"PERFORMANCE / RPM",Color.Silver,89);Fps(d,139,79);Pair(d,283);Text(d,"Versnelling","SPORT / USB",129,344,154,21,12,Color.OrangeRed);},"metal");
        Add(17,"Weather + System",TemplateCategory.Nature,"Echt configureerbaar weer met dag/nachtpictogram en systeemwaarden.",d=>{Clock(d,69,51);d.Widgets.Add(new(){Name="Weerpictogram",Kind=WidgetKind.WeatherIcon,X=115,Y=161,Width=62,Height=58,Foreground=Color.LightSkyBlue,Fill=Color.FromArgb(8,24,47)});Text(d,"Weertemperatuur","{value} °C",182,161,120,56,32,Color.White,WidgetKind.WeatherTemperature);Text(d,"Provider","OPEN-METEO / -- zonder configuratie",66,225,280,22,10,Color.LightBlue);Sensor(d,WidgetKind.CpuTemperature,"CPU",73,279,84,Blue,16);Sensor(d,WidgetKind.GpuTemperature,"GPU",164,279,84,Blue,16);Sensor(d,WidgetKind.MemoryLoad,"RAM",255,279,84,Blue,16);},"weather");
        Add(18,"Game HUD",TemplateCategory.SciFi,"Hoekige cyaanpanelen, centrale FPS en draaiende tactische accenten.",d=>{Panel(d,94,103,224,145,Blue,true);var ring=Ring(d,"HUD scanboog",WidgetKind.CpuLoad,28,28,356,Blue,2,1,20,65);ring.Motion=WidgetMotion.Rotate;Title(d,"TACTICAL / LINK ACTIVE",Blue,64);Fps(d,120,76);Panel(d,50,265,146,65,Blue,true);Panel(d,216,265,146,65,Blue,true);Pair(d,270);Ram(d,341);});
        Add(19,"Aurora",TemplateCategory.Nature,"Noorderlicht boven donkere dennen en een rustige horizon.",d=>{Title(d,"POLAR / NIGHT",Color.Aquamarine,66);Clock(d,113,57);Pair(d,273);Text(d,"FPS","{value} FPS",126,340,160,28,18,Color.Aquamarine,WidgetKind.Fps);},"aurora",PageAnimation.Aurora);
        Add(20,"Clean Dashboard",TemplateCategory.Minimal,"Drie gelijk verdeelde ronde meters met klok en datum.",d=>{Clock(d,72,56);Text(d,"Datum","ddd dd MMMM",76,147,260,28,15,Muted,WidgetKind.Date);int i=0;foreach(var source in new[]{WidgetKind.CpuTemperature,WidgetKind.GpuTemperature,WidgetKind.MemoryLoad}){int x=63+i*100;Ring(d,"Meter "+source,source,x,230,86,Color.White,3);Text(d,"Waarde "+source,"{value}"+(i<2?"°":"%"),x+7,251,72,35,21,Color.White,source);Text(d,"Label "+source,new[]{"CPU","GPU","RAM"}[i],x,321,86,22,11,Muted);i++;}});
        Add(21,"Tactical FPS",TemplateCategory.Gaming,"Metalen horlogekast, originele tactische illustratie en rode gepantserde informatiepanelen.",d=>{
            var rim=Ring(d,"Lichtsegmenten",WidgetKind.MemoryLoad,21,21,370,Color.Red,3,1,-90,360);rim.Segments=16;rim.Accent=Color.OrangeRed;rim.Motion=WidgetMotion.Breathe;
            Text(d,"QPRINTS merk","QPRINTS",125,34,162,23,16,Color.White).Italic=true;
            Text(d,"Tactische titel","TACTICAL",89,61,234,40,29,Color.White).FontFamily="Bahnschrift";
            Text(d,"Subtitel","SYSTEM / OPERATIONS",111,99,190,17,10,Color.LightGray);
            Panel(d,80,129,252,72,Color.Red,true,96);
            var time=Text(d,"Digitale klok","HH:mm",88,129,236,54,48,Color.White,WidgetKind.Time);time.Italic=true;time.FontFamily="Bahnschrift";
            Text(d,"Datum","ddd dd MMM yyyy",95,179,222,20,12,Color.Silver,WidgetKind.Date);
            Panel(d,51,212,146,65,Color.Red,true,96);Panel(d,215,212,146,65,Color.Red,true,96);
            Sensor(d,WidgetKind.CpuTemperature,"CPU",72,215,106,Color.White,24);Sensor(d,WidgetKind.GpuTemperature,"GPU",234,215,106,Color.White,24);
            Bar(d,"CPU-belasting",WidgetKind.CpuLoad,70,264,105,Color.Red);Bar(d,"GPU-belasting",WidgetKind.GpuLoad,235,264,105,Color.Red);
            Panel(d,66,290,131,62,Color.Red,true,96);Panel(d,215,290,131,62,Color.Red,true,96);
            Text(d,"FPS label","FPS",85,294,93,17,10,Color.Silver);var fps=Text(d,"FPS","{value}",83,308,97,38,34,Color.White,WidgetKind.Fps);fps.Italic=true;
            Text(d,"RAM label","RAM / USAGE",231,294,101,17,9,Color.Silver);Text(d,"RAM","{value} %",229,312,105,30,24,Color.White,WidgetKind.MemoryLoad);Bar(d,"RAM-belasting",WidgetKind.MemoryLoad,232,343,97,Color.Red);
            d.Widgets.Add(new(){Name="CPU-grafiek",Kind=WidgetKind.HistoryGraph,BarSource=WidgetKind.CpuLoad,X=133,Y=357,Width=146,Height=28,Foreground=Color.Red,Fill=Color.FromArgb(180,5,6,8),Opacity=80});
            foreach(var w in d.Widgets.Where(w=>w.Kind is WidgetKind.CpuTemperature or WidgetKind.GpuTemperature))w.Italic=true;
        },"tactical",PageAnimation.Smoke);
        return result;
    }
}

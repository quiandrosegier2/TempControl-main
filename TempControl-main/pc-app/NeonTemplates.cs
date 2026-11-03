using System.Drawing;

// Numbering follows the user's reference poster. The original catalog is untouched.
static class NeonTemplates
{
    static readonly Lazy<IReadOnlyList<DesignTemplate>> catalog=new(()=>Build());
    public static IReadOnlyList<DesignTemplate> All=>catalog.Value;
    public static IReadOnlyList<DesignTemplate> Batch(int batch)=>batch is >=1 and <=5?Build((batch-1)*10+1,batch*10):throw new ArgumentOutOfRangeException(nameof(batch));
    static readonly Color Cyan=Color.FromArgb(65,221,255),Pink=Color.FromArgb(255,60,175),Purple=Color.FromArgb(173,95,255),Orange=Color.FromArgb(255,143,39),Green=Color.FromArgb(81,255,144);
    static ScreenWidget Text(ScreenDesign d,string name,string format,int x,int y,int width,int height,float size,Color color,WidgetKind kind=WidgetKind.Text)
    {
        var w=new ScreenWidget{Name=name,Kind=kind,Format=format,X=x,Y=y,Width=width,Height=height,FontSize=size,Foreground=color,TemperatureColors=false,FontFamily="Bahnschrift"};d.Widgets.Add(w);return w;
    }
    static void Clock(ScreenDesign d,int y=53,Color? color=null,float size=58)=>Text(d,"Klok · bewerkbaar","HH:mm",52,y,308,75,size,color??Color.White,WidgetKind.Time);
    static void Fps(ScreenDesign d,int x=106,int y=302,int width=200,float size=34,Color? color=null)=>Text(d,"FPS · live","{value} FPS",x,y,width,48,size,color??Color.White,WidgetKind.Fps);
    static void Sensor(ScreenDesign d,WidgetKind source,string label,int x,int y,int width,Color color,int size=27)
    {
        Text(d,label+" label",label,x,y,width,20,12,Color.Silver);
        var w=Text(d,label+" · live","{value}"+(source is WidgetKind.CpuTemperature or WidgetKind.GpuTemperature?"°C":"%"),x,y+18,width,39,size,color,source);
        if(source is WidgetKind.CpuTemperature or WidgetKind.GpuTemperature)w.ColorRules=[new(){Minimum=-100,Color=color},new(){Minimum=70,Color=Orange},new(){Minimum=85,Color=Color.OrangeRed}];
    }
    static void Pair(ScreenDesign d,int y=300,Color? cpu=null,Color? gpu=null){if(y>=328){y=Math.Min(y,330);Sensor(d,WidgetKind.CpuTemperature,"CPU",84,y,116,cpu??Cyan,24);Sensor(d,WidgetKind.GpuTemperature,"GPU",212,y,116,gpu??Pink,24);}else{Sensor(d,WidgetKind.CpuTemperature,"CPU",67,y,132,cpu??Cyan);Sensor(d,WidgetKind.GpuTemperature,"GPU",213,y,132,gpu??Pink);}}
    static void Trio(ScreenDesign d,int y=296,Color? color=null)
    {
        Sensor(d,WidgetKind.CpuTemperature,"CPU",58,y,98,color??Cyan,21);Sensor(d,WidgetKind.GpuTemperature,"GPU",157,y,98,color??Cyan,21);Sensor(d,WidgetKind.MemoryLoad,"RAM",256,y,98,color??Cyan,21);
    }
    static void Panel(ScreenDesign d,int x,int y,int width,int height,Color accent,int opacity=88)
        =>d.Widgets.Add(new(){Name="Informatiepaneel",Kind=WidgetKind.Panel,X=x,Y=y,Width=width,Height=height,Foreground=accent,Fill=Color.FromArgb(9,12,22),Opacity=opacity,StrokeWidth=1,TemperatureColors=false});
    static ScreenWidget Gauge(ScreenDesign d,string name,WidgetKind source,int x,int y,int size,Color color,int stroke=6,int maximum=100,float start=-90,float sweep=360)
    {var w=new ScreenWidget{Name=name,Kind=WidgetKind.CircularGauge,BarSource=source,X=x,Y=y,Width=size,Height=size,StrokeWidth=stroke,Foreground=color,Fill=Color.FromArgb(26,31,42),Maximum=maximum,StartAngle=start,SweepAngle=sweep,TemperatureColors=false};d.Widgets.Add(w);return w;}
    static void Bar(ScreenDesign d,string name,WidgetKind source,int x,int y,int width,Color color)=>d.Widgets.Add(new(){Name=name,Kind=WidgetKind.ProgressBar,BarSource=source,X=x,Y=y,Width=width,Height=5,Foreground=color,Fill=Color.FromArgb(30,36,46),TemperatureColors=false});
    static void Hearts(ScreenDesign d,int y=333){Text(d,"Pixelharten","♥ ♥ ♥",96,y,220,38,26,Color.FromArgb(255,69,104)).FontFamily="Segoe UI Symbol";}
    static void Character(ScreenDesign d,Color? clock=null){Clock(d,277,clock,53);Fps(d,y:340,size:25);}
    static IReadOnlyList<DesignTemplate> Build(int first=1,int last=50)
    {
        var list=new List<DesignTemplate>();
        void Add(int n,string name,TemplateCategory category,string? asset,Action<ScreenDesign> compose,string description,PageAnimation animation=PageAnimation.None)
        {
            if(n<first||n>last)return;
            var d=new ScreenDesign{Name=name,BackgroundArgb=Color.Black.ToArgb(),Darken=0,Animation=animation,AnimationSpeed=.6f,BackgroundPng=NeonArtwork.Create(asset??"black")};compose(d);
            string id="515052494e545335300000000000"+n.ToString("D4");
            list.Add(new(id,new(){TemplateId=id,Name=name,Author="QPRINTS",Category=category,Description=$"{n:00} / NEON 50 · "+description,MinimumAppVersion="2.3.0",Animated=DesignRenderer.IsAnimated(d),RequiredAssets=["embedded:background"],CreatedUtc=new(2026,11,3,0,0,0,DateTimeKind.Utc)},d));
        }
        // Batch 1 — gaming & performance.
        Add(1,"Tactical Ops",TemplateCategory.Gaming,"tactical",d=>{
            var rim=Gauge(d,"Rode lichtsegmenten",WidgetKind.CpuLoad,12,12,388,Color.Red,4,100,-100,310);rim.Motion=WidgetMotion.Breathe;
            Clock(d,224,size:53);Panel(d,58,302,140,62,Color.DarkRed);Panel(d,214,302,140,62,Color.DarkRed);Pair(d,304,Color.White,Color.White);Fps(d,y:359,size:22);
        },"Originele gepantserde operator, rook en rode verlichting; klok, CPU/GPU en FPS.",PageAnimation.Smoke);
        Add(2,"Neon Racer",TemplateCategory.Gaming,"neon-racer",d=>{Clock(d,53,size:52);Panel(d,99,292,214,51,Purple,75);Fps(d,y:293,size:36);Pair(d,345,Cyan,Pink);},"Neon sportwagen, nat wegdek en een grote live FPS-teller.",PageAnimation.CityLights);
        Add(3,"Cyberpunk City",TemplateCategory.Cyberpunk,"cyberpunk-city",d=>{Clock(d);Panel(d,64,291,284,67,Pink,78);Sensor(d,WidgetKind.CpuTemperature,"CPU",72,295,128,Cyan);Sensor(d,WidgetKind.MemoryLoad,"RAM",212,295,128,Pink);Bar(d,"RAM belasting",WidgetKind.MemoryLoad,230,353,92,Pink);},"Paarse neonstad met klok, CPU en RAM.",PageAnimation.CityLights);
        Add(4,"Minimal Pro",TemplateCategory.Minimal,null,d=>{Gauge(d,"CPU boog",WidgetKind.CpuLoad,21,21,370,Cyan,9,100,115,125);Gauge(d,"GPU boog",WidgetKind.GpuLoad,21,21,370,Pink,9,100,-85,125);Clock(d,100,size:56);Panel(d,79,214,119,79,Cyan,70);Panel(d,214,214,119,79,Pink,70);Sensor(d,WidgetKind.CpuTemperature,"CPU",83,226,112,Cyan,24);Sensor(d,WidgetKind.GpuTemperature,"GPU",217,226,112,Pink,24);},"Zuiver zwart, heldere klok en twee onafhankelijke belastingbogen.");
        Add(5,"RGB Pulse",TemplateCategory.Animated,null,d=>{var ring=Gauge(d,"Volledige regenboog",WidgetKind.MemoryLoad,18,18,376,Cyan,15,1);ring.Rainbow=true;ring.Motion=WidgetMotion.Rotate;ring.MotionSpeed=.8f;Clock(d,96,size:55);Fps(d,y:177,size:28);Pair(d,259,Cyan,Green);},"Volledige geanimeerde RGB-kleurring met klok, FPS en temperaturen.");
        Add(6,"Frost Glass",TemplateCategory.Nature,"frost-glass",d=>{Clock(d,51,size:55);Panel(d,54,287,304,74,Cyan,68);Trio(d,294,Color.LightCyan);},"Besneeuwde bergtoppen met een koelblauw glazen informatiepaneel.");
        Add(7,"Space Explorer",TemplateCategory.SciFi,"space-explorer",d=>{Clock(d);Panel(d,63,291,286,71,Cyan,75);Pair(d,297,Cyan,Color.White);Fps(d,y:359,size:21);},"Astronaut, planeten en een rustige sterrenhemel.",PageAnimation.Stars);
        Add(8,"Retro Pixel",TemplateCategory.Retro,"retro-pixel",d=>{Clock(d,53,size:50);Text(d,"FPS","FPS {value}",54,280,180,38,25,Color.Plum,WidgetKind.Fps);Text(d,"Sierharten","♥ ♥",236,280,125,35,18,Color.Pink);Hearts(d,330);foreach(var w in d.Widgets)w.FontFamily="Consolas";},"Originele pixelzon, bergen, spelharten en live FPS.",PageAnimation.PixelStars);
        Add(9,"Anime Style",TemplateCategory.Nature,"anime-style",d=>{Clock(d,259,Color.White,56);Pair(d,330,Color.White,Color.White);},"Origineel avonturierssilhouet tegen een oranje zon; geen bestaand personage.");
        Add(10,"System Rings",TemplateCategory.Hardware,null,d=>{Gauge(d,"CPU ring",WidgetKind.CpuLoad,22,22,368,Cyan,9,100,-90,310);Gauge(d,"GPU ring",WidgetKind.GpuLoad,43,43,326,Purple,9,100,-90,310);Gauge(d,"RAM ring",WidgetKind.MemoryLoad,64,64,284,Pink,9,100,-90,310);Text(d,"Centrale FPS","{value}",108,141,196,82,76,Color.White,WidgetKind.Fps);Text(d,"FPS label","FPS",154,220,104,27,17,Cyan);Text(d,"CPU","CPU  {value}°C",106,276,200,29,20,Cyan,WidgetKind.CpuTemperature);Text(d,"GPU","GPU  {value}°C",106,310,200,29,20,Pink,WidgetKind.GpuTemperature);},"Concentrische live CPU/GPU/RAM-ringen met FPS-kern.");
        // Batch 2 — hardware, nature & science fiction.
        Add(11,"Matrix Code",TemplateCategory.Animated,null,d=>{Panel(d,66,143,280,91,Green,78);Clock(d,149,Green,56);Pair(d,306,Green,Green);foreach(var w in d.Widgets)w.FontFamily="Consolas";},"Groene digitale regen met terminalklok en temperaturen.",PageAnimation.DigitalRain);
        Add(12,"Aurora",TemplateCategory.Nature,"aurora",d=>{Clock(d);Text(d,"Buitentemperatuur","{value}°C",218,235,142,40,29,Color.Plum,WidgetKind.WeatherTemperature);Pair(d,303,Color.Aquamarine,Color.Aquamarine);},"Noorderlicht boven een dennenbos; weer verschijnt alleen met een echte provider.",PageAnimation.Aurora);
        Add(13,"Weather + System",TemplateCategory.Nature,"weather",d=>{Clock(d);d.Widgets.Add(new(){Name="Actueel weerpictogram",Kind=WidgetKind.WeatherIcon,X=84,Y=153,Width=100,Height=91,Foreground=Color.LightSkyBlue,Fill=Color.FromArgb(9,32,67)});Text(d,"Buitentemperatuur","{value}°C",183,166,153,70,47,Color.White,WidgetKind.WeatherTemperature);Panel(d,54,281,304,75,Cyan,75);Trio(d,291);},"Blauwe hemel en actuele weerdata; geen verzonnen temperatuur of weersymbool.");
        Add(14,"Car Dashboard",TemplateCategory.Gaming,"metal",d=>{var gauge=Gauge(d,"FPS toerenteller",WidgetKind.Fps,25,25,362,Color.OrangeRed,9,300,140,260);gauge.Segments=54;gauge.Needle=true;gauge.Accent=Color.Red;Text(d,"Schaallabel","PERFORMANCE",106,97,200,26,16,Color.Silver);Text(d,"FPS waarde","{value}",94,148,224,93,82,Color.White,WidgetKind.Fps);Text(d,"Eenheid","FPS",153,239,106,26,18,Color.White);Pair(d,299,Color.White,Color.White);},"Rode toerenteller met vloeiende live FPS-naald en CPU/GPU.");
        Add(15,"Hardware Pro",TemplateCategory.Hardware,"hardware-pro",d=>{Panel(d,207,83,151,248,Cyan,92);int y=95;foreach(var item in new[]{(WidgetKind.CpuTemperature,"CPU",Cyan),(WidgetKind.GpuTemperature,"GPU",Purple),(WidgetKind.MemoryLoad,"RAM",Pink)}){Text(d,item.Item2,item.Item2+"  {value}"+(item.Item1==WidgetKind.MemoryLoad?"%":"°C"),214,y,137,37,22,item.Item3,item.Item1);y+=53;}Text(d,"FPS","FPS  {value}",214,258,137,38,24,Color.White,WidgetKind.Fps);Bar(d,"CPU belasting",WidgetKind.CpuLoad,221,306,122,Cyan);Text(d,"CPU klok","{value} MHz",88,346,236,27,18,Cyan,WidgetKind.CpuClock);},"RGB pc-hardware met compact technisch telemetriepaneel.");
        Add(16,"Game HUD",TemplateCategory.SciFi,"hud",d=>{var a=Gauge(d,"Scannende buitenboog",WidgetKind.CpuLoad,20,20,372,Cyan,3,1,-90,150);a.Motion=WidgetMotion.Rotate;Gauge(d,"FPS kernmeter",WidgetKind.Fps,87,87,238,Cyan,7,300,-140,280);Text(d,"FPS waarde","{value}",111,147,190,85,73,Color.White,WidgetKind.Fps);Text(d,"FPS label","FPS / LINK",120,227,172,25,16,Cyan);Panel(d,42,277,148,66,Cyan);Panel(d,222,277,148,66,Cyan);Pair(d,282,Cyan,Cyan);Bar(d,"CPU belasting",WidgetKind.CpuLoad,71,351,125,Cyan);Bar(d,"GPU belasting",WidgetKind.GpuLoad,216,351,125,Cyan);},"Cyaan richt- en energieringen met onafhankelijke live sensorkaarten.");
        Add(17,"Lava Monster",TemplateCategory.Gaming,"lava-monster",d=>{var glow=Gauge(d,"Lavagloed",WidgetKind.CpuLoad,17,17,378,Color.OrangeRed,3,1);glow.Motion=WidgetMotion.Breathe;Clock(d,273,Color.White,51);Pair(d,333,Color.White,Color.White);},"Origineel obsidiaanmonster met zacht pulserende lavagloed.");
        Add(18,"Ocean Depths",TemplateCategory.Nature,"ocean-depths",d=>{Clock(d);Pair(d,325,Cyan,Cyan);},"Lichtgevende kwal in een diepblauwe oceaan.",PageAnimation.Bubbles);
        Add(19,"Forest Nature",TemplateCategory.Nature,"forest-nature",d=>{Clock(d,285,Color.White,53);Text(d,"CPU","CPU {value}°C",88,349,236,27,18,Color.LightGreen,WidgetKind.CpuTemperature);},"Hertensilhouet in een zonovergoten bos; rustige minimale informatie.");
        Add(20,"Zen Minimal",TemplateCategory.Minimal,"zen-minimal",d=>{Clock(d,63,size:51);Pair(d,328,Purple,Purple);var halo=Gauge(d,"Lotus ademhaling",WidgetKind.CpuLoad,37,37,338,Purple,1,1);halo.Motion=WidgetMotion.Breathe;halo.Opacity=35;},"Paarse lotus met subtiele ademende gloed en kleine sensoren.");
        // Batch 3 — original characters and playful designs.
        Add(21,"Skull Fury",TemplateCategory.Gaming,"skull-fury",d=>Character(d),"Originele neon schedel met gamingheadset, klok en FPS.");
        Add(22,"Robot Buddy",TemplateCategory.SciFi,"robot-buddy",d=>{Character(d,Cyan);Text(d,"CPU","CPU {value}°C",91,377,230,22,14,Cyan,WidgetKind.CpuTemperature);},"Vriendelijke eigen robot met blauwe ogen, klok en live systeemdata.");
        Add(23,"Cat Gamer",TemplateCategory.Gaming,"cat-gamer",d=>Character(d),"Grappige kat met zonnebril en RGB-hoofdtelefoon.");
        Add(24,"Panda Chill",TemplateCategory.Nature,"panda-chill",d=>Character(d,Color.FromArgb(225,255,106)),"Relaxte panda met paarse zonnebril en warm gouden licht.");
        Add(25,"Doge Vibes",TemplateCategory.Retro,"doge-vibes",d=>Character(d),"Originele Shiba Inu met pixelzonnebril en oranje gloed.");
        Add(26,"Angry CPU",TemplateCategory.Hardware,"angry-cpu",d=>{d.Widgets.Add(new(){Name="Temperatuurgezicht",Kind=WidgetKind.ThermalFace,BarSource=WidgetKind.CpuTemperature,X=146,Y=125,Width=120,Height=96,Foreground=Color.Black,Fill=Color.FromArgb(255,157,23),Accent=Color.Red,TemperatureColors=false});Pair(d,308,Color.White,Color.White);Bar(d,"CPU belasting",WidgetKind.CpuLoad,104,376,204,Orange);},"Het gezicht reageert op de echte CPU-temperatuur: koel, warm, heet of onbekend.",PageAnimation.Embers);
        Add(27,"Coffee Mode",TemplateCategory.Productivity,"coffee-mode",d=>{Clock(d,280,size:54);Text(d,"CPU belasting","CPU  {value}%",91,350,230,34,24,Orange,WidgetKind.CpuLoad);},"Koffiekarakter met bewegende stoom en live CPU-belasting.",PageAnimation.Steam);
        Add(28,"Pizza Power",TemplateCategory.Gaming,"pizza-power",d=>{Fps(d,y:281,size:37);Pair(d,337,Orange,Color.White);},"Originele pizzapunt met zonnebril, FPS en temperaturen.",PageAnimation.Embers);
        Add(29,"Sleep Mode",TemplateCategory.SciFi,"sleep-mode",d=>{Clock(d,286,size:52);Text(d,"Slaapsymbool","z Z z",237,91,108,43,25,Color.LightSteelBlue);Text(d,"Status","EVEN OPLADEN",90,350,232,25,14,Color.LightSteelBlue);},"Slapende astronaut; optioneel via bestaande inactiviteitsregels te kiezen.",PageAnimation.Stars);
        Add(30,"Toilet Break",TemplateCategory.Productivity,"toilet-break",d=>{Text(d,"Status","TOILET BREAK",64,283,284,46,33,Color.White);Clock(d,334,size:31);},"Humoristisch toiletkarakter; wijzigt de inactiviteitsregels niet automatisch.");
        // Batch 4 — premium futuristic compositions.
        Add(31,"Rocket Launch",TemplateCategory.SciFi,"rocket-launch",d=>{Fps(d,y:276,size:43);Pair(d,335,Color.White,Color.White);},"Originele raket met bewegende uitlaatvonken.",PageAnimation.Embers);
        Add(32,"Moon Base",TemplateCategory.SciFi,"moon-base",d=>{Clock(d);Pair(d,329,Cyan,Cyan);},"Verlichte maanbasis met planeten en een sterrenhemel.",PageAnimation.Stars);
        Add(33,"Mecha Core",TemplateCategory.Gaming,"mecha-core",d=>{Fps(d,y:281,size:39);Pair(d,335,Color.White,Color.White);},"Originele blauwmetalen mecha met rode ogen.");
        Add(34,"Hologram AI",TemplateCategory.SciFi,"hologram-ai",d=>{Clock(d,278,Cyan,52);Pair(d,338,Cyan,Cyan);},"Origineel holografisch AI-portret met bewegende scanlijnen.",PageAnimation.Scanlines);
        Add(35,"Hacker Terminal",TemplateCategory.Cyberpunk,"hacker-terminal",d=>{Clock(d,280,Green,51);Pair(d,341,Green,Green);foreach(var w in d.Widgets)w.FontFamily="Consolas";},"Anonieme fictieve figuur met groene terminalglitch.",PageAnimation.Scanlines);
        Add(36,"Vintage Nixie",TemplateCategory.Retro,"nixie",d=>{for(int i=0;i<4;i++){var digit=Text(d,"Nixiecijfer "+(i+1),"HHmm",72+i*69,143,62,90,67,Orange,WidgetKind.Time);digit.FontFamily="Consolas";digit.ClockCharacter=i;}Pair(d,304,Color.Bisque,Color.Bisque);},"Vier afzonderlijk bewerkbare live klokcijfers in warme glazen Nixiebuizen.");
        Add(37,"Steampunk",TemplateCategory.Retro,"steampunk",d=>{Panel(d,76,235,260,71,Color.Goldenrod,86);Clock(d,231,Color.Bisque,53);Pair(d,328,Color.Bisque,Color.Bisque);},"Messing tandwielen en warm mechanisch uurwerk.");
        Add(38,"Neon Skull",TemplateCategory.Gaming,"neon-skull",d=>{Clock(d,278,size:53);Pair(d,338,Cyan,Purple);var glow=Gauge(d,"Neon rand",WidgetKind.CpuLoad,15,15,382,Purple,2,1);glow.Motion=WidgetMotion.Breathe;},"Blauw-paarse neon schedel met pulserende rand.");
        Add(39,"Liquid RGB",TemplateCategory.Animated,"liquid-rgb",d=>{Panel(d,73,143,266,78,Purple,85);Clock(d,145,size:54);Pair(d,320,Cyan,Pink);},"Kleurrijke vloeibare vormen met zachte bewegende lichtgolven.",PageAnimation.Waves);
        Add(40,"Geometry",TemplateCategory.Minimal,"geometry",d=>{Clock(d,268,size:53);Pair(d,337,Cyan,Purple);var arc=Gauge(d,"Geometriescan",WidgetKind.CpuLoad,25,25,362,Purple,2,1,-90,40);arc.Motion=WidgetMotion.Rotate;},"Neon driehoeken en een subtiel roterende scanboog.");
        // Batch 5 — colorful scenes and authentic supplied branding.
        Add(41,"Minecraft Vibes",TemplateCategory.Retro,"minecraft-vibes",d=>{Clock(d,54,size:49);Hearts(d,300);Pair(d,344,Color.White,Color.White);},"Origineel voxel-landschap; geen officiële textures, personages of logo's.");
        Add(42,"Survival Camp",TemplateCategory.Nature,"survival-camp",d=>{Clock(d,53,size:50);Text(d,"Buitentemperatuur","{value}°C",184,131,160,43,28,Color.LightSkyBlue,WidgetKind.WeatherTemperature);Text(d,"CPU","CPU {value}°C",85,351,242,29,20,Orange,WidgetKind.CpuTemperature);},"Knusse tent en bewegende kampvuurvonken; weer blijft configureerbaar.",PageAnimation.Embers);
        Add(43,"Racing HUD",TemplateCategory.Gaming,"racing",d=>{Panel(d,62,121,288,119,Color.Red,94);Text(d,"FPS","{value}",91,126,230,83,76,Color.White,WidgetKind.Fps).Italic=true;Text(d,"FPS label","FPS",149,207,114,29,19,Color.White);Pair(d,303,Color.White,Color.White);Bar(d,"GPU belasting",WidgetKind.GpuLoad,100,371,212,Color.Red);},"Zwart-rode racecockpit met dambordvlag en grote FPS-teller.");
        Add(44,"Winter Snow",TemplateCategory.Nature,"winter-snow",d=>{Clock(d);Pair(d,333,Color.LightCyan,Color.LightCyan);},"Warme blokhut in een blauwe winternacht met vallende sneeuw.",PageAnimation.Snow);
        Add(45,"Summer Beach",TemplateCategory.Nature,"summer-beach",d=>{Clock(d,278,size:53);Text(d,"FPS","FPS  {value}",100,348,212,31,23,Color.Plum,WidgetKind.Fps);},"Tropisch strand met palmbomen en een zachte waterbeweging.",PageAnimation.Waves);
        Add(46,"Galaxy Core",TemplateCategory.SciFi,"galaxy-core",d=>{Clock(d,278,size:53);Pair(d,338,Color.White,Color.White);},"Lichtgevende spiraalnevel met bewegende kosmische deeltjes.",PageAnimation.Stars);
        Add(47,"Energy Core",TemplateCategory.SciFi,"reactor",d=>{var outer=Gauge(d,"Reactorrotatie",WidgetKind.CpuLoad,27,27,358,Cyan,4,1,-90,285);outer.Motion=WidgetMotion.Rotate;var core=Gauge(d,"Energiemeter",WidgetKind.Fps,67,67,278,Cyan,11,300,-90,360);core.Segments=24;Text(d,"CPU","CPU {value}°C",93,78,226,33,23,Cyan,WidgetKind.CpuTemperature);Text(d,"FPS","{value}",112,153,188,87,76,Color.White,WidgetKind.Fps);Text(d,"FPS label","FPS",154,236,104,26,18,Cyan);Text(d,"GPU","GPU {value}°C",91,304,230,34,23,Cyan,WidgetKind.GpuTemperature);},"Blauwe reactor met gesegmenteerde FPS-energie en roterende buitenring.");
        Add(48,"Circuit Neon",TemplateCategory.Hardware,"circuit",d=>{Pair(d,326,Pink,Purple);var pulse=Gauge(d,"Elektronische puls",WidgetKind.CpuLoad,39,39,334,Pink,2,1,-60,80);pulse.Motion=WidgetMotion.Rotate;},"Lichtgevende chip met het originele Q-merk uit de aangeleverde poster.");
        Add(49,"Abstract Waves",TemplateCategory.Animated,"abstract-waves",d=>{Clock(d,53,size:53);Pair(d,305,Cyan,Purple);Fps(d,y:363,size:19);},"Blauw-paarse lichtlinten met zachte golfanimatie.",PageAnimation.Waves);
        Add(50,"QPRINTS Signature",TemplateCategory.Minimal,"signature",d=>{Pair(d,288,Color.White,Color.White);Fps(d,y:353,size:26);},"Het oorspronkelijke QPRINTS-logo uit jouw aangeleverde poster, met losse live widgets.");
        return list;
    }
}

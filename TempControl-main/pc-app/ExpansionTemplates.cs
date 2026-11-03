using System.Drawing;

static class ExpansionTemplates
{
    static readonly Lazy<IReadOnlyList<DesignTemplate>> catalog=new(()=>Build());
    public static IReadOnlyList<DesignTemplate> All=>catalog.Value;
    public static IReadOnlyList<DesignTemplate> Batch(int batch)=>batch is >=1 and <=5?Build(51+(batch-1)*10,50+batch*10):throw new ArgumentOutOfRangeException(nameof(batch));
    static readonly Color Cyan=Color.FromArgb(69,221,255),Pink=Color.FromArgb(255,100,181),Purple=Color.FromArgb(175,120,255),Orange=Color.FromArgb(255,167,73),Green=Color.FromArgb(130,242,170);
    static ScreenWidget Text(ScreenDesign d,string name,WidgetKind kind,string format,int x,int y,int width,int height,float size,Color color)
    {var w=new ScreenWidget{Name=name,Kind=kind,Format=format,X=x,Y=y,Width=width,Height=height,FontSize=size,Foreground=color,TemperatureColors=false,FontFamily="Bahnschrift"};d.Widgets.Add(w);return w;}
    static void Panel(ScreenDesign d,int x,int y,int width,int height,Color color,int opacity=78)=>d.Widgets.Add(new(){Name="Glazen informatiepaneel",Kind=WidgetKind.Panel,X=x,Y=y,Width=width,Height=height,Foreground=color,Fill=Color.FromArgb(5,9,19),StrokeWidth=1,Opacity=opacity,TemperatureColors=false});
    static void Clock(ScreenDesign d,int y,Color color,int size=51)
    {Text(d,"Digitale klok",WidgetKind.Time,"HH:mm",54,y,304,65,size,color);Text(d,"Datum",WidgetKind.Date,"ddd dd MMM yyyy",84,y+63,244,19,11,Color.Silver);}
    static void Telemetry(ScreenDesign d,int y,Color cpu,Color gpu)
    {
        foreach(var pair in new[]{(WidgetKind.CpuTemperature,"CPU",86,cpu),(WidgetKind.GpuTemperature,"GPU",212,gpu)})
        {
            Text(d,pair.Item2+" label",WidgetKind.Text,pair.Item2,pair.Item3,y,114,16,10,Color.Silver);
            var value=Text(d,pair.Item2+" temperatuur",pair.Item1,"{value}°C",pair.Item3,y+14,114,30,23,pair.Item4);
            value.ColorRules=[new(){Minimum=-100,Color=pair.Item4},new(){Minimum=75,Color=Orange},new(){Minimum=88,Color=Color.OrangeRed}];
        }
        d.Widgets.Add(new(){Name="CPU-belasting",Kind=WidgetKind.ProgressBar,BarSource=WidgetKind.CpuLoad,X=104,Y=y+45,Width=92,Height=4,Foreground=cpu,Fill=Color.FromArgb(35,43,53),TemperatureColors=false});
        d.Widgets.Add(new(){Name="GPU-belasting",Kind=WidgetKind.ProgressBar,BarSource=WidgetKind.GpuLoad,X=216,Y=y+45,Width=92,Height=4,Foreground=gpu,Fill=Color.FromArgb(35,43,53),TemperatureColors=false});
        Text(d,"FPS",WidgetKind.Fps,"FPS {value}",112,y+51,91,21,12,Color.White);
        Text(d,"RAM",WidgetKind.MemoryLoad,"RAM {value}%",209,y+51,91,21,12,gpu);
    }
    static void Accent(ScreenDesign d,Color color,bool pulse=false)
    {d.Widgets.Add(new(){Name="Bewerkbare accentboog",Kind=WidgetKind.CircularGauge,BarSource=WidgetKind.GpuLoad,X=13,Y=13,Width=386,Height=386,Maximum=100,StartAngle=-110,SweepAngle=135,StrokeWidth=2,Foreground=color,Fill=Color.FromArgb(35,43,53),Motion=pulse?WidgetMotion.Breathe:WidgetMotion.None,TemperatureColors=false,Opacity=65,DecorativeRing=true});}
    static void Feature(ScreenDesign d,WidgetKind kind,int y,int size,Color color)
    {var w=d.Widgets.Single(w=>w.Kind==kind);w.X=106;w.Y=y;w.Width=200;w.Height=38;w.FontSize=size;w.Foreground=color;var companion=d.Widgets.Single(w=>w.Kind==(kind==WidgetKind.Fps?WidgetKind.MemoryLoad:WidgetKind.Fps));companion.X=156;companion.Width=100;}
    static IReadOnlyList<DesignTemplate> Build(int first=51,int last=100)
    {
        var result=new List<DesignTemplate>();
        void Add(int number,string name,TemplateCategory category,string scene,Action<ScreenDesign> compose,string description,PageAnimation animation=PageAnimation.None)
        {
            if(number<first||number>last)return;
            var design=new ScreenDesign{Name=name,BackgroundArgb=Color.Black.ToArgb(),BackgroundPng=ExpansionArtwork.Create(scene),Darken=0,Animation=animation,AnimationSpeed=.55f};compose(design);
            string id="515052494e545331303000000000"+number.ToString("D4");
            result.Add(new(id,new(){TemplateId=id,Name=name,Category=category,Author="QPRINTS",Description=$"{number:00} / EXPANSION · "+description,MinimumAppVersion="2.4.0",Animated=DesignRenderer.IsAnimated(design),RequiredAssets=["embedded:background"],CreatedUtc=new(2026,11,3,0,0,0,DateTimeKind.Utc)},design));
        }
        // Batch 1 is implemented and validated before adding further batches.
        Add(51,"Dragon Flame",TemplateCategory.Fantasy,"dragon-flame",d=>{Clock(d,244,Color.White);Telemetry(d,325,Orange,Color.PeachPuff);Accent(d,Orange,true);},"Originele zwarte draak, vulkanische gloed en bewegende vuurvonken.",PageAnimation.Embers);
        Add(52,"Samurai Spirit",TemplateCategory.Japanese,"samurai-spirit",d=>{Clock(d,44,Color.White);Panel(d,79,288,254,61,Color.DarkRed);Telemetry(d,296,Color.White,Color.Salmon);Accent(d,Color.Red);},"Anonieme samoerai, rode maan en traditionele daken.");
        Add(53,"Kawaii Cat",TemplateCategory.Kawaii,"kawaii-cat",d=>{Clock(d,244,Color.White);Telemetry(d,325,Pink,Color.Plum);Accent(d,Pink,true);},"Originele glimlachende kat met hoofdtelefoon en zwevende hartjes.",PageAnimation.Hearts);
        Add(54,"Cherry Blossom",TemplateCategory.Japanese,"cherry-blossom",d=>{Clock(d,44,Color.White);Panel(d,78,288,256,61,Pink,70);Telemetry(d,296,Color.LightPink,Color.Plum);},"Pagode, spiegelend water en zacht vallende sakurablaadjes.",PageAnimation.Petals);
        Add(55,"Neon Tokyo",TemplateCategory.Cyberpunk,"neon-tokyo",d=>{Text(d,"Tokyo lichtschrift",WidgetKind.Text,"東京",72,138,72,28,19,Cyan).FontFamily="Yu Gothic UI";Clock(d,44,Color.White);Panel(d,79,288,254,61,Purple);Telemetry(d,296,Cyan,Pink);Accent(d,Purple,true);},"Neonstraat met blauwe en paarse reflecties; alle meetwaarden zijn losse widgets.",PageAnimation.CityLights);
        Add(56,"Pirate Anime",TemplateCategory.Japanese,"pirate-anime",d=>{var icon=Text(d,"Piratenmotief",WidgetKind.Text,"☠",312,141,38,35,25,Color.LightCyan);icon.FontFamily="Segoe UI Symbol";icon.Opacity=75;Clock(d,244,Color.White);Telemetry(d,325,Cyan,Color.LightSkyBlue);Accent(d,Cyan);},"Originele vrolijke piraat met strohoed en een eigen kostuum, tegen een blauwe oceaan.");
        Add(57,"Monster Capsule",TemplateCategory.SciFi,"monster-capsule",d=>{Panel(d,79,44,254,83,Color.DarkRed,88);Clock(d,44,Color.White);Panel(d,79,288,254,61,Color.IndianRed);Telemetry(d,296,Color.White,Color.Salmon);Accent(d,Color.Red,true);},"Originele bolvormige energiecapsule met asymmetrische panelen; geen bestaand spelvoorwerp.");
        Add(58,"Saiyan Energy",TemplateCategory.Japanese,"saiyan-energy",d=>{Clock(d,244,Color.White);Telemetry(d,325,Color.Gold,Orange);Accent(d,Orange,true);},"Originele animevechter met eigen outfit en een oranje energieaura.",PageAnimation.Embers);
        Add(59,"Ninja Shadow",TemplateCategory.Japanese,"ninja-shadow",d=>{Clock(d,244,Color.White);Telemetry(d,325,Color.Salmon,Color.White);Accent(d,Color.Red,true);},"Originele ninja zonder franchisesymbool, met rode energieaccenten.",PageAnimation.Smoke);
        Add(60,"Forest Spirit",TemplateCategory.Kawaii,"forest-spirit",d=>{Clock(d,44,Color.White);Telemetry(d,296,Color.LightSkyBlue,Color.PaleGoldenrod);Accent(d,Cyan);},"Eigen boswezentje tussen maanlicht en gouden vuurvliegjes.",PageAnimation.Stars);
        // Batch 2 — Japanese, kawaii and atmospheric nature.
        Add(61,"Moon Guardian",TemplateCategory.Japanese,"moon-guardian",d=>{Clock(d,244,Color.White);Telemetry(d,325,Color.Plum,Pink);Accent(d,Purple,true);},"Originele magische silhouetfiguur met paarse maan en zwevende harten.",PageAnimation.Hearts);
        Add(62,"Neon Ronin",TemplateCategory.Japanese,"neon-ronin",d=>{d.Widgets.Add(new(){Name="Katana-gloed",Kind=WidgetKind.NeonLine,X=192,Y=140,Width=187,Height=85,StrokeWidth=3,Foreground=Color.FromArgb(215,125,255),Motion=WidgetMotion.Breathe,MotionSpeed=.8f,TemperatureColors=false});Clock(d,244,Color.White);Telemetry(d,325,Purple,Cyan);Accent(d,Purple,true);},"Eigen cyberpunk samoerai met afzonderlijk bewerkbare, pulserende katana-gloed.",PageAnimation.CityLights);
        Add(63,"Kawaii Bunny",TemplateCategory.Kawaii,"kawaii-bunny",d=>{Clock(d,244,Color.White);Telemetry(d,325,Pink,Color.Plum);Accent(d,Pink,true);},"Wit konijn met hoofdtelefoon en zacht zwevende roze hartjes.",PageAnimation.Hearts);
        Add(64,"Demon Mask",TemplateCategory.Japanese,"demon-mask",d=>{Clock(d,244,Color.White);Telemetry(d,325,Color.Salmon,Color.White);Accent(d,Color.Crimson);},"Origineel wit-rood oni-masker tussen kersenbloesem.",PageAnimation.Petals);
        Add(65,"Ramen Time",TemplateCategory.Japanese,"ramen-time",d=>{Clock(d,44,Color.White);Panel(d,79,288,254,61,Orange);Telemetry(d,296,Orange,Color.Bisque);},"Warme ramenkom in een eigen restaurantillustratie met bewegende stoom.",PageAnimation.Steam);
        Add(66,"Kitsune Fox",TemplateCategory.Fantasy,"kitsune-fox",d=>{Clock(d,244,Color.White);Telemetry(d,325,Purple,Pink);Accent(d,Purple,true);},"Mystiek vossenmasker met paarse vossevlammen.",PageAnimation.Stars);
        Add(67,"Anime Swordsman",TemplateCategory.Japanese,"anime-swordsman",d=>{Clock(d,244,Color.White);Telemetry(d,325,Color.Turquoise,Color.Salmon);Accent(d,Color.Turquoise);},"Originele reizende zwaardvechter met teal geometrie en rode energie.");
        Add(68,"Happy Anime",TemplateCategory.Japanese,"happy-anime",d=>{Panel(d,80,244,252,82,Color.LightPink,75);Clock(d,244,Color.White);Telemetry(d,325,Pink,Color.LightGoldenrodYellow);},"Vrolijk origineel volwassen personage met roze haar en warme kleuren.");
        Add(69,"Neon Panda",TemplateCategory.Kawaii,"neon-panda",d=>{Clock(d,244,Color.White);Telemetry(d,325,Cyan,Purple);Accent(d,Cyan,true);},"Eigen panda met blauwe ogen en paarse holografische lichtjes.",PageAnimation.Stars);
        Add(70,"Koi Pond",TemplateCategory.Nature,"koi-pond",d=>{Panel(d,79,44,254,83,Cyan,72);Clock(d,44,Color.White);Panel(d,79,288,254,61,Cyan,70);Telemetry(d,296,Cyan,Color.LightPink);},"Originele koivijver met lotus, waterlelies en rustige waterrimpels.",PageAnimation.Ripples);
        // Batch 3 — horror, Halloween and dark original science fiction.
        Add(71,"Horror Night",TemplateCategory.Horror,"horror-night",d=>{Clock(d,244,Color.White);Telemetry(d,325,Color.Salmon,Color.LightGray);Accent(d,Color.Crimson,true);},"Anonieme schim, rode maan en bewegende bosmist.",PageAnimation.Smoke);
        Add(72,"Zombie Mode",TemplateCategory.Horror,"zombie-mode",d=>{Clock(d,44,Color.White);Panel(d,79,288,254,61,Green);Telemetry(d,296,Green,Color.LightSkyBlue);},"Niet-grafische kerkhofsilhouetten met blauwe maan en groene mist.",PageAnimation.Smoke);
        Add(73,"Halloween",TemplateCategory.Horror,"halloween",d=>{Clock(d,244,Color.White);Telemetry(d,325,Orange,Color.Bisque);Accent(d,Orange,true);},"Eigen jack-o'-lantern, spookkasteel en bewegende lichtvonken.",PageAnimation.Embers);
        Add(74,"Vampire Night",TemplateCategory.Horror,"vampire-night",d=>{Clock(d,44,Color.White);Panel(d,79,288,254,61,Color.DarkRed);Telemetry(d,296,Color.Salmon,Color.White);Accent(d,Color.Crimson);},"Origineel gotisch kasteel tegen een karmijnrode maan.");
        Add(75,"Ghost Rider",TemplateCategory.Horror,"ghost-rider",d=>{Clock(d,244,Color.White);Telemetry(d,325,Cyan,Color.LightCyan);Accent(d,Cyan,true);},"Originele spectrale schedel met bewegende cyaanvlammen.",PageAnimation.SpectralFire);
        Add(76,"Cyber Skull",TemplateCategory.Horror,"cyber-skull",d=>{Clock(d,244,Color.White);Telemetry(d,325,Purple,Cyan);Accent(d,Purple,true);},"Asymmetrische cybernetische schedel met mechanische details en neonpuls.");
        Add(77,"Biohazard",TemplateCategory.Horror,"biohazard",d=>{Clock(d,44,Green);Panel(d,79,288,254,61,Green);Telemetry(d,296,Green,Color.PaleGreen);Accent(d,Color.LimeGreen,true);},"Groen waarschuwingssymbool op industrieel metaal; alle meetwaarden blijven bewerkbaar.");
        Add(78,"Secret Laboratory",TemplateCategory.SciFi,"secret-laboratory",d=>{Panel(d,79,44,254,83,Color.DarkRed,80);Clock(d,44,Color.White);Panel(d,79,288,254,61,Color.IndianRed);Telemetry(d,296,Color.White,Color.Salmon);},"Fictief laboratorium met een origineel drieledig geometrisch insigne.",PageAnimation.Scanlines);
        Add(79,"Alien Invasion",TemplateCategory.SciFi,"alien-invasion",d=>{Clock(d,244,Color.White);Telemetry(d,325,Cyan,Color.Aquamarine);Accent(d,Cyan,true);},"Originele UFO, cyaan lichtbundel en bewegende zweefdeeltjes.",PageAnimation.Stars);
        Add(80,"Desert Mystery",TemplateCategory.SciFi,"desert-mystery",d=>{Clock(d,44,Color.White);Panel(d,79,288,254,61,Cyan);Telemetry(d,296,Cyan,Color.LightSteelBlue);Accent(d,Cyan);},"Geheime fictieve woestijnbasis onder een diepe blauwe sterrenhemel.");
        // Batch 4 — historical inspiration, fantasy and adventure.
        Add(81,"Stealth Assassin",TemplateCategory.Fantasy,"stealth-assassin",d=>{Clock(d,244,Color.White);Telemetry(d,325,Color.Silver,Cyan);Accent(d,Color.LightSteelBlue);},"Origineel middeleeuws sluipavonturier met eigen kostuum in een donkere stad.",PageAnimation.Smoke);
        Add(82,"Viking Storm",TemplateCategory.Fantasy,"viking-storm",d=>{Clock(d,244,Color.White);Telemetry(d,325,Cyan,Color.LightSteelBlue);Accent(d,Cyan,true);},"Noordse krijger met hoornvrije helm en bewegende stormlichtflitsen.",PageAnimation.Lightning);
        Add(83,"Gladiator",TemplateCategory.Fantasy,"gladiator",d=>{Clock(d,244,Color.White);Telemetry(d,325,Color.Gold,Color.Bisque);Accent(d,Orange);},"Originele Romeinse helm en een Colosseum-achtergrond bij zonsondergang.");
        Add(84,"Pirate Ship",TemplateCategory.Fantasy,"pirate-ship",d=>{Panel(d,79,44,254,83,Cyan,72);Clock(d,44,Color.White);Panel(d,79,288,254,61,Cyan);Telemetry(d,296,Cyan,Color.White);},"Eigen zeilschip met blauw oceaanwater en rustige bewegende rimpels.",PageAnimation.Ripples);
        Add(85,"Wild West",TemplateCategory.Fantasy,"wild-west",d=>{Panel(d,79,44,254,83,Orange,72);Clock(d,44,Color.White);Telemetry(d,296,Color.Bisque,Orange);Accent(d,Orange);},"Cowboy- en paardensilhouet tegen een originele woestijnzon.");
        Add(86,"Knight Armor",TemplateCategory.Fantasy,"knight-armor",d=>{Panel(d,79,244,254,81,Cyan,75);Clock(d,244,Color.White);Telemetry(d,325,Color.White,Color.Gold);Accent(d,Color.Silver);},"Originele zilveren ridderuitrusting met gouden accenten en een donker kasteel.");
        Add(87,"Egyptian Gold",TemplateCategory.Fantasy,"egyptian-gold",d=>{Panel(d,79,244,254,81,Color.Goldenrod,78);Clock(d,244,Color.White);Telemetry(d,325,Color.Gold,Cyan);Accent(d,Color.Gold);},"Eigen goud-blauw faraomasker met piramidevormen op de achtergrond.");
        Add(88,"Ancient Temple",TemplateCategory.Fantasy,"ancient-temple",d=>{Clock(d,44,Color.White);Panel(d,79,288,254,61,Green,72);Telemetry(d,296,Green,Color.PaleGoldenrod);},"Meso-Amerikaans geïnspireerde stenen piramide, jungle en bewegende ochtendmist.",PageAnimation.Smoke);
        Add(89,"Jungle Explorer",TemplateCategory.Nature,"jungle-explorer",d=>{Panel(d,79,44,254,83,Green,70);Clock(d,44,Color.White);Panel(d,79,288,254,61,Green,75);Telemetry(d,296,Green,Cyan);},"Originele avonturier bij een tropische waterval met subtiele waterbeweging.",PageAnimation.Ripples);
        Add(90,"Dino World",TemplateCategory.Fantasy,"dino-world",d=>{Clock(d,244,Color.White);Telemetry(d,325,Color.PaleGoldenrod,Green);Accent(d,Orange);},"Originele realistisch geïllustreerde dinosaurus in een prehistorische jungle.");
        // Batch 5 — nature, space and fully editable abstract light graphics.
        Add(91,"Underwater Reef",TemplateCategory.Nature,"underwater-reef",d=>{Panel(d,79,44,254,83,Cyan,74);Clock(d,44,Color.White);Panel(d,79,288,254,61,Cyan,78);Telemetry(d,296,Cyan,Green);},"Originele zeeschildpad boven een kleurrijk koraalrif met opstijgende bubbels.",PageAnimation.Bubbles);
        Add(92,"Shark Attack",TemplateCategory.Nature,"shark-attack",d=>{Panel(d,79,44,254,83,Cyan,72);Clock(d,44,Color.White);Panel(d,106,248,200,40,Cyan,72);Panel(d,79,288,254,61,Cyan,75);Telemetry(d,296,Cyan,Color.White);Feature(d,WidgetKind.Fps,248,27,Color.White);},"Originele witte haai in diepblauw water, met extra grote FPS-weergave.",PageAnimation.Ripples);
        Add(93,"Frozen Ice",TemplateCategory.Nature,"frozen-ice",d=>{Panel(d,79,44,254,83,Cyan,72);Clock(d,44,Color.White);Panel(d,79,288,254,61,Cyan,72);Telemetry(d,296,Color.LightCyan,Cyan);},"Origineel magisch ijskasteel in een blauwe sneeuwnacht.",PageAnimation.Snow);
        Add(94,"Volcano Core",TemplateCategory.Nature,"volcano-core",d=>{Clock(d,44,Color.White);Panel(d,79,288,254,61,Orange,82);Telemetry(d,296,Orange,Color.Bisque);Accent(d,Color.OrangeRed,true);},"Uitbarstende vulkaan, donkere rook en bewegende lavavonken.",PageAnimation.Embers);
        Add(95,"Desert Mirage",TemplateCategory.Minimal,"desert-mirage",d=>{Panel(d,79,44,254,83,Orange,72);Clock(d,44,Color.White,58);Panel(d,106,248,200,40,Orange,72);Telemetry(d,296,Color.Bisque,Orange);Feature(d,WidgetKind.MemoryLoad,248,23,Color.White);},"Rustige gouden zandduinen met grote klok en nadruk op live RAM-gebruik.");
        Add(96,"Lightning Storm",TemplateCategory.Nature,"lightning-storm",d=>{Panel(d,79,244,254,81,Purple,72);Clock(d,244,Color.White);Telemetry(d,325,Purple,Color.White);Accent(d,Purple,true);},"Paarse bliksem boven donkere bergen met subtiele bewegende flitsen.",PageAnimation.Lightning);
        Add(97,"Earth From Space",TemplateCategory.SciFi,"earth-from-space",d=>{Panel(d,79,44,254,83,Cyan,75);Clock(d,44,Color.White);Telemetry(d,296,Cyan,Color.LightSkyBlue);Accent(d,Cyan);},"Origineel zicht op de aarde met blauwe atmosfeer en rustige orbitale lichtpunten.",PageAnimation.Orbit);
        Add(98,"Black Hole",TemplateCategory.SciFi,"black-hole",d=>{Clock(d,244,Color.White);Telemetry(d,325,Purple,Pink);Accent(d,Purple);d.Widgets[^1].Motion=WidgetMotion.Rotate;},"Kosmisch zwart gat met paarse accretieschijf en een bewegende energieboog.",PageAnimation.Stars);
        Add(99,"Quantum Core",TemplateCategory.SciFi,"quantum-core",d=>{Panel(d,79,44,254,83,Purple,78);Clock(d,44,Color.White);Panel(d,106,258,200,40,Purple,78);Panel(d,79,305,254,55,Purple,82);Telemetry(d,311,Purple,Pink);Feature(d,WidgetKind.Fps,258,26,Color.White);Accent(d,Purple);},"Originele atoomgrafiek met bewegende deeltjes en een grote FPS-waarde.",PageAnimation.Orbit);
        Add(100,"RGB Dream",TemplateCategory.Animated,"rgb-dream",d=>{Clock(d,44,Color.White);d.Widgets.Add(new(){Name="RGB oneindigheid",Kind=WidgetKind.InfinityRibbon,X=45,Y=142,Width=322,Height=126,StrokeWidth=8,Foreground=Cyan,Accent=Orange,Rainbow=true,Motion=WidgetMotion.RgbCycle,MotionSpeed=.8f,TemperatureColors=false});Telemetry(d,296,Cyan,Pink);},"Volledig bewerkbaar oneindigheidslint met vloeiend bewegend RGB-verloop.");
        return result;
    }
}

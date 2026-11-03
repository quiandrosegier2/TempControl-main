using System.ComponentModel;
using System.Text.Json;

enum ProfileTrigger { Foreground, Running, Idle }
enum PageTransition { Cut, Fade, Slide, Zoom }
enum PageAnimation { None, Orbit, Radar, Stars, Pulse, Waves, DigitalRain, Aurora, CityLights, PixelStars, Smoke }
enum TemplateCategory { Gaming, Minimal, Cyberpunk, Productivity, Hardware, Clock, Animated, Retro, Nature, SciFi }
enum AlertCondition { Above, Below, Unavailable, Disconnected }

sealed class ProfileRule
{
    public bool Enabled { get; set; }=true;
    public ProfileTrigger Trigger { get; set; }
    public string Executable { get; set; }="game.exe";
    public string ProfileId { get; set; }="";
    public int IdleSeconds { get; set; }=300;
    public int Priority { get; set; }=10;
}
[TypeConverter(typeof(ExpandableObjectConverter))]
sealed class BrightnessPolicy
{
    public int Manual { get; set; }=100;
    public int Minimum { get; set; }=0;
    public int Maximum { get; set; }=100;
    public bool Scheduled { get; set; }
    public int DayStartHour { get; set; }=7;
    public int NightStartHour { get; set; }=22;
    public int DayBrightness { get; set; }=100;
    public int NightBrightness { get; set; }=25;
    public bool DimWhenIdle { get; set; }
    public int IdleSeconds { get; set; }=300;
    public int IdleBrightness { get; set; }=10;
    public bool SleepWhenIdle { get; set; }
    public int Evaluate(DateTime now,TimeSpan idle)
    {
        int value=Manual;
        if(Scheduled){bool day=DayStartHour<NightStartHour?now.Hour>=DayStartHour&&now.Hour<NightStartHour:now.Hour>=DayStartHour||now.Hour<NightStartHour;value=day?DayBrightness:NightBrightness;}
        if(DimWhenIdle&&idle.TotalSeconds>=IdleSeconds){if(SleepWhenIdle)return 0;value=IdleBrightness;}
        return Math.Clamp(value,Math.Clamp(Minimum,0,100),Math.Clamp(Maximum,Math.Clamp(Minimum,0,100),100));
    }
}
[TypeConverter(typeof(ExpandableObjectConverter))]
sealed class TouchPolicy
{
    public bool Enabled { get; set; }=true;
    public int SwipePixels { get; set; }=60;
    public int LongPressMs { get; set; }=800;
    public int MinimumWeight { get; set; }=1;
    public int Rotation { get; set; }=3;
    public bool Feedback { get; set; }=true;
    public bool QuickMenu { get; set; }=true;
}
sealed class DeviceAssignment
{
    [Browsable(false)]public string DeviceId { get; set; }="";
    public string Name { get; set; }="Qscreen";
    [Browsable(false)]public string ProfileId { get; set; }="";
    [DisplayName("Automatisch meewisselen"),Description("Zet uit om dit apparaat vast het geselecteerde profiel te laten gebruiken.")]public bool FollowAutomaticProfiles { get; set; }=true;
    public int RequestedPage { get; set; }=-1;
    public string PageRequest { get; set; }="";
    public BrightnessPolicy Brightness { get; set; }=new();
    public TouchPolicy Touch { get; set; }=new();
}
sealed class AlertRule
{
    public string Id { get; set; }=Guid.NewGuid().ToString("N");
    public string Name { get; set; }="CPU warm";
    public bool Enabled { get; set; }=true;
    public WidgetKind Sensor { get; set; }=WidgetKind.CpuTemperature;
    public AlertCondition Condition { get; set; }=AlertCondition.Above;
    public float Threshold { get; set; }=80;
    public float Hysteresis { get; set; }=3;
    [Browsable(false)]public int ColorArgb { get; set; }=System.Drawing.Color.OrangeRed.ToArgb();
    [System.Text.Json.Serialization.JsonIgnore]public System.Drawing.Color Color{get=>System.Drawing.Color.FromArgb(ColorArgb);set=>ColorArgb=value.ToArgb();}
    public int Priority { get; set; }=100;
    public int DurationSeconds { get; set; }=8;
    public int CooldownSeconds { get; set; }=60;
    public bool DynamicColor { get; set; }=true;
    public bool Pulse { get; set; }
    public bool FullScreen { get; set; }
    public bool DesktopNotification { get; set; }
}
sealed class StudioConfiguration
{
    public int Version { get; set; }=1;
    public bool AutomaticProfiles { get; set; }
    public string DefaultProfileId { get; set; }="legacy";
    public string ManualProfileId { get; set; }="";
    public int SwitchDelaySeconds { get; set; }=3;
    public int SamplingSeconds { get; set; }=1;
    public List<ProfileRule> Rules { get; set; }=[];
    public List<DeviceAssignment> Devices { get; set; }=[];
    public List<AlertRule> Alerts { get; set; }=[];
    public static string PathIn(string directory)=>Path.Combine(directory,"studio-settings.json");
    public static StudioConfiguration Load(string directory)
    {
        string path=PathIn(directory);if(!File.Exists(path))return new();
        if(new FileInfo(path).Length>1_000_000)throw new InvalidDataException("Instellingenbestand te groot.");
        var config=JsonSerializer.Deserialize<StudioConfiguration>(File.ReadAllText(path),DesignFiles.Json)??throw new InvalidDataException("Lege instellingen.");
        config.Validate();return config;
    }
    public void Validate()
    {
        if(Version!=1||Rules==null||Devices==null||Alerts==null||Rules.Count>512||Devices.Count>32||Alerts.Count>128)throw new InvalidDataException("Ongeldige studio-instellingen.");
        if(SwitchDelaySeconds is <0 or >120||SamplingSeconds is <1 or >60)throw new InvalidDataException("Wisselvertraging: 0–120 s; meetinterval: 1–60 s.");
        foreach(var rule in Rules)if(rule==null||!Enum.IsDefined(rule.Trigger)||rule.IdleSeconds<1||rule.IdleSeconds>86400||rule.Executable==null)throw new InvalidDataException("Ongeldige profielregel.");
        foreach(var device in Devices)
        {
            if(device==null||device.DeviceId==null||device.Name==null||device.Brightness==null||device.Touch==null)throw new InvalidDataException("Ongeldig apparaat.");
            var b=device.Brightness;var t=device.Touch;
            if(b.Minimum<0||b.Maximum>100||b.Minimum>b.Maximum||b.Manual is <0 or >100||b.DayBrightness is <0 or >100||b.NightBrightness is <0 or >100||b.IdleBrightness is <0 or >100||b.DayStartHour is <0 or >23||b.NightStartHour is <0 or >23||b.IdleSeconds is <1 or >86400)throw new InvalidDataException("Ongeldige helderheidsinstellingen.");
            if(t.SwipePixels is <20 or >250||t.LongPressMs is <400 or >3000||t.MinimumWeight is <1 or >255||t.Rotation is <0 or >3)throw new InvalidDataException("Ongeldige aanraakdrempels.");
        }
        if(Alerts.Select(a=>a?.Id).Distinct().Count()!=Alerts.Count)throw new InvalidDataException("Waarschuwingen moeten unieke ID's hebben.");
        foreach(var rule in Alerts)if(rule==null||string.IsNullOrEmpty(rule.Id)||rule.Name==null||rule.Name.Length>120||!SensorHistory.Metrics.Contains(rule.Sensor)||!Enum.IsDefined(rule.Condition)||!float.IsFinite(rule.Threshold)||!float.IsFinite(rule.Hysteresis)||rule.Hysteresis<0||rule.DurationSeconds is <1 or >300||rule.CooldownSeconds is <1 or >86400)throw new InvalidDataException("Ongeldige waarschuwingsregel.");
    }
    public void Save(string directory){Validate();AtomicJson.Write(PathIn(directory),this);}
    public static void ManualLegacy(string directory)
    {
        if(!File.Exists(PathIn(directory)))return;
        var config=Load(directory);config.ManualProfileId="legacy";config.Save(directory);
    }
}
static class AtomicJson
{
    public static void Write<T>(string path,T value)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        string temporary=path+"."+Guid.NewGuid().ToString("N")+".tmp";
        try{File.WriteAllText(temporary,JsonSerializer.Serialize(value,DesignFiles.Json));File.Move(temporary,path,true);}
        finally{if(File.Exists(temporary))File.Delete(temporary);}
    }
}

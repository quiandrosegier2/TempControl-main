using System.IO.Ports;
using System.Management;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

sealed record DeviceCandidate(string Port,string UsbId,string Label);
sealed record DeviceInformation(string Id,string Firmware,bool Advanced,bool Touch,bool Patch=false,bool Burst=false,bool Compression=false);
sealed record DeviceRuntimeStatus(string DeviceId,string Name,string Port,bool Connected,string Firmware,string ProfileId,string ProfileName,int Page,int PageCount,int Brightness,string Message,DateTime UpdatedUtc,string UsbId="",bool TouchAvailable=false,bool AdvancedControls=false,AnimationTransferMetrics? Performance=null);
sealed record DeviceMeasurements(long HeapFree,long PsramFree,long PsramTotal,long FlashBytes,double DrawMs,double? CompletedDrawsPerSecond,DateTime UpdatedUtc)
{
    public double? ComposeMs {get;init;}
    public double? DisplayTransferMs {get;init;}
    public string Description=>$"ESP heap vrij {HeapFree/1024} KB · PSRAM {(PsramTotal-PsramFree)/1024}/{PsramTotal/1024} KB · tekenen {DrawMs:0.##} ms · voltooide beelden {(CompletedDrawsPerSecond.HasValue?CompletedDrawsPerSecond.Value.ToString("0.##"):"--")}/s";
}
static class DeviceDiscovery
{
    public static IReadOnlyList<DeviceCandidate> Scan(string? fixedPort=null)
    {
        if(fixedPort!=null)return [new(fixedPort,"port:"+fixedPort,fixedPort)];
        using var searcher=new ManagementObjectSearcher("SELECT Name,PNPDeviceID FROM Win32_PnPEntity WHERE PNPDeviceID LIKE '%VID_303A%' AND Name LIKE '%(COM%'");
        var result=new List<DeviceCandidate>();
        foreach(var item in searcher.Get())using(item){string label=item["Name"]?.ToString()??"";var match=Regex.Match(label,@"\((COM\d+)\)");if(!match.Success)continue;string pnp=item["PNPDeviceID"]?.ToString()??match.Groups[1].Value;string id="usb:"+Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(pnp)));result.Add(new(match.Groups[1].Value,id,label));}
        return result.DistinctBy(d=>d.Port,StringComparer.OrdinalIgnoreCase).Take(32).ToArray();
    }
}
interface IDisplaySession : IDisposable
{
    DeviceInformation Information { get; }
    bool Send(byte[] frame);
    void Native(string packet);
    void Configure(string command);
    void KeepAlive();
    void Poll();
}
sealed class DisplaySession : IDisplaySession,IProgressDisplaySession
{
    readonly SerialPort port;
    readonly CustomScreenSender sender;
    DateTime nextStatistics;
    long lastFrames,lastUptime;
    public DeviceMeasurements? Measurements {get;private set;}
    public DeviceInformation Information { get; }
    public DisplaySession(DeviceCandidate candidate,Action<string> onEvent)
    {
        port=new SerialPort(candidate.Port,115200){NewLine="\n",DtrEnable=false,RtsEnable=false,WriteTimeout=5000,ReadTimeout=2500};
        try
        {
            port.Open();port.WriteLine("INFO");string? info=null;
            try{for(int i=0;i<8;i++){string line=port.ReadLine().Trim();if(line.StartsWith("QSCREEN;MODEL=WS146;",StringComparison.Ordinal)){info=line;break;}}}catch(TimeoutException){ }
            if(info!=null)
            {
                var fields=info.Split(';').Where(s=>s.Contains('=')).Select(s=>s.Split('=',2)).ToDictionary(s=>s[0],s=>s[1]);
                string id=fields.GetValueOrDefault("ID","");string caps=fields.GetValueOrDefault("CAPS","");Information=new(id.Length>0?"mac:"+id:candidate.UsbId,fields.GetValueOrDefault("FW","onbekend"),caps.Contains("BRIGHTNESS",StringComparison.Ordinal)&&caps.Contains("ALIVE",StringComparison.Ordinal),caps.Contains("TOUCH:1",StringComparison.Ordinal),caps.Split(',').Contains("PATCH1"),caps.Split(',').Contains("BURST8K"),caps.Split(',').Contains("RLE565"));
            }
            else
            {
                port.WriteLine("PING");bool identified=false;
                for(int i=0;i<8;i++){if(port.ReadLine().Trim()=="TEMPMON"){identified=true;break;}}
                if(!identified)throw new IOException("Geen compatibel Qscreen-scherm.");
                Information=new(candidate.UsbId,"Legacy (versie niet beschikbaar)",false,false);
            }
            port.ReadTimeout=12000;sender=new(new SerialFrameConnection(port),onEvent,Information.Patch,Information.Burst,Information.Compression);
        }
        catch{port.Dispose();throw;}
    }
    public int LastPayloadBytes=>sender.LastPayloadBytes;
    public bool Send(byte[] frame)=>sender.Send(frame);
    public bool SendWithProgress(byte[] frame,Action<int> progress)=>sender.Send(frame,progress);
    public void Native(string packet){sender.InvalidateBaseline();port.WriteLine(packet);}
    public void Configure(string command){if(Information.Advanced)sender.Control(command,"QSETOK");}
    public void KeepAlive(){if(Information.Advanced)sender.Control("ALIVE","ALIVEOK");}
    public void Poll()
    {
        sender.PollEvents();
        if(!Information.Patch||DateTime.UtcNow<nextStatistics)return;
        nextStatistics=DateTime.UtcNow.AddSeconds(5);
        var values=sender.ReadStatistics().Split(';').Skip(1).Select(s=>s.Split('=',2)).Where(p=>p.Length==2&&long.TryParse(p[1],out _)).ToDictionary(p=>p[0],p=>long.Parse(p[1],System.Globalization.CultureInfo.InvariantCulture));
        if(!new[]{"HEAP","PSRAM_FREE","PSRAM_TOTAL","FLASH","DRAW_US","FRAMES","UPTIME_MS"}.All(values.ContainsKey))throw new IOException("Onvolledige ESP-statistieken.");
        long frames=values["FRAMES"],uptime=values["UPTIME_MS"];double? fps=lastUptime>0&&uptime>lastUptime&&frames>=lastFrames?(frames-lastFrames)*1000d/(uptime-lastUptime):null;
        Measurements=new(values["HEAP"],values["PSRAM_FREE"],values["PSRAM_TOTAL"],values["FLASH"],values["DRAW_US"]/1000d,fps,DateTime.UtcNow){ComposeMs=values.TryGetValue("COMPOSE_US",out var compose)?compose/1000d:null,DisplayTransferMs=values.TryGetValue("DISPLAY_US",out var display)?display/1000d:null};lastFrames=frames;lastUptime=uptime;
    }
    public void Dispose()=>port.Dispose();
}

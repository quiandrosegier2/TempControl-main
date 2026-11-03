using System.IO.Ports;
using System.Management;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

sealed record DeviceCandidate(string Port,string UsbId,string Label);
sealed record DeviceInformation(string Id,string Firmware,bool Advanced,bool Touch);
sealed record DeviceRuntimeStatus(string DeviceId,string Name,string Port,bool Connected,string Firmware,string ProfileId,string ProfileName,int Page,int PageCount,int Brightness,string Message,DateTime UpdatedUtc,string UsbId="",bool TouchAvailable=false,bool AdvancedControls=false);
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
sealed class DisplaySession : IDisplaySession
{
    readonly SerialPort port;
    readonly CustomScreenSender sender;
    public DeviceInformation Information { get; }
    public DisplaySession(DeviceCandidate candidate,Action<string> onEvent)
    {
        port=new SerialPort(candidate.Port,115200){NewLine="\n",DtrEnable=false,RtsEnable=false,WriteTimeout=5000,ReadTimeout=900};
        try
        {
            port.Open();port.WriteLine("INFO");string? info=null;
            try{for(int i=0;i<8;i++){string line=port.ReadLine().Trim();if(line.StartsWith("QSCREEN;MODEL=WS146;",StringComparison.Ordinal)){info=line;break;}}}catch(TimeoutException){ }
            if(info!=null)
            {
                var fields=info.Split(';').Where(s=>s.Contains('=')).Select(s=>s.Split('=',2)).ToDictionary(s=>s[0],s=>s[1]);
                string id=fields.GetValueOrDefault("ID","");string caps=fields.GetValueOrDefault("CAPS","");Information=new(id.Length>0?"mac:"+id:candidate.UsbId,fields.GetValueOrDefault("FW","onbekend"),caps.Contains("BRIGHTNESS",StringComparison.Ordinal)&&caps.Contains("ALIVE",StringComparison.Ordinal),caps.Contains("TOUCH:1",StringComparison.Ordinal));
            }
            else
            {
                port.WriteLine("PING");bool identified=false;
                for(int i=0;i<8;i++){if(port.ReadLine().Trim()=="TEMPMON"){identified=true;break;}}
                if(!identified)throw new IOException("Geen compatibel Qscreen-scherm.");
                Information=new(candidate.UsbId,"Legacy (versie niet beschikbaar)",false,false);
            }
            port.ReadTimeout=12000;sender=new(new SerialFrameConnection(port),onEvent);
        }
        catch{port.Dispose();throw;}
    }
    public bool Send(byte[] frame)=>sender.Send(frame);
    public void Native(string packet)=>port.WriteLine(packet);
    public void Configure(string command){if(Information.Advanced)sender.Control(command,"QSETOK");}
    public void KeepAlive(){if(Information.Advanced)sender.Control("ALIVE","ALIVEOK");}
    public void Poll()=>sender.PollEvents();
    public void Dispose()=>port.Dispose();
}

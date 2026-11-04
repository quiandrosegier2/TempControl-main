using LibreHardwareMonitor.Hardware;
using System.Diagnostics;
using System.Management;
using System.Net.NetworkInformation;

// Called only by the existing sensor sampler after Computer.Update(). The GUI
// reads its snapshot and never opens a second hardware provider.
sealed class HardwareCollector
{
    float? maximum;
    float? installed,configured;
    DateTime nextSlow;
    HardwareExtraReading[] disks=[];
    NetworkInterface? network;
    long lastRx,lastTx,lastNetworkTick;
    public HardwareCollector()
    {
        try{using var query=new ManagementObjectSearcher("SELECT Capacity,ConfiguredClockSpeed FROM Win32_PhysicalMemory");query.Options.Timeout=TimeSpan.FromSeconds(2);double bytes=0;var speeds=new List<float>();foreach(var item in query.Get())using(item){bytes+=Convert.ToDouble(item["Capacity"]??0);float speed=Convert.ToSingle(item["ConfiguredClockSpeed"]??0);if(speed>0)speeds.Add(speed);}if(bytes>0)installed=(float)(bytes/1073741824d);if(speeds.Count>0)configured=speeds.Min();}catch(Exception ex)when(ex is ManagementException or UnauthorizedAccessException or System.Runtime.InteropServices.COMException){}
    }
    public HardwareTelemetry Capture(IEnumerable<IHardware> hardware,ScreenReadings readings,GamingReading gaming,IHardware? selectedGpu=null)
    {
        try{return CaptureCore(hardware,readings,gaming,selectedGpu);}
        catch(Exception ex)when(ex is IOException or UnauthorizedAccessException or InvalidOperationException or NetworkInformationException or System.ComponentModel.Win32Exception or System.Runtime.InteropServices.ExternalException){return new(){CpuSessionMax=maximum,RamInstalledGb=installed,RamConfiguredClockMhz=configured,UptimeSeconds=Environment.TickCount64/1000d};}
    }
    HardwareTelemetry CaptureCore(IEnumerable<IHardware> hardware,ScreenReadings readings,GamingReading gaming,IHardware? selectedGpu=null)
    {
        var all=hardware.ToArray();var cpu=all.FirstOrDefault(h=>h.HardwareType==HardwareType.Cpu);
        var gpu=selectedGpu??all.Where(h=>h.HardwareType is HardwareType.GpuNvidia or HardwareType.GpuAmd or HardwareType.GpuIntel).OrderBy(h=>h.HardwareType==HardwareType.GpuNvidia?0:h.HardwareType==HardwareType.GpuAmd?1:2).FirstOrDefault();
        static float? Find(IHardware? h,SensorType type,params string[] names){foreach(string name in names){var value=h?.Sensors.FirstOrDefault(s=>s.SensorType==type&&s.Name.Equals(name,StringComparison.OrdinalIgnoreCase))?.Value;if(value.HasValue&&float.IsFinite(value.Value)&&value>=0)return value;}return null;}
        var memory=all.FirstOrDefault(h=>h.HardwareType==HardwareType.Memory);
        if(readings.Cpu is {} current)maximum=maximum.HasValue?Math.Max(maximum.Value,current):current;
        if(DateTime.UtcNow>=nextSlow)
        {
            nextSlow=DateTime.UtcNow.AddSeconds(30);
            disks=DriveInfo.GetDrives().Where(d=>d.DriveType==DriveType.Fixed).Select(d=>{try{return d.IsReady?new HardwareExtraReading(d.Name,(float)(d.AvailableFreeSpace/1073741824d),"GiB vrij"):null;}catch(IOException){return null;}}).OfType<HardwareExtraReading>().Take(12).ToArray();
            try{var selected=NetworkInterface.GetAllNetworkInterfaces().Where(n=>n.OperationalStatus==OperationalStatus.Up&&n.NetworkInterfaceType is NetworkInterfaceType.Ethernet or NetworkInterfaceType.Wireless80211).OrderByDescending(n=>n.GetIPProperties().GatewayAddresses.Count).ThenByDescending(n=>n.Speed).FirstOrDefault();if(selected?.Id!=network?.Id){lastNetworkTick=0;network=selected;}}catch(NetworkInformationException){network=null;}
        }
        float? receive=null,send=null;
        if(network!=null)try{var stats=network.GetIPv4Statistics();long tick=Stopwatch.GetTimestamp();double seconds=lastNetworkTick==0?0:Stopwatch.GetElapsedTime(lastNetworkTick,tick).TotalSeconds;if(seconds>0&&stats.BytesReceived>=lastRx&&stats.BytesSent>=lastTx){receive=(float)((stats.BytesReceived-lastRx)/seconds);send=(float)((stats.BytesSent-lastTx)/seconds);}lastRx=stats.BytesReceived;lastTx=stats.BytesSent;lastNetworkTick=tick;}catch(NetworkInformationException){}
        HardwareExtraReading[] Extra(Func<IHardware,bool> select,SensorType type,string unit)=>all.Where(select).SelectMany(h=>h.Sensors.Where(s=>s.SensorType==type&&s.Value.HasValue&&float.IsFinite(s.Value.Value)&&s.Value>=0).Select(s=>new HardwareExtraReading(h.Name+" / "+s.Name,s.Value!.Value,unit))).Take(12).ToArray();
        float? used=Find(memory,SensorType.Data,"Memory Used"),available=Find(memory,SensorType.Data,"Memory Available");
        return new(){CpuModel=cpu?.Name,GpuModel=gpu?.Name,CpuPowerWatts=Find(cpu,SensorType.Power,"CPU Package","Package"),GpuPowerWatts=Find(gpu,SensorType.Power,"GPU Package","GPU Power","GPU Board Power","GPU Total"),CpuSessionMax=maximum,GpuMemoryClockMhz=Find(gpu,SensorType.Clock,"GPU Memory"),VramUsedGb=Find(gpu,SensorType.SmallData,"GPU Memory Used","D3D Dedicated Memory Used")/1024,VramTotalGb=Find(gpu,SensorType.SmallData,"GPU Memory Total")/1024,RamInstalledGb=installed,RamUsedGb=used,RamAvailableGb=available,RamConfiguredClockMhz=configured,AverageFps=gaming.AverageFps,FrameTimeMs=gaming.FrameTimeMs,FrameProcess=gaming.Process,NetworkName=network?.Name,ReceiveBytesPerSecond=receive,SendBytesPerSecond=send,UptimeSeconds=Environment.TickCount64/1000d,Fans=Extra(_=>true,SensorType.Fan,"RPM"),Motherboard=Extra(h=>h.HardwareType is HardwareType.Motherboard or HardwareType.SuperIO,SensorType.Temperature,"°C"),Storage=Extra(h=>h.HardwareType==HardwareType.Storage,SensorType.Temperature,"°C"),FreeDisks=disks};
    }
}

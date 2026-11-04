// Qscreen - leest CPU/GPU-temperatuur en FPS en stuurt die via USB naar het ronde ESP32-scherm.
//
// Gebruik:
//   Qscreen.exe                 Qscreen openen (met --monitor de achtergrondmeting starten) (USB VID 303A)
//   Qscreen.exe --port COM5     vaste COM-poort gebruiken
//   Qscreen.exe --list          alle gevonden temperatuursensoren tonen

using LibreHardwareMonitor.Hardware;

if (args.Length == 0 || args.Contains("--settings"))
{
    var ui = new Thread(() => { System.Windows.Forms.Application.SetHighDpiMode(System.Windows.Forms.HighDpiMode.PerMonitorV2); System.Windows.Forms.Application.EnableVisualStyles(); System.Windows.Forms.Application.Run(new StudioShellWindow()); });
    ui.SetApartmentState(ApartmentState.STA);
    ui.Start(); ui.Join(); return;
}


string? fixedPort = null;
bool listOnly = false;
for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--port" && i + 1 < args.Length) fixedPort = args[++i];
    else if (args[i] == "--list") listOnly = true;
}

Mutex? monitorInstance = null;
if (!listOnly)
{
    monitorInstance = new Mutex(false, @"Global\Qscreen.Background", out bool firstMonitor);
    if (!firstMonitor) { monitorInstance.Dispose(); return; }
    AppDomain.CurrentDomain.ProcessExit += (_, _) => monitorInstance.Dispose();
    Log.Start();
    Log.Write("Qscreen-achtergrondmeting wordt gestart.");
    AppDomain.CurrentDomain.UnhandledException += (_, e) => Log.Write($"Onverwachte fout: {e.ExceptionObject}");
}
var computer = new Computer { IsCpuEnabled = true, IsGpuEnabled = true, IsMemoryEnabled = true, IsMotherboardEnabled=true, IsStorageEnabled=true };
computer.Open();
AppDomain.CurrentDomain.ProcessExit += (_, _) => computer.Close();

if (listOnly)
{
    UpdateAll(computer);
    foreach (var hw in AllHardware(computer))
        foreach (var s in hw.Sensors.Where(s => s.SensorType == SensorType.Temperature))
            Console.WriteLine($"[{hw.HardwareType}] {hw.Name} / {s.Name}: {s.Value:0.0} °C");
    return;
}

Log.Write("Qscreen-achtergrondmeting gestart.");
using var fpsMonitor = new FpsMonitor();
AppDomain.CurrentDomain.ProcessExit += (_, _) => fpsMonitor.Dispose();
var hardwareCollector=new HardwareCollector();
await AdvancedMonitor.RunAsync(AppContext.BaseDirectory, () => { UpdateAll(computer); var gpu=AllHardware(computer).Where(h=>h.HardwareType is HardwareType.GpuNvidia or HardwareType.GpuAmd or HardwareType.GpuIntel).OrderBy(h=>h.HardwareType==HardwareType.GpuNvidia?0:h.HardwareType==HardwareType.GpuAmd?1:2).FirstOrDefault(); var readings=new ScreenReadings(
    ReadCpuTemp(computer), gpu==null?null:PickTemp([gpu],["GPU Core","GPU Hot Spot"]), fpsMonitor.TakeFps(),
    ReadLoad(computer, HardwareType.Cpu),
    GpuSensor(gpu,SensorType.Load,"GPU Core"),
    ReadLoad(computer, HardwareType.Memory), DateTime.Now,
    ReadClock(computer, HardwareType.Cpu),
    GpuSensor(gpu,SensorType.Clock,"GPU Core"));return readings with{Hardware=hardwareCollector.Capture(AllHardware(computer),readings,fpsMonitor.Latest,gpu)}; }, fixedPort);
return;

static float? ReadClock(Computer computer, params HardwareType[] types)
{
    var values = AllHardware(computer).Where(h => types.Contains(h.HardwareType)).SelectMany(h => h.Sensors)
        .Where(s => s.SensorType == SensorType.Clock && s.Name.Contains("Core", StringComparison.OrdinalIgnoreCase))
        .Select(s => s.Value).Where(v => v.HasValue && float.IsFinite(v.Value)).ToArray();
    return values.Length == 0 ? null : values.Average(v => v!.Value);
}

static float? ReadLoad(Computer computer, params HardwareType[] types) => AllHardware(computer)
    .Where(h => types.Contains(h.HardwareType)).SelectMany(h => h.Sensors)
    .Where(s => s.SensorType == SensorType.Load && (s.Name.Contains("Total", StringComparison.OrdinalIgnoreCase) || s.Name.Contains("Core", StringComparison.OrdinalIgnoreCase) || s.Name.Contains("Memory", StringComparison.OrdinalIgnoreCase)))
    .OrderBy(s => s.Name.Contains("Total", StringComparison.OrdinalIgnoreCase) ? 0 : s.Name.Equals("GPU Core", StringComparison.OrdinalIgnoreCase) ? 1 : 2)
    .Select(s => s.Value).FirstOrDefault(v => v.HasValue);

static IEnumerable<IHardware> AllHardware(Computer c) =>
    c.Hardware.SelectMany(h => h.SubHardware.Prepend(h));

static void UpdateAll(Computer c)
{
    foreach (var hw in c.Hardware)
    {
        try{hw.Update();foreach(var sub in hw.SubHardware)sub.Update();}
        catch(Exception ex)when(hw.HardwareType is HardwareType.Motherboard or HardwareType.SuperIO or HardwareType.Storage && ex is IOException or InvalidOperationException or System.ComponentModel.Win32Exception or System.Runtime.InteropServices.ExternalException){Log.Write("Optionele sensor tijdelijk niet beschikbaar: "+hw.Name);}
    }
}

static float? PickTemp(IEnumerable<IHardware> hardware, string[] preferred)
{
    var temps = hardware
        .SelectMany(h => h.Sensors)
        .Where(s => s.SensorType == SensorType.Temperature && s.Value is > 0 and < 150)
        .ToList();

    foreach (var name in preferred)
    {
        var match = temps.FirstOrDefault(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (match != null) return match.Value;
    }
    return temps.Count > 0 ? temps.Max(s => s.Value) : null;
}

static float? ReadCpuTemp(Computer c) => PickTemp(
    AllHardware(c).Where(h => h.HardwareType == HardwareType.Cpu),
    ["Core (Tctl/Tdie)", "CPU Package", "Core (Tdie)", "Core (Tctl)", "Core Average"]);

static float? GpuSensor(IHardware? gpu,SensorType type,string name)=>gpu?.Sensors.FirstOrDefault(s=>s.SensorType==type&&s.Name.Equals(name,StringComparison.OrdinalIgnoreCase))?.Value;

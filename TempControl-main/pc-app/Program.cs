// Qscreen - leest CPU/GPU-temperatuur en FPS en stuurt die via USB naar het ronde ESP32-scherm.
//
// Gebruik:
//   Qscreen.exe                 Qscreen openen (met --monitor de achtergrondmeting starten) (USB VID 303A)
//   Qscreen.exe --port COM5     vaste COM-poort gebruiken
//   Qscreen.exe --list          alle gevonden temperatuursensoren tonen

using LibreHardwareMonitor.Hardware;

if (args.Length == 0 || args.Contains("--settings"))
{
    var ui = new Thread(() => { System.Windows.Forms.Application.EnableVisualStyles(); System.Windows.Forms.Application.Run(new StudioShellWindow()); });
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
var computer = new Computer { IsCpuEnabled = true, IsGpuEnabled = true, IsMemoryEnabled = true };
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
await AdvancedMonitor.RunAsync(AppContext.BaseDirectory, () => { UpdateAll(computer); return new ScreenReadings(
    ReadCpuTemp(computer), ReadGpuTemp(computer), fpsMonitor.TakeFps(),
    ReadLoad(computer, HardwareType.Cpu),
    ReadLoad(computer, HardwareType.GpuNvidia, HardwareType.GpuAmd, HardwareType.GpuIntel),
    ReadLoad(computer, HardwareType.Memory), DateTime.Now,
    ReadClock(computer, HardwareType.Cpu),
    ReadClock(computer, HardwareType.GpuNvidia, HardwareType.GpuAmd, HardwareType.GpuIntel)); }, fixedPort);
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
        hw.Update();
        foreach (var sub in hw.SubHardware) sub.Update();
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

static float? ReadGpuTemp(Computer c)
{
    // Eerst een losse videokaart (NVIDIA/AMD), anders de iGPU
    foreach (var type in new[] { HardwareType.GpuNvidia, HardwareType.GpuAmd, HardwareType.GpuIntel })
    {
        var t = PickTemp(AllHardware(c).Where(h => h.HardwareType == type), ["GPU Core", "GPU Hot Spot"]);
        if (t.HasValue) return t;
    }
    return null;
}

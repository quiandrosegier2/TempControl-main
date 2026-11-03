// Qscreen - leest CPU/GPU-temperatuur en FPS en stuurt die via USB naar het ronde ESP32-scherm.
//
// Gebruik:
//   Qscreen.exe                 Qscreen openen (met --monitor de achtergrondmeting starten) (USB VID 303A)
//   Qscreen.exe --port COM5     vaste COM-poort gebruiken
//   Qscreen.exe --list          alle gevonden temperatuursensoren tonen

using System.Globalization;
using System.IO.Ports;
using System.Management;
using System.Text.RegularExpressions;
using LibreHardwareMonitor.Hardware;

if (args.Length == 0 || args.Contains("--settings"))
{
    var ui = new Thread(() => { System.Windows.Forms.Application.EnableVisualStyles(); System.Windows.Forms.Application.Run(new SettingsWindow()); });
    ui.SetApartmentState(ApartmentState.STA);
    ui.Start(); ui.Join(); return;
}

const int IntervalMs = 1000;
const string EspressifVid = "VID_303A";

string? fixedPort = null;
bool listOnly = false;
for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--port" && i + 1 < args.Length) fixedPort = args[++i];
    else if (args[i] == "--list") listOnly = true;
}

if (!listOnly)
{
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
string layout = LayoutSettings.Read(AppContext.BaseDirectory);
Log.Write($"Layout: {layout}");
var fpsMonitor = new FpsMonitor();
AppDomain.CurrentDomain.ProcessExit += (_, _) => fpsMonitor.Dispose();
SerialPort? port = null;
CustomScreenSender? customSender = null;
using var customRenderer = new DesignRenderer();
ScreenDesign? customDesign = null;
DateTime designModified = DateTime.MinValue;
string? designError = null;
bool customFirmwareWarning = false;
string customRevision = "";
string? lastStatus = null;

while (true)
{
    layout = LayoutSettings.Read(AppContext.BaseDirectory);
    UpdateAll(computer);
    float? cpu = ReadCpuTemp(computer);
    float? gpu = ReadGpuTemp(computer);
    float? fps = fpsMonitor.TakeFps();
    string line = $"CPU={Fmt(cpu)};GPU={Fmt(gpu)};FPS={(fps.HasValue ? fps.Value.ToString("0", CultureInfo.InvariantCulture) : "nan")};LAYOUT={layout}";

    try
    {
        if (port == null)
        {
            port = OpenDisplayPort(fixedPort);
            customSender = port == null ? null : new CustomScreenSender(new SerialFrameConnection(port));
            customFirmwareWarning = false;
        }
        if (port != null)
        {
            bool sentCustom = false;
            if (File.Exists(Path.Combine(AppContext.BaseDirectory, "custom-active.txt")))
            {
                try
                {
                    string path = Path.Combine(AppContext.BaseDirectory, "custom-design.json");
                    DateTime modified = File.GetLastWriteTimeUtc(path);
                    if (customDesign == null || modified != designModified)
                    {
                        customRevision = ScreenDelivery.RevisionOf(AppContext.BaseDirectory);
                        customDesign = DesignFiles.Load(path); designModified = modified;
                    }
                    var readings = new ScreenReadings(cpu, gpu, fps,
                        ReadLoad(computer, HardwareType.Cpu),
                        ReadLoad(computer, HardwareType.GpuNvidia, HardwareType.GpuAmd, HardwareType.GpuIntel),
                        ReadLoad(computer, HardwareType.Memory), DateTime.Now);
                    using var frame = customRenderer.Render(customDesign, readings);
                    ScreenDelivery.Write(AppContext.BaseDirectory,customRevision,"sending","Beeld verzenden");
                    sentCustom = customSender!.Send(DesignRenderer.Rgb565(frame));
                    ScreenDelivery.Write(AppContext.BaseDirectory,customRevision,sentCustom ? "received" : "firmware-required",sentCustom ? "FRAMEOK" : "QSCREEN_FRAME2 ontbreekt");
                    if (!sentCustom && !customFirmwareWarning)
                    {
                        Log.Write("Eigen ontwerpen vereisen Qscreen-firmware 1.5.1. Werk het scherm bij via de app.");
                        customFirmwareWarning = true;
                    }
                    designError = null;
                }
                catch (Exception ex) when (ex is System.Text.Json.JsonException or InvalidDataException or UnauthorizedAccessException or ArgumentException or FileNotFoundException or DirectoryNotFoundException)
                {
                    if (designError != ex.Message) Log.Write("Eigen ontwerp: " + ex.Message);
                    designError = ex.Message;
                    ScreenDelivery.Write(AppContext.BaseDirectory,customRevision,"invalid-design",ex.Message);
                }
            }
            if (!sentCustom)
            {
                port.Write(line + "\n");
                if(!File.Exists(Path.Combine(AppContext.BaseDirectory,"custom-active.txt")))ScreenDelivery.Write(AppContext.BaseDirectory,"","preset","USB verbonden");
            }
            Console.Write($"\r{port.PortName} -> {line}      ");
        }
        else if (lastStatus != "notfound")
        {
            Log.Write($"Scherm niet gevonden, blijf zoeken... ({line})");
            lastStatus = "notfound";
        }
        if(port==null)ScreenDelivery.Write(AppContext.BaseDirectory,customRevision,"disconnected","USB-scherm niet gevonden.");

        // Ontbrekende sensoren eenmalig melden
        string sensors = $"cpu:{cpu.HasValue} gpu:{gpu.HasValue}";
        if (port != null && lastStatus != sensors)
        {
            Log.Write($"Verstuurd: {line}" +
                (cpu.HasValue ? "" : "  (geen CPU-temperatuur: draait de app als administrator en is PawnIO geinstalleerd?)") +
                (gpu.HasValue ? "" : "  (geen GPU-temperatuur gevonden)"));
            lastStatus = sensors;
        }
    }
    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException or TimeoutException)
    {
        Log.Write($"Verbinding verbroken: {ex.Message}");
        ScreenDelivery.Write(AppContext.BaseDirectory,customRevision,"disconnected",ex.Message);
        lastStatus = null;
        try { port?.Dispose(); } catch { }
        port = null;
    }

    Thread.Sleep(IntervalMs);
}

static string Fmt(float? v) => v.HasValue ? v.Value.ToString("0.0", CultureInfo.InvariantCulture) : "nan";

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

static SerialPort? OpenDisplayPort(string? fixedPort)
{
    string? name = fixedPort ?? FindEspressifPort();
    if (name == null) return null;

    var port = new SerialPort(name, 115200)
    {
        NewLine = "\n",
        WriteTimeout = 5000,
        ReadTimeout = 12000,
        // DTR/RTS uit laten: anders kan de ESP32-S3 resetten bij openen
        DtrEnable = false,
        RtsEnable = false,
    };
    port.Open();
    Log.Write($"Verbonden met scherm op {name}");
    return port;
}

static string? FindEspressifPort()
{
    using var searcher = new ManagementObjectSearcher(
        $"SELECT Name FROM Win32_PnPEntity WHERE PNPDeviceID LIKE '%{EspressifVid}%' AND Name LIKE '%(COM%'");
    foreach (var obj in searcher.Get())
    {
        var m = Regex.Match(obj["Name"]?.ToString() ?? "", @"\((COM\d+)\)");
        if (m.Success) return m.Groups[1].Value;
    }
    return null;
}

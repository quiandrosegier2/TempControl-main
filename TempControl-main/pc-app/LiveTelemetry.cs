using System.Globalization;
using System.Text.Json;

sealed record LiveTelemetry(DateTime UpdatedUtc,ScreenReadings Readings)
{
    static string PathFor(string directory)=>Path.Combine(directory,"live-telemetry.json");
    public static void Write(string directory,ScreenReadings readings)
    {
        static float? Clean(float? value)=>value.HasValue&&float.IsFinite(value.Value)?value:null;
        var values=readings with{Cpu=Clean(readings.Cpu),Gpu=Clean(readings.Gpu),Fps=Clean(readings.Fps),CpuLoad=Clean(readings.CpuLoad),GpuLoad=Clean(readings.GpuLoad),MemoryLoad=Clean(readings.MemoryLoad),CpuClock=Clean(readings.CpuClock),GpuClock=Clean(readings.GpuClock),Weather=readings.Weather is {} weather&&float.IsFinite(weather.Temperature)?weather:null,History=null,Alerts=null};
        try{AtomicJson.Write(PathFor(directory),new LiveTelemetry(DateTime.UtcNow,values));}
        catch(Exception ex)when(ex is IOException or UnauthorizedAccessException){ }
    }
    public static LiveTelemetry? Read(string directory,DateTime utcNow)
    {
        try
        {
            string file=PathFor(directory);if(new FileInfo(file).Length>65536)return null;
            var snapshot=JsonSerializer.Deserialize<LiveTelemetry>(File.ReadAllText(file),DesignFiles.Json);
            return snapshot?.Readings!=null&&utcNow-snapshot.UpdatedUtc<=TimeSpan.FromSeconds(5)&&snapshot.UpdatedUtc-utcNow<TimeSpan.FromSeconds(2)?snapshot:null;
        }
        catch(Exception ex)when(ex is IOException or UnauthorizedAccessException or JsonException){return null;}
    }
    public static ScreenReadings Unavailable(DateTime now)=>new(null,null,null,null,null,null,now);
    public static string Summary(LiveTelemetry? snapshot)
    {
        if(snapshot==null)return "LIVE · Geen recente meting. Start de achtergrondmeting; ontbrekende waarden worden als -- getoond.";
        string F(float? value)=>value.HasValue&&float.IsFinite(value.Value)?value.Value.ToString("0.#",CultureInfo.CurrentCulture):"--";
        var r=snapshot.Readings;return $"LIVE · CPU {F(r.Cpu)}°C  ·  GPU {F(r.Gpu)}°C  ·  FPS {F(r.Fps)}  ·  RAM {F(r.MemoryLoad)}%";
    }
}

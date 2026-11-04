sealed record HardwareExtraReading(string Name,float Value,string Unit);
sealed record HardwareTelemetry
{
    public string? CpuModel {get;init;}
    public string? GpuModel {get;init;}
    public float? CpuPowerWatts {get;init;}
    public float? GpuPowerWatts {get;init;}
    public float? CpuSessionMax {get;init;}
    public float? GpuMemoryClockMhz {get;init;}
    public float? VramUsedGb {get;init;}
    public float? VramTotalGb {get;init;}
    public float? RamInstalledGb {get;init;}
    public float? RamUsedGb {get;init;}
    public float? RamAvailableGb {get;init;}
    public float? RamConfiguredClockMhz {get;init;}
    public float? AverageFps {get;init;}
    public float? FrameTimeMs {get;init;}
    public string? FrameProcess {get;init;}
    public string? NetworkName {get;init;}
    public float? ReceiveBytesPerSecond {get;init;}
    public float? SendBytesPerSecond {get;init;}
    public double UptimeSeconds {get;init;}
    public HardwareExtraReading[] Fans {get;init;}=[];
    public HardwareExtraReading[] Motherboard {get;init;}=[];
    public HardwareExtraReading[] Storage {get;init;}=[];
    public HardwareExtraReading[] FreeDisks {get;init;}=[];
}
sealed record DashboardHistoryRequest(int Minutes,DateTime UpdatedUtc);
sealed record DashboardHistorySnapshot(DateTime UpdatedUtc,int Minutes,Dictionary<WidgetKind,HistorySeries> Series,Dictionary<WidgetKind,HistorySeries>? Recent=null);

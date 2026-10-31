static class LayoutSettings
{
    public static readonly string[] Names = [
        "classic", "gauges", "minimal", "orbit", "radar", "neon-pulse", "reactor",
        "waveform", "matrix", "starfield", "speedometer", "aurora", "hex-grid"
    ];
    public static string Read(string appDirectory)
    {
        try
        {
            string value = File.ReadAllText(Path.Combine(appDirectory, "layout.txt")).Trim();
            return Names.Contains(value, StringComparer.Ordinal) ? value : "classic";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return "classic";
        }
    }
}

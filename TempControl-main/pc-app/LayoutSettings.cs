static class LayoutSettings
{
    public static string Read(string appDirectory)
    {
        try
        {
            string value = File.ReadAllText(Path.Combine(appDirectory, "layout.txt")).Trim();
            return value is "classic" or "gauges" or "minimal" ? value : "classic";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return "classic";
        }
    }
}

string directory = Path.Combine(AppContext.BaseDirectory, "test-data");
Directory.CreateDirectory(directory);
string file = Path.Combine(directory, "layout.txt");
void Expect(string expected)
{
    string actual = LayoutSettings.Read(directory);
    if (actual != expected) throw new Exception($"Expected {expected}, got {actual}");
}
try
{
    Expect("classic");
    foreach (string layout in new[] { "classic", "gauges", "minimal" })
    {
        File.WriteAllText(file, "  " + layout + "\r\n");
        Expect(layout);
    }
    foreach (string invalid in new[] { "", "unknown", "gauges;CPU=999", "MINIMAL" })
    {
        File.WriteAllText(file, invalid);
        Expect("classic");
    }
    File.Delete(file);
    Directory.CreateDirectory(file);
    Expect("classic"); // An unreadable configuration must not stop monitoring.
    Directory.Delete(file);
    Console.WriteLine("PASS: default, all three layouts, whitespace, invalid settings and I/O failure.");
}
finally
{
    if (File.Exists(file)) File.Delete(file);
    if (Directory.Exists(file)) Directory.Delete(file);
    Directory.Delete(directory);
}

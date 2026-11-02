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
    if (LayoutSettings.Names.Length != 31 || LayoutSettings.Names.Distinct().Count() != 31)
        throw new Exception("Expected thirty-one unique layouts");
    string project = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../.."));
    var firmwareNames = System.Text.RegularExpressions.Regex.Matches(
        File.ReadAllText(Path.Combine(project, "firmware/src/display-layout.h")),
        "strcmp\\(name,\\s*\"([^\"]+)\"\\)").Select(match => match.Groups[1].Value);
    var installerNames = System.Text.RegularExpressions.Regex.Matches(
        File.ReadAllText(Path.Combine(project, "pc-app/installer/TempMonitor.iss")),
        "AddLayout\\('([^']+)'").Select(match => match.Groups[1].Value);
    if (!LayoutSettings.Names.SequenceEqual(firmwareNames) || !LayoutSettings.Names.SequenceEqual(installerNames))
        throw new Exception("Layout identifiers differ between app, firmware and installer");
    foreach (string layout in LayoutSettings.Names)
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
    Console.WriteLine("PASS: default, all thirty-one layouts, whitespace, invalid settings and I/O failure.");
}
finally
{
    if (File.Exists(file)) File.Delete(file);
    if (Directory.Exists(file)) Directory.Delete(file);
    Directory.Delete(directory);
}

using System.Diagnostics;
using System.Runtime.InteropServices;

sealed record ActivitySnapshot(string Foreground,IReadOnlySet<string> Running,TimeSpan Idle)
{
    [DllImport("user32.dll")]static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")]static extern uint GetWindowThreadProcessId(IntPtr window,out uint pid);
    [DllImport("user32.dll")]static extern bool GetLastInputInfo(ref LastInput input);
    [StructLayout(LayoutKind.Sequential)]struct LastInput{public uint Size,Tick;}
    public static string Normalize(string executable)=>Path.GetFileNameWithoutExtension(executable.Trim()).ToLowerInvariant();
    public static ActivitySnapshot Capture()
    {
        var running=new HashSet<string>(StringComparer.OrdinalIgnoreCase);string foreground="";
        GetWindowThreadProcessId(GetForegroundWindow(),out uint active);
        foreach(var process in Process.GetProcesses())using(process){try{string name=Normalize(process.ProcessName);running.Add(name);if(process.Id==active)foreground=name;}catch(Exception ex)when(ex is InvalidOperationException or System.ComponentModel.Win32Exception) { }}
        var input=new LastInput{Size=(uint)Marshal.SizeOf<LastInput>()};
        TimeSpan idle=GetLastInputInfo(ref input)?TimeSpan.FromMilliseconds(unchecked((uint)Environment.TickCount-input.Tick)):TimeSpan.Zero;
        return new(foreground,running,idle);
    }
}
sealed class ProfileEngine
{
    string active="legacy",candidate="legacy";
    DateTime candidateSince;
    public string Select(StudioConfiguration config,ActivitySnapshot activity,DateTime now,Func<string,bool> exists)
    {
        string fallback=exists(config.DefaultProfileId)?config.DefaultProfileId:"legacy";
        if(!string.IsNullOrWhiteSpace(config.ManualProfileId)&&exists(config.ManualProfileId)){active=candidate=config.ManualProfileId;candidateSince=now;return active;}
        string desired=fallback;
        if(config.AutomaticProfiles)
        {
            var match=config.Rules.Where(r=>r.Enabled&&exists(r.ProfileId)).OrderByDescending(r=>r.Priority).FirstOrDefault(r=>r.Trigger switch{
                ProfileTrigger.Foreground=>activity.Foreground==ActivitySnapshot.Normalize(r.Executable),
                ProfileTrigger.Running=>activity.Running.Contains(ActivitySnapshot.Normalize(r.Executable)),
                ProfileTrigger.Idle=>activity.Idle.TotalSeconds>=r.IdleSeconds,_=>false});
            if(match!=null)desired=match.ProfileId;
        }
        if(desired!=candidate){candidate=desired;candidateSince=now;}
        if(!exists(active)||config.SwitchDelaySeconds==0||now-candidateSince>=TimeSpan.FromSeconds(config.SwitchDelaySeconds))active=desired;
        return active;
    }
}
sealed record ProfileEntry(string Id,string Name,string File);
sealed class ProfileRepository(string directory)
{
    string Root=>Path.Combine(directory,"profiles");
    public bool Exists(string id)=>id=="legacy"||(Guid.TryParseExact(id,"N",out _)&&File.Exists(Path.Combine(Root,id+".qscreen")));
    public string PathFor(string id)
    {
        if(id=="legacy")return Path.Combine(directory,"custom-design.json");
        if(!Guid.TryParseExact(id,"N",out _))throw new InvalidDataException("Ongeldig profiel-ID.");
        return Path.Combine(Root,id+".qscreen");
    }
    public IReadOnlyList<ProfileEntry> List()
    {
        Directory.CreateDirectory(Root);var profiles=new List<ProfileEntry>();
        foreach(string file in Directory.EnumerateFiles(Root,"*.qscreen"))try{string id=Path.GetFileNameWithoutExtension(file);if(Guid.TryParseExact(id,"N",out _))profiles.Add(new(id,ReadName(file,id),file));}catch(Exception ex)when(ex is IOException or InvalidDataException or System.Text.Json.JsonException or ArgumentException){ }
        return profiles.OrderBy(p=>p.Name,StringComparer.CurrentCultureIgnoreCase).ToList();
    }
    static string ReadName(string path,string fallback)
    {
        using var stream=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete);
        if(stream.Length>64_000_000)throw new InvalidDataException("Profiel te groot.");
        var prefix=new byte[16384];int count=stream.Read(prefix,0,prefix.Length);var reader=new System.Text.Json.Utf8JsonReader(prefix.AsSpan(0,count),false,new System.Text.Json.JsonReaderState(new(){MaxDepth=16}));
        while(reader.Read())if(reader.TokenType==System.Text.Json.JsonTokenType.PropertyName&&reader.CurrentDepth==1&&reader.ValueTextEquals("Name")&&reader.Read()&&reader.TokenType==System.Text.Json.JsonTokenType.String){string name=reader.GetString()??fallback;return name[..Math.Min(120,name.Length)];}
        return fallback;
    }
    public string Add(ScreenDesign design)
    {
        design=DesignSnapshot.Capture(design).Restore();var names=List().Select(p=>p.Name).ToHashSet(StringComparer.CurrentCultureIgnoreCase);string original=design.Name[..Math.Min(110,design.Name.Length)];int suffix=2;
        while(names.Contains(design.Name))design.Name=original+" ("+(suffix++)+")";
        string id=Guid.NewGuid().ToString("N");DesignFiles.Save(PathFor(id),design);return id;
    }
    public void Save(string id,ScreenDesign design)=>DesignFiles.Save(PathFor(id),design);
    public void Delete(string id){if(id=="legacy")throw new InvalidOperationException("Het huidige ontwerp kan hier niet worden verwijderd.");File.Delete(PathFor(id));}
}

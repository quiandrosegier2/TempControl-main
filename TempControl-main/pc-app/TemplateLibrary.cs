using System.IO.Compression;
using System.Text.Json;

sealed class TemplateManifest
{
    public string Format { get; set; }="QscreenPackage";
    public int Version { get; set; }=1;
    public string Target { get; set; }="WS146";
    public string MinimumAppVersion { get; set; }="2.0.0";
    public string Name { get; set; }="Ontwerp";
    public TemplateCategory Category { get; set; }
    public string Author { get; set; }="Lokaal";
    public DateTime CreatedUtc { get; set; }=DateTime.UtcNow;
}
sealed record DesignTemplate(string Id,TemplateManifest Manifest,ScreenDesign Design);
interface ICommunityDesignSource
{
    Task<IReadOnlyList<DesignTemplate>> SearchAsync(string query,TemplateCategory? category,CancellationToken cancellation=default);
    Task<DesignTemplate> DownloadAsync(string id,CancellationToken cancellation=default);
    Task PublishAsync(DesignTemplate template,CancellationToken cancellation=default);
}
static class QscreenPackage
{
    const long Limit=64_000_000;
    static string ReadBounded(Stream stream,int maximum)
    {
        using var reader=new StreamReader(stream);var builder=new System.Text.StringBuilder();var buffer=new char[8192];int read;
        while((read=reader.Read(buffer,0,buffer.Length))>0){if(builder.Length+read>maximum)throw new InvalidDataException("Pakketonderdeel overschrijdt de maximale uitgepakte grootte.");builder.Append(buffer,0,read);}return builder.ToString();
    }
    public static void Export(string path,ScreenDesign design,TemplateManifest manifest)
    {
        string json=DesignFiles.Serialize(design);DesignFiles.Parse(json);string metadata=JsonSerializer.Serialize(manifest,DesignFiles.Json);
        using var renderer=new DesignRenderer();using var image=renderer.Render(design,ScreenReadings.Demo);using var png=new MemoryStream();image.Save(png,System.Drawing.Imaging.ImageFormat.Png);
        if(System.Text.Encoding.UTF8.GetByteCount(json)+System.Text.Encoding.UTF8.GetByteCount(metadata)+png.Length>Limit)throw new InvalidDataException("Het complete pakket is groter dan 64 MB.");
        string temp=path+"."+Guid.NewGuid().ToString("N")+".tmp";
        try
        {
            using(var stream=File.Create(temp))using(var zip=new ZipArchive(stream,ZipArchiveMode.Create))
            {
                using(var writer=new StreamWriter(zip.CreateEntry("manifest.json").Open()))writer.Write(metadata);
                using(var writer=new StreamWriter(zip.CreateEntry("design.qscreen").Open()))writer.Write(json);
                using var entry=zip.CreateEntry("preview.png").Open();png.Position=0;png.CopyTo(entry);
            }
            File.Move(temp,path,true);
        }
        finally{if(File.Exists(temp))File.Delete(temp);}
    }
    public static DesignTemplate Import(string path)
    {
        if(new FileInfo(path).Length>Limit)throw new InvalidDataException("Pakket groter dan 64 MB.");
        using var zip=ZipFile.OpenRead(path);
        var allowed=new HashSet<string>(StringComparer.Ordinal){"manifest.json","design.qscreen","preview.png"};
        if(zip.Entries.Count is <2 or >3||zip.Entries.Sum(e=>e.Length)>Limit||zip.Entries.Any(e=>!allowed.Contains(e.FullName))||zip.Entries.Select(e=>e.FullName).Distinct().Count()!=zip.Entries.Count)throw new InvalidDataException("Ongeldig of onveilig pakket. Alleen manifest, ontwerp en voorbeeld zijn toegestaan.");
        string Read(string name,int max)
        {
            var entry=zip.GetEntry(name)??throw new InvalidDataException("Ontbrekend pakketonderdeel: "+name);
            if(entry.Length>max)throw new InvalidDataException("Pakketonderdeel te groot.");
            return ReadBounded(entry.Open(),max);
        }
        var manifest=JsonSerializer.Deserialize<TemplateManifest>(Read("manifest.json",16384),DesignFiles.Json)??throw new InvalidDataException("Manifest ontbreekt.");
        if(manifest.Format!="QscreenPackage"||manifest.Version!=1||manifest.Target!="WS146"||string.IsNullOrWhiteSpace(manifest.Name)||manifest.Name.Length>120||manifest.Author==null||manifest.Author.Length>120||!Enum.IsDefined(manifest.Category)||!Version.TryParse(manifest.MinimumAppVersion,out var minimum)||minimum>new Version(2,0,0))throw new InvalidDataException("Pakket is niet compatibel met deze versie of dit scherm.");
        var design=DesignFiles.Parse(Read("design.qscreen",(int)Limit));
        return new(Guid.NewGuid().ToString("N"),manifest,design);
    }
    public static TemplateManifest Metadata(string path)
    {
        if(new FileInfo(path).Length>Limit)throw new InvalidDataException("Pakket te groot.");
        using var zip=ZipFile.OpenRead(path);var names=new HashSet<string>(StringComparer.Ordinal){"manifest.json","design.qscreen","preview.png"};
        if(zip.Entries.Count is <2 or >3||zip.Entries.Any(e=>!names.Contains(e.FullName))||zip.Entries.Sum(e=>e.Length)>Limit||zip.Entries.Select(e=>e.FullName).Distinct().Count()!=zip.Entries.Count)throw new InvalidDataException("Onveilig pakket.");
        var entry=zip.GetEntry("manifest.json")??throw new InvalidDataException("Manifest ontbreekt.");if(entry.Length>16384)throw new InvalidDataException("Manifest te groot.");
        var manifest=JsonSerializer.Deserialize<TemplateManifest>(ReadBounded(entry.Open(),16384),DesignFiles.Json)??throw new InvalidDataException("Manifest ontbreekt.");
        if(manifest.Format!="QscreenPackage"||manifest.Version!=1||manifest.Target!="WS146"||string.IsNullOrWhiteSpace(manifest.Name)||manifest.Name.Length>120||manifest.Author==null||manifest.Author.Length>120||!Enum.IsDefined(manifest.Category)||!Version.TryParse(manifest.MinimumAppVersion,out var min)||min>new Version(2,0,0))throw new InvalidDataException("Niet compatibel.");
        return manifest;
    }
}
sealed class LocalTemplateLibrary(string directory) : ICommunityDesignSource
{
    string Root=>Path.Combine(directory,"templates");
    public string Add(ScreenDesign design,TemplateCategory category,TemplateManifest? manifest=null)
    {
        Directory.CreateDirectory(Root);string id=Guid.NewGuid().ToString("N");QscreenPackage.Export(Path.Combine(Root,id+".qscreenpack"),design,manifest??new(){Name=design.Name,Category=category});return id;
    }
    public Task<IReadOnlyList<DesignTemplate>> SearchAsync(string query,TemplateCategory? category,CancellationToken cancellation=default)=>Task.Run<IReadOnlyList<DesignTemplate>>(()=>
    {
        Directory.CreateDirectory(Root);var list=new List<DesignTemplate>();
        foreach(string file in Directory.EnumerateFiles(Root,"*.qscreenpack"))
        {
            cancellation.ThrowIfCancellationRequested();
            try{string id=Path.GetFileNameWithoutExtension(file);if(!Guid.TryParseExact(id,"N",out _))continue;var manifest=QscreenPackage.Metadata(file);var item=new DesignTemplate(id,manifest,new(){Name=manifest.Name});if((category==null||item.Manifest.Category==category)&&(string.IsNullOrWhiteSpace(query)||item.Manifest.Name.Contains(query,StringComparison.CurrentCultureIgnoreCase)||item.Manifest.Author.Contains(query,StringComparison.CurrentCultureIgnoreCase)))list.Add(item);}
            catch(Exception ex)when(ex is IOException or InvalidDataException or JsonException or ArgumentException){ }
        }
        return list;
    },cancellation);
    public Task<DesignTemplate> DownloadAsync(string id,CancellationToken cancellation=default)
    {
        cancellation.ThrowIfCancellationRequested();if(!Guid.TryParseExact(id,"N",out _))throw new InvalidDataException("Ongeldig sjabloon-ID.");
        return Task.Run(()=>QscreenPackage.Import(Path.Combine(Root,id+".qscreenpack")) with{Id=id},cancellation);
    }
    public Task PublishAsync(DesignTemplate template,CancellationToken cancellation=default){cancellation.ThrowIfCancellationRequested();Add(template.Design,template.Manifest.Category,template.Manifest);return Task.CompletedTask;}
    public void EnsureExamples()
    {
        Directory.CreateDirectory(Root);if(Directory.EnumerateFiles(Root,"*.qscreenpack").Any())return;
        foreach(var category in Enum.GetValues<TemplateCategory>())
        {
            var design=ScreenDesign.CreateDefault();design.Name="Qscreen / "+category;
            design.BackgroundArgb=(category switch{TemplateCategory.Gaming=>System.Drawing.Color.FromArgb(35,10,22),TemplateCategory.Cyberpunk=>System.Drawing.Color.FromArgb(15,8,40),TemplateCategory.Clock=>System.Drawing.Color.FromArgb(10,22,30),_=>System.Drawing.Color.FromArgb(10,15,24)}).ToArgb();
            if(category==TemplateCategory.Clock){design.Widgets=design.Widgets.Where(w=>w.Kind==WidgetKind.Time||w.Name=="Titel").ToList();var clock=design.Widgets.First(w=>w.Kind==WidgetKind.Time);clock.Y=180;clock.FontSize=48;clock.Width=330;clock.X=41;design.Widgets.Add(new(){Kind=WidgetKind.Date,Name="Datum",Format="dddd dd MMMM",Y=250,FontSize=19});}
            if(category is TemplateCategory.Hardware or TemplateCategory.Productivity)design.Widgets.Add(new(){Kind=WidgetKind.HistoryGraph,Name="CPU-historie",BarSource=WidgetKind.CpuTemperature,X=66,Y=230,Width=280,Height=55,Foreground=System.Drawing.Color.Cyan});
            Add(design,category);
        }
    }
}

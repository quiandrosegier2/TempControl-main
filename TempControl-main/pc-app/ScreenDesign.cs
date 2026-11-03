using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;

[TypeConverter(typeof(WidgetKindConverter))]
enum WidgetKind { Text, CpuTemperature, GpuTemperature, Fps, Time, Date, CpuLoad, GpuLoad, MemoryLoad, Rectangle, Ellipse, ProgressBar }
enum BackgroundFit { Cover, Contain, Stretch }

static class WidgetLabels
{
    public static string Title(WidgetKind kind) => kind switch {
        WidgetKind.Text=>"Eigen tekst",WidgetKind.CpuTemperature=>"CPU-temperatuur",WidgetKind.GpuTemperature=>"GPU-temperatuur",WidgetKind.Fps=>"FPS",
        WidgetKind.Time=>"Klok",WidgetKind.Date=>"Datum",WidgetKind.CpuLoad=>"CPU-belasting (%)",WidgetKind.GpuLoad=>"GPU-belasting (%)",WidgetKind.MemoryLoad=>"Geheugengebruik (%)",
        WidgetKind.Rectangle=>"Rechthoek",WidgetKind.Ellipse=>"Cirkel / ovaal",WidgetKind.ProgressBar=>"Voortgangsbalk",_=>kind.ToString()
    };
}
sealed class WidgetKindConverter() : EnumConverter(typeof(WidgetKind))
{
    public override object? ConvertTo(ITypeDescriptorContext? context,CultureInfo? culture,object? value,Type destinationType) =>
        destinationType==typeof(string)&&value is WidgetKind kind ? WidgetLabels.Title(kind) : base.ConvertTo(context,culture,value,destinationType);
    public override object? ConvertFrom(ITypeDescriptorContext? context,CultureInfo? culture,object value)
    {
        if(value is string text)foreach(var kind in Enum.GetValues<WidgetKind>())if(WidgetLabels.Title(kind)==text)return kind;
        return base.ConvertFrom(context,culture,value);
    }
}

sealed class ScreenWidget
{
    [Category("Inhoud"),DisplayName("Naam")] public string Name { get; set; } = "Tekst";
    [Category("Inhoud"),DisplayName("Elementtype")] public WidgetKind Kind { get; set; }
    [Category("Inhoud"),DisplayName("Tekst / opmaak"), Description("Gebruik {value} voor de meetwaarde. Voor klok en datum bijvoorbeeld HH:mm:ss of dd-MM-yyyy.")]
    public string Format { get; set; } = "Qscreen";
    [Category("Positie")] public int X { get; set; } = 106;
    [Category("Positie")] public int Y { get; set; } = 180;
    [Category("Positie"),DisplayName("Breedte")] public int Width { get; set; } = 200;
    [Category("Positie"),DisplayName("Hoogte")] public int Height { get; set; } = 52;
    [Category("Lettertype"),DisplayName("Lettertype")] public string FontFamily { get; set; } = "Segoe UI";
    [Category("Lettertype"),DisplayName("Grootte (px)")] public float FontSize { get; set; } = 32;
    [Category("Lettertype"),DisplayName("Vet")] public bool Bold { get; set; } = true;
    [Category("Lettertype"),DisplayName("Cursief")] public bool Italic { get; set; }
    [Category("Lettertype"),DisplayName("Uitlijning")] public StringAlignment Alignment { get; set; } = StringAlignment.Center;
    [Browsable(false)] public int ForegroundArgb { get; set; } = Color.White.ToArgb();
    [Browsable(false)] public int FillArgb { get; set; } = Color.FromArgb(25, 30, 40).ToArgb();
    [JsonIgnore, Category("Kleuren"),DisplayName("Kleur")] public Color Foreground { get => Color.FromArgb(ForegroundArgb); set => ForegroundArgb = value.ToArgb(); }
    [JsonIgnore, Category("Kleuren"),DisplayName("Balkachtergrond")] public Color Fill { get => Color.FromArgb(FillArgb); set => FillArgb = value.ToArgb(); }
    [Category("Kleuren"),DisplayName("Temperatuurkleuren"),Description("Blauw t/m 50 graden, rood erboven. Zet dit uit om je eigen tekstkleur te gebruiken.")] public bool TemperatureColors { get; set; } = true;
    [Category("Balk"),DisplayName("Meetwaarde"), Description("Kies de temperatuur, belasting, het geheugenpercentage of FPS voor deze balk.")]
    public WidgetKind BarSource { get; set; } = WidgetKind.CpuLoad;
    [Category("Balk"),DisplayName("Maximum")] public float Maximum { get; set; } = 100;
    [Category("Weergave"),DisplayName("Zichtbaar")] public bool Visible { get; set; } = true;
    public override string ToString() => Name;
}

sealed class ScreenDesign
{
    public int Version { get; set; } = 1;
    public string Name { get; set; } = "Mijn Qscreen";
    public int BackgroundArgb { get; set; } = Color.FromArgb(10, 14, 24).ToArgb();
    public string? BackgroundPng { get; set; }
    public BackgroundFit Fit { get; set; } = BackgroundFit.Cover;
    public int Darken { get; set; } = 25;
    public List<ScreenWidget> Widgets { get; set; } = [];

    public static ScreenDesign CreateDefault() => new()
    {
        Widgets = [
            new() { Name="Titel", Format="QSCREEN //", Y=50, FontSize=26 },
            new() { Name="FPS", Kind=WidgetKind.Fps, Format="{value}", X=76,Y=120,Width=260,Height=100,FontSize=78 },
            new() { Name="FPS label", Format="FPS", Y=215,FontSize=20,Foreground=Color.Silver },
            new() { Name="CPU",Kind=WidgetKind.CpuTemperature,Format="CPU {value} °C",X=46,Y=285,Width=160,FontSize=22 },
            new() { Name="GPU",Kind=WidgetKind.GpuTemperature,Format="GPU {value} °C",X=206,Y=285,Width=160,FontSize=22 },
            new() { Name="Klok",Kind=WidgetKind.Time,Format="HH:mm:ss",Y=345,FontSize=21 }
        ]
    };
}

sealed record ScreenReadings(float? Cpu, float? Gpu, float? Fps, float? CpuLoad, float? GpuLoad, float? MemoryLoad, DateTime Now)
{
    public static ScreenReadings Demo => new(42, 58, 165, 35, 76, 52, DateTime.Now);
    public float? Value(WidgetKind kind) => kind switch
    {
        WidgetKind.CpuTemperature => Cpu, WidgetKind.GpuTemperature => Gpu, WidgetKind.Fps => Fps,
        WidgetKind.CpuLoad => CpuLoad, WidgetKind.GpuLoad => GpuLoad, WidgetKind.MemoryLoad => MemoryLoad, _ => null
    };
}

static class DesignFiles
{
    public static readonly JsonSerializerOptions Json = new() { WriteIndented = true, Converters = { new JsonStringEnumConverter() } };
    public static ScreenDesign Parse(string json)
    {
        if (json.Length > 8_000_000) throw new InvalidDataException("Het ontwerpbestand is te groot (maximaal 8 MB).");
        var design = JsonSerializer.Deserialize<ScreenDesign>(json, Json) ?? throw new InvalidDataException("Leeg ontwerp.");
        if (design.Version != 1 || design.Widgets == null || design.Widgets.Count > 64 || design.Widgets.Any(w => w == null))
            throw new InvalidDataException("Ongeldig ontwerp of meer dan 64 elementen.");
        design.Name = (design.Name ?? "Mijn Qscreen")[..Math.Min(120, (design.Name ?? "Mijn Qscreen").Length)];
        if (!Enum.IsDefined(design.Fit)) throw new InvalidDataException("Onbekende achtergrondmodus.");
        design.Darken = Math.Clamp(design.Darken, 0, 100);
        foreach (var w in design.Widgets)
        {
            if (!Enum.IsDefined(w.Kind) || !Enum.IsDefined(w.Alignment) || !Enum.IsDefined(w.BarSource)) throw new InvalidDataException("Onbekend elementtype.");
            w.X = Math.Clamp(w.X, -412, 412); w.Y = Math.Clamp(w.Y, -412, 412);
            w.Width = Math.Clamp(w.Width, 1, 824); w.Height = Math.Clamp(w.Height, 1, 824);
            w.FontSize = float.IsFinite(w.FontSize) ? Math.Clamp(w.FontSize, 6, 160) : 32;
            w.Maximum = float.IsFinite(w.Maximum) ? Math.Clamp(w.Maximum, 1, 10000) : 100;
            w.Format = (w.Format ?? "")[..Math.Min(512, (w.Format ?? "").Length)];
            w.Name = (w.Name ?? "Element")[..Math.Min(120, (w.Name ?? "Element").Length)];
            w.FontFamily = (w.FontFamily ?? "Segoe UI")[..Math.Min(120, (w.FontFamily ?? "Segoe UI").Length)];
        }
        if (design.BackgroundPng is { Length: > 0 })
        {
            try
            {
                using var stream = new MemoryStream(Convert.FromBase64String(design.BackgroundPng));
                using var image = Image.FromStream(stream);
                if (image.Width > 2048 || image.Height > 2048) throw new InvalidDataException("Achtergrond is te groot.");
            }
            catch (Exception ex) when (ex is FormatException or ArgumentException or OutOfMemoryException)
            { throw new InvalidDataException("Ongeldige achtergrondafbeelding.", ex); }
        }
        return design;
    }
    public static ScreenDesign Load(string path)
    {
        if (new FileInfo(path).Length > 8_000_000) throw new InvalidDataException("Ontwerpbestand groter dan 8 MB.");
        return Parse(File.ReadAllText(path));
    }
    public static string Serialize(ScreenDesign design) => JsonSerializer.Serialize(design, Json);
    public static void Save(string path, ScreenDesign design)
    {
        string json = Serialize(Parse(Serialize(design)));
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try { File.WriteAllText(temporary, json); File.Move(temporary, path, true); }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
    public static void Apply(string directory, ScreenDesign design)
    {
        Save(Path.Combine(directory, "custom-design.json"), design);
        File.WriteAllText(Path.Combine(directory, "custom-active.txt"), "1");
    }
}

sealed class DesignRenderer : IDisposable
{
    Image? background;
    string? cachedBackground;
    public Bitmap Render(ScreenDesign design, ScreenReadings readings)
    {
        var bitmap = new Bitmap(412, 412, PixelFormat.Format24bppRgb);
        using var g = Graphics.FromImage(bitmap);
        g.Clear(Color.FromArgb(design.BackgroundArgb));
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
        if (cachedBackground != design.BackgroundPng)
        {
            background?.Dispose(); background = null; cachedBackground = design.BackgroundPng;
            if (!string.IsNullOrEmpty(cachedBackground))
            {
                using var stream = new MemoryStream(Convert.FromBase64String(cachedBackground));
                using var image = Image.FromStream(stream);
                background = new Bitmap(image);
            }
        }
        using var circle = new GraphicsPath(); circle.AddEllipse(0, 0, 412, 412); g.SetClip(circle);
        if (background != null)
        {
            float scale = design.Fit == BackgroundFit.Cover ? Math.Max(412f / background.Width, 412f / background.Height) : Math.Min(412f / background.Width, 412f / background.Height);
            float width = design.Fit == BackgroundFit.Stretch ? 412 : background.Width * scale;
            float height = design.Fit == BackgroundFit.Stretch ? 412 : background.Height * scale;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.DrawImage(background, (412 - width) / 2, (412 - height) / 2, width, height);
        }
        using (var shade = new SolidBrush(Color.FromArgb((int)(Math.Clamp(design.Darken, 0, 100) * 2.55), Color.Black))) g.FillRectangle(shade, 0, 0, 412, 412);
        foreach (var w in design.Widgets.Where(w => w.Visible))
        {
            var bounds = new RectangleF(w.X, w.Y, Math.Clamp(w.Width, 1, 824), Math.Clamp(w.Height, 1, 824));
            var value = readings.Value(w.Kind);
            var color = w.TemperatureColors && w.Kind is WidgetKind.CpuTemperature or WidgetKind.GpuTemperature
                ? value is null ? Color.Gray : value > 50 ? Color.FromArgb(255,70,60) : Color.FromArgb(60,190,255) : w.Foreground;
            using var brush = new SolidBrush(color);
            using var fill = new SolidBrush(w.Fill);
            if (w.Kind == WidgetKind.Rectangle) { g.FillRectangle(brush, bounds); continue; }
            if (w.Kind == WidgetKind.Ellipse) { g.FillEllipse(brush, bounds); continue; }
            if (w.Kind == WidgetKind.ProgressBar)
            {
                g.FillRectangle(fill, bounds);
                float fraction = Math.Clamp((readings.Value(w.BarSource) ?? 0) / Math.Max(1, w.Maximum), 0, 1);
                g.FillRectangle(brush, bounds.X, bounds.Y, bounds.Width * fraction, bounds.Height); continue;
            }
            string text;
            try
            {
                text = w.Kind is WidgetKind.Time or WidgetKind.Date ? readings.Now.ToString(w.Format, CultureInfo.CurrentCulture)
                    : w.Kind == WidgetKind.Text ? w.Format : w.Format.Replace("{value}", value?.ToString(w.Kind is WidgetKind.CpuTemperature or WidgetKind.GpuTemperature ? "0.0" : "0", CultureInfo.CurrentCulture) ?? "--");
            }
            catch (FormatException) { text = "Ongeldige opmaak"; }
            var style = (w.Bold ? FontStyle.Bold : FontStyle.Regular) | (w.Italic ? FontStyle.Italic : FontStyle.Regular);
            using var font = CreateFont(w.FontFamily, Math.Clamp(w.FontSize, 6, 160), style);
            using var format = new StringFormat { Alignment = w.Alignment, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter };
            g.DrawString(text, font, brush, bounds, format);
        }
        g.ResetClip();
        // Pixels outside the physical circular display are black, including in previews.
        using var outside = new Region(new Rectangle(0,0,412,412)); outside.Exclude(circle); g.FillRegion(Brushes.Black, outside);
        return bitmap;
    }
    static Font CreateFont(string family, float size, FontStyle style)
    {
        try { return new Font(family, size, style, GraphicsUnit.Pixel); }
        catch (ArgumentException) { return new Font("Segoe UI", size, FontStyle.Regular, GraphicsUnit.Pixel); }
    }
    public static byte[] Rgb565(Bitmap image)
    {
        if (image.Width != 412 || image.Height != 412) throw new ArgumentException("Expected a 412x412 frame.");
        var result = new byte[412 * 412 * 2];
        var data = image.LockBits(new Rectangle(0,0,412,412), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
        try
        {
            var row = new byte[412 * 3];
            for (int y=0;y<412;y++)
            {
                Marshal.Copy(data.Scan0 + y * data.Stride, row, 0, row.Length);
                for (int x=0;x<412;x++)
                {
                    int pixel = ((row[x*3+2] & 248) << 8) | ((row[x*3+1] & 252) << 3) | (row[x*3] >> 3);
                    int index = (y*412+x)*2; result[index]=(byte)pixel; result[index+1]=(byte)(pixel>>8);
                }
            }
        }
        finally { image.UnlockBits(data); }
        return result;
    }
    public void Dispose() => background?.Dispose();
}

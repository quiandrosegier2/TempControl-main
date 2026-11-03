using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;

// The monitor owns the USB port; this window only changes its configuration.
sealed class SettingsWindow : Form
{
    [DllImport("dwmapi.dll")]
    static extern int DwmSetWindowAttribute(IntPtr window, int attribute, ref int value, int size);
    readonly ComboBox layouts = new() { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Top };
    readonly PictureBox preview = new() { SizeMode = PictureBoxSizeMode.Zoom, Dock = DockStyle.Fill };
    readonly Label status = new() { AutoSize = false, Dock = DockStyle.Bottom, Height = 64 };
    readonly Button play = new() { Text = "Pauzeren", AutoSize = true };
    readonly System.Windows.Forms.Timer timer = new() { Interval = 100 };
    int frame;
    bool paused;
    readonly string appDirectory;
    readonly DesignRenderer ownPreviewRenderer = new();
    ScreenDesign? ownPreview;
    bool showingCustom;
    string Selected => layouts.SelectedItem as string ?? "classic";
    bool Animated => layouts.SelectedIndex >= 3 && layouts.SelectedIndex < 26;

    public SettingsWindow(string? directory = null)
    {
        appDirectory = directory ?? AppContext.BaseDirectory;
        Text = "Qscreen — Layout Studio";
        Size = new Size(1000, 760); MinimumSize = new Size(850, 700);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(24, 25, 31); ForeColor = Color.White;
        Font = new Font("Segoe UI", 11);
        layouts.BackColor = Color.FromArgb(40, 42, 51); layouts.ForeColor = Color.White;
        layouts.DrawMode = DrawMode.OwnerDrawFixed; layouts.ItemHeight = 26;
        layouts.DrawItem += (_, e) =>
        {
            if (e.Index < 0) return;
            e.DrawBackground();
            string name = System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(LayoutSettings.Names[e.Index].Replace('-', ' '));
            name += e.Index >= 3 && e.Index < 26 ? " · bewegend" : " · statisch";
            TextRenderer.DrawText(e.Graphics, name, e.Font, e.Bounds, Color.White, TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
            e.DrawFocusRectangle();
        };
        string icon = Path.Combine(appDirectory, "branding", "qprints.ico");
        if (File.Exists(icon)) Icon = new Icon(icon);
        var left = new Panel { Dock = DockStyle.Left, Width = 310, Padding = new Padding(20) };
        var logo = new PictureBox { Dock = DockStyle.Top, Height = 80, SizeMode = PictureBoxSizeMode.Zoom };
        string logoPath = Path.Combine(appDirectory, "branding", "qprints-wordmark.png");
        if (File.Exists(logoPath)) logo.Image = ReadImage(logoPath);
        var title = new Label { Text = "LAYOUT STUDIO\n31 layouts · 23 bewegend", Dock = DockStyle.Top, Height = 70 };
        var help = new Label { Text = "Kies een thema of maak je eigen ontwerp met een eigen achtergrond, lettertypen, kleuren, datum en klok.\n\nEigen ontwerpen vereisen firmware 1.5.0 en een actieve USB-verbinding.", Dock = DockStyle.Top, Height = 170 };
        var actions = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false };
        Button Action(string label, Action handler)
        {
            var button = new Button { Text = label, Width = 260, Height = 42, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(40, 42, 51), ForeColor = Color.White };
            button.Click += (_, _) => { try { handler(); } catch (Exception ex) { status.Text = ex.Message; } };
            actions.Controls.Add(button); return button;
        }
        Action("Layout toepassen", Apply).BackColor = Color.FromArgb(165, 35, 43);
        Action("Eigen ontwerp maken…", () => { using var designer = new DesignWindow(appDirectory); designer.ShowDialog(this); LoadCustomPreview(); });
        Action("Firmware installeren", () => LaunchPowerShell("-NoProfile -STA -ExecutionPolicy Bypass -WindowStyle Hidden -File \"" + Path.Combine(AppContext.BaseDirectory, "flash-firmware.ps1") + "\""));
        Action("Achtergrondmeting starten", () => LaunchPowerShell("-NoProfile -Command \"Start-ScheduledTask -TaskName TempMonitor\""));
        Action("Logboek openen", () => Process.Start(new ProcessStartInfo("explorer.exe", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "TempMonitor")) { UseShellExecute = true }));
        left.Controls.Add(actions); left.Controls.Add(help); left.Controls.Add(title); left.Controls.Add(logo);
        var right = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20) };
        var footer = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 45 };
        footer.Controls.Add(play);
        right.Controls.Add(preview); right.Controls.Add(footer); right.Controls.Add(status); right.Controls.Add(layouts);
        Controls.Add(right); Controls.Add(left);
        layouts.Items.AddRange(LayoutSettings.Names);
        layouts.SelectedIndexChanged += (_, _) => { showingCustom = false; frame = 0; paused = false; play.Enabled = Animated; play.Text = Animated ? "Pauzeren" : "Statisch"; status.Text = $"Voorbeeld: {Selected} · testwaarden"; timer.Start(); ShowFrame(); };
        play.Click += (_, _) => { paused = !paused; play.Text = paused ? "Afspelen" : "Pauzeren"; };
        layouts.SelectedItem = LayoutSettings.Read(appDirectory);
        timer.Tick += (_, _) => { if (showingCustom || (Animated && !paused)) { frame = (frame + 1) % 40; ShowFrame(); } };
        timer.Start();
        LoadCustomPreview();
        FormClosed += (_, _) => { timer.Stop(); timer.Dispose(); ownPreviewRenderer.Dispose(); preview.Image?.Dispose(); logo.Image?.Dispose(); Icon?.Dispose(); };
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        int dark = 1;
        DwmSetWindowAttribute(Handle, 20, ref dark, sizeof(int));
    }

    static Image ReadImage(string path)
    {
        using var source = Image.FromFile(path);
        return new Bitmap(source);
    }

    void ShowFrame()
    {
        if (showingCustom && ownPreview != null)
        {
            var next = ownPreviewRenderer.Render(ownPreview, ScreenReadings.Demo);
            var previous = preview.Image; preview.Image = next; previous?.Dispose(); return;
        }
        string name = Animated ? $"{Selected}-{frame:D2}.bmp" : $"layout-{Selected}.png";
        string path = Path.Combine(appDirectory, "previews", name);
        try
        {
            var next = ReadImage(path);
            var previous = preview.Image; preview.Image = next; previous?.Dispose();
        }
        catch (Exception ex) when (ex is IOException or ArgumentException or OutOfMemoryException)
        {
            timer.Stop(); status.Text = "Voorbeeld ontbreekt of kan niet worden geladen. Installeer Qscreen opnieuw.";
        }
    }

    void LoadCustomPreview()
    {
        if (!File.Exists(Path.Combine(appDirectory, "custom-active.txt"))) return;
        try
        {
            ownPreview = DesignFiles.Load(Path.Combine(appDirectory, "custom-design.json")); showingCustom = true;
            play.Enabled = false; play.Text = "Eigen ontwerp";
            status.Text = $"Eigen ontwerp actief: {ownPreview.Name}. Voorbeeld met testwaarden; open de ontwerper om aan te passen.";
            ShowFrame(); timer.Start();
        }
        catch (Exception ex) { status.Text = "Eigen ontwerp kon niet worden geopend: " + ex.Message; }
    }

    void Apply()
    {
        string path = Path.Combine(appDirectory, "layout.txt");
        string temporary = path + ".new";
        File.WriteAllText(temporary, Selected + Environment.NewLine);
        File.Move(temporary, path, true);
        File.Delete(Path.Combine(appDirectory, "custom-active.txt"));
        showingCustom = false;
        play.Enabled = Animated;
        play.Text = Animated ? "Pauzeren" : "Statisch";
        ShowFrame();
        status.Text = $"Opgeslagen: {Selected}. De achtergrondmeting past dit binnen een seconde toe zodra het scherm verbonden is.";
    }

    static void LaunchPowerShell(string arguments) => Process.Start(new ProcessStartInfo("powershell.exe", arguments)
    {
        UseShellExecute = true, WindowStyle = ProcessWindowStyle.Hidden, WorkingDirectory = AppContext.BaseDirectory
    });
}

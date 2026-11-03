using System.ComponentModel;
using System.Drawing;
using System.Text.Json;
using System.Windows.Forms;

sealed class ColorRulesWindow : Form
{
    public List<WidgetColorRule> Rules { get; }
    public ColorRulesWindow(ScreenWidget widget)
    {
        Rules=JsonSerializer.Deserialize<List<WidgetColorRule>>(JsonSerializer.Serialize(widget.ColorRules,DesignFiles.Json),DesignFiles.Json)!;
        Text="Qscreen · Kleurregels";Size=new(750,490);BackColor=StudioTheme.Background;ForeColor=StudioTheme.Text;StartPosition=FormStartPosition.CenterParent;
        var binding=new BindingList<WidgetColorRule>(Rules);var grid=AdvancedUi.Grid(binding);var inspector=AdvancedUi.Inspector(null);inspector.Dock=DockStyle.Right;inspector.Width=300;grid.SelectionChanged+=(_,_)=>inspector.SelectedObject=grid.CurrentRow?.DataBoundItem;
        var note=StudioTheme.Label("De hoogste Minimum-drempel die bereikt is bepaalt de kleur. Ontbrekende waarden gebruiken de gewone elementkleur. De regels gelden voor tekst, balken en ronde meters.",10,false,76);
        var bar=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=100};AdvancedUi.Button(bar,"+ Regel",()=>binding.Add(new()));AdvancedUi.Button(bar,"Verwijderen",()=>{if(grid.CurrentRow?.DataBoundItem is WidgetColorRule r)binding.Remove(r);});
        AdvancedUi.Button(bar,"CPU voorbeeld",()=>{binding.Clear();binding.Add(new(){Minimum=-100,Color=Color.DeepSkyBlue});binding.Add(new(){Minimum=60,Color=Color.Orange});binding.Add(new(){Minimum=80,Color=Color.Red,Pulse=true});});
        AdvancedUi.Button(bar,"Bewaren",()=>{grid.EndEdit();if(Rules.Count>16||Rules.Any(r=>!float.IsFinite(r.Minimum)))throw new InvalidDataException("Maximaal 16 geldige kleurregels.");DialogResult=DialogResult.OK;Close();},primary:true);
        Controls.Add(grid);Controls.Add(inspector);Controls.Add(note);Controls.Add(bar);
    }
    protected override void OnHandleCreated(EventArgs e){base.OnHandleCreated(e);StudioTheme.DarkTitle(this);}
}
sealed class WeatherSettingsWindow : Form
{
    public WeatherSettingsWindow(string directory)
    {
        string path=Path.Combine(directory,"weather-settings.json");var settings=File.Exists(path)?JsonSerializer.Deserialize<WeatherSettings>(File.ReadAllText(path),DesignFiles.Json)??new():new();
        Text="Qscreen · Weerprovider";Size=new(620,490);StartPosition=FormStartPosition.CenterParent;BackColor=StudioTheme.Background;ForeColor=StudioTheme.Text;
        var note=StudioTheme.Label("OPEN-METEO\nSchakel de provider bewust in en vul je locatiecoördinaten in. De app vraagt iedere 10 minuten weergegevens op; coördinaten worden naar Open-Meteo gestuurd. Zonder verbinding of configuratie verschijnt --. Open-Meteo / CC BY 4.0. Controleer hun voorwaarden voor commercieel gebruik.",11,false,155);
        var inspector=AdvancedUi.Inspector(settings);var actions=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=60};
        AdvancedUi.Button(actions,"Bewaren",()=>{settings.Validate();AtomicJson.Write(path,settings);DialogResult=DialogResult.OK;Close();},primary:true);
        var link=new LinkLabel{Text="Open-Meteo documentatie en voorwaarden",AutoSize=true,LinkColor=Color.DeepSkyBlue};link.LinkClicked+=(_,_)=>System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("https://open-meteo.com/en/docs"){UseShellExecute=true});actions.Controls.Add(link);
        Controls.Add(inspector);Controls.Add(note);Controls.Add(actions);
    }
    protected override void OnHandleCreated(EventArgs e){base.OnHandleCreated(e);StudioTheme.DarkTitle(this);}
}

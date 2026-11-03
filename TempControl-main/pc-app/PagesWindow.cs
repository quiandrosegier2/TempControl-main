using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

sealed class PagesWindow : Form
{
    public ScreenDesign Project { get; private set; }
    readonly ListView pages=new(){Dock=DockStyle.Fill,View=View.LargeIcon,MultiSelect=false,BackColor=StudioTheme.Surface,ForeColor=StudioTheme.Text,HideSelection=false};
    readonly ImageList thumbnails=new(){ImageSize=new Size(96,96),ColorDepth=ColorDepth.Depth32Bit};
    readonly List<Bitmap> thumbnailImages=[];
    readonly string directory;
    readonly TextBox pageName=new(){Width=220};
    readonly Label status=new(){Dock=DockStyle.Bottom,Height=48};
    int Selected=>pages.SelectedIndices.Count==0?0:pages.SelectedIndices[0];
    public PagesWindow(string directory,ScreenDesign project)
    {
        this.directory=directory;Project=DesignSnapshot.Capture(project).Restore();Text="Qscreen · Pagina's";Size=new(900,680);MinimumSize=new(700,540);StartPosition=FormStartPosition.CenterParent;BackColor=StudioTheme.Background;ForeColor=StudioTheme.Text;Font=new("Segoe UI",10);
        var tools=new FlowLayoutPanel{Dock=DockStyle.Top,Height=104,Padding=new(12)};
        void Button(string text,Action action){var button=StudioTheme.Button(text);button.Width=130;button.Click+=(_,_)=>Safe(action);tools.Controls.Add(button);}
        Button("+ Pagina",()=>{var page=ScreenDesign.CreateDefault();page.Name="Pagina "+(DisplayPages.Count(Project)+1);Project.Pages.Add(DisplayPage.From(page));RefreshPages(DisplayPages.Count(Project)-1);});
        Button("Dupliceren",()=>{var copy=DesignSnapshot.Capture(DisplayPages.At(Project,Selected)).Restore();copy.Name+=" kopie";Project.Pages.Add(DisplayPage.From(copy));RefreshPages(DisplayPages.Count(Project)-1);});
        Button("Bewerken",Edit);Button("Verwijderen",()=>{int index=Selected;DisplayPages.Remove(Project,index);RefreshPages(Math.Max(0,index-1));});
        Button("Naar links",()=>{int i=Selected;DisplayPages.Reorder(Project,i,i-1);RefreshPages(Math.Max(0,i-1));});Button("Naar rechts",()=>{int i=Selected;DisplayPages.Reorder(Project,i,i+1);RefreshPages(Math.Min(DisplayPages.Count(Project)-1,i+1));});
        StudioTheme.Input(pageName);tools.Controls.Add(pageName);Button("Hernoemen",()=>{int index=Selected;var page=DisplayPages.At(Project,index);page.Name=pageName.Text;DisplayPages.Replace(Project,index,page);RefreshPages(index);});
        var bottom=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=100,Padding=new(12)};
        var rotate=new CheckBox{Text="Automatisch roteren",Checked=Project.RotatePages,AutoSize=true};var seconds=new NumericUpDown{Minimum=2,Maximum=3600,Value=Project.RotationSeconds,Width=80};
        var transitions=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList,Width=150};transitions.Items.AddRange(Enum.GetNames<PageTransition>());transitions.SelectedItem=Project.Transition.ToString();StudioTheme.Input(transitions);
        var done=StudioTheme.Button("Pagina's bewaren",true);done.Click+=(_,_)=>{Project.RotatePages=rotate.Checked;Project.RotationSeconds=(int)seconds.Value;Project.Transition=Enum.Parse<PageTransition>((string)transitions.SelectedItem!);DialogResult=DialogResult.OK;Close();};
        bottom.Controls.AddRange([rotate,seconds,new Label{Text="seconden",AutoSize=true},transitions,done,new Label{Text="Cut = direct. Fade/Slide/Zoom gebruiken één tussenbeeld; snelheid hangt af van USB.",AutoSize=true}]);
        pages.LargeImageList=thumbnails;pages.SelectedIndexChanged+=(_,_)=>pageName.Text=DisplayPages.At(Project,Selected).Name;pages.DoubleClick+=(_,_)=>Safe(Edit);
        Controls.Add(pages);Controls.Add(status);Controls.Add(bottom);Controls.Add(tools);RefreshPages(0);StudioTheme.FitWindow(this,new(900,680),new(700,540));
    }
    void Safe(Action action){try{action();}catch(Exception ex){status.Text=ex.Message;}}
    void Edit(){int index=Selected;var copy=DisplayPage.From(DisplayPages.At(Project,index)).Design();using var editor=new DesignWindow(directory,()=>{},copy,saved=>DisplayPages.Replace(Project,index,saved));editor.ShowDialog(this);RefreshPages(index);}
    void RefreshPages(int selected)
    {
        pages.BeginUpdate();pages.Items.Clear();thumbnails.Images.Clear();foreach(var old in thumbnailImages)old.Dispose();thumbnailImages.Clear();using var renderer=new DesignRenderer();
        for(int i=0;i<DisplayPages.Count(Project);i++){var page=DisplayPages.At(Project,i);using var image=renderer.Render(page,ScreenReadings.Demo);var thumb=new Bitmap(image,new Size(96,96));thumbnailImages.Add(thumb);thumbnails.Images.Add(thumb);pages.Items.Add(new ListViewItem($"{i+1}. {page.Name}",i));}
        pages.EndUpdate();if(pages.Items.Count>0){pages.Items[Math.Clamp(selected,0,pages.Items.Count-1)].Selected=true;pages.Select();}
    }
    protected override void OnHandleCreated(EventArgs e){base.OnHandleCreated(e);StudioTheme.DarkTitle(this);}
    protected override void Dispose(bool disposing){if(disposing){thumbnails.Dispose();foreach(var image in thumbnailImages)image.Dispose();thumbnailImages.Clear();}base.Dispose(disposing);}
}
sealed class TouchZonesWindow : Form
{
    public List<TouchZone> Zones { get; private set; }
    public TouchZonesWindow(List<TouchZone> zones)
    {
        Zones=System.Text.Json.JsonSerializer.Deserialize<List<TouchZone>>(System.Text.Json.JsonSerializer.Serialize(zones,DesignFiles.Json),DesignFiles.Json)!;
        Text="Qscreen · Aanraakzones";Size=new(980,540);BackColor=StudioTheme.Background;ForeColor=StudioTheme.Text;StartPosition=FormStartPosition.CenterParent;
        var binding=new BindingList<TouchZone>(Zones);var grid=AdvancedUi.Grid(binding);var note=StudioTheme.Label("Zones gebruiken canvascoördinaten (412×412). Pagina 0 = de eerste pagina. Zichtbaar toont de knop op het scherm.",10,false,60);
        var actions=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=58};AdvancedUi.Button(actions,"+ Zone",()=>binding.Add(new()));AdvancedUi.Button(actions,"Verwijder zone",()=>{if(grid.CurrentRow?.DataBoundItem is TouchZone zone)binding.Remove(zone);});
        var save=StudioTheme.Button("Zones bewaren",true);save.Click+=(_,_)=>{grid.EndEdit();DialogResult=DialogResult.OK;Close();};actions.Controls.Add(save);Controls.Add(grid);Controls.Add(actions);Controls.Add(note);
    }
    protected override void OnHandleCreated(EventArgs e){base.OnHandleCreated(e);StudioTheme.DarkTitle(this);}
}

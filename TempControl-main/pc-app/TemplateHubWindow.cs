using System.Drawing;
using System.Windows.Forms;

sealed class TemplateHubWindow : Form
{
    readonly string directory;
    readonly LocalTemplateLibrary library;
    readonly ListBox list=new(){Dock=DockStyle.Left,Width=280,BackColor=StudioTheme.Surface,ForeColor=StudioTheme.Text};
    readonly PictureBox preview=new StudioPreview{Dock=DockStyle.Fill};
    readonly Label status=StudioTheme.Label("Lokale bibliotheek · online communitybackend niet geconfigureerd",10,false,64);
    readonly TextBox search=new(){Width=240};
    readonly ComboBox category=new(){DropDownStyle=ComboBoxStyle.DropDownList,Width=170};
    readonly ComboBox sorting=new(){DropDownStyle=ComboBoxStyle.DropDownList,Width=130};
    IReadOnlyList<DesignTemplate> items=[];
    DesignTemplate? fullTemplate;
    readonly CancellationTokenSource closing=new();
    int searchGeneration;
    public TemplateHubWindow(string directory)
    {
        this.directory=directory;library=new(directory);Text="Qscreen · Lokale Design Hub";Size=new(1020,760);MinimumSize=new(820,620);StartPosition=FormStartPosition.CenterParent;BackColor=StudioTheme.Background;ForeColor=StudioTheme.Text;Font=new("Segoe UI",10);
        var toolbar=new FlowLayoutPanel{Dock=DockStyle.Top,Height=60,Padding=new(12)};StudioTheme.Input(search);StudioTheme.Input(category);StudioTheme.Input(sorting);search.PlaceholderText="Zoek ontwerpen…";
        category.Items.Add("Alle categorieën");category.Items.AddRange(Enum.GetNames<TemplateCategory>());category.SelectedIndex=0;sorting.Items.AddRange(["Nieuwste","Naam","Categorie"]);sorting.SelectedIndex=0;
        toolbar.Controls.AddRange([search,category,sorting]);search.TextChanged+=(_,_)=>RefreshLibrary();category.SelectedIndexChanged+=(_,_)=>RefreshLibrary();sorting.SelectedIndexChanged+=(_,_)=>RefreshLibrary();
        var actions=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=100,Padding=new(12)};
        AdvancedUi.Button(actions,"Pakket importeren",ImportPackage);AdvancedUi.Button(actions,"Huidig ontwerp bewaren",AddCurrent);AdvancedUi.Button(actions,"Als nieuw profiel",ImportProfile,primary:true);AdvancedUi.Button(actions,"Pakket exporteren",ExportPackage);
        list.DisplayMember="Name";list.SelectedIndexChanged+=(_,_)=>Preview();Controls.Add(preview);Controls.Add(list);Controls.Add(status);Controls.Add(actions);Controls.Add(toolbar);status.Dock=DockStyle.Bottom;
        library.EnsureExamples();RefreshLibrary();StudioTheme.FitWindow(this,new(1020,760),new(820,620));
    }
    async void RefreshLibrary()
    {
        try
        {
            TemplateCategory? selected=category.SelectedIndex<=0?null:Enum.Parse<TemplateCategory>((string)category.SelectedItem!);
            int generation=++searchGeneration;var found=await library.SearchAsync(search.Text,selected,closing.Token);
            if(IsDisposed||generation!=searchGeneration)return;
            items=(sorting.SelectedIndex switch{1=>found.OrderBy(t=>t.Manifest.Name),2=>found.OrderBy(t=>t.Manifest.Category),_=>found.OrderByDescending(t=>t.Manifest.CreatedUtc)}).ToList();
            list.Items.Clear();foreach(var item in items)list.Items.Add(item.Manifest.Name);if(list.Items.Count>0)list.SelectedIndex=0;
        }
        catch(OperationCanceledException){ }
        catch(Exception ex){if(!IsDisposed)status.Text=ex.Message;}
    }
    DesignTemplate Selected()=>list.SelectedIndex>=0?items[list.SelectedIndex]:throw new InvalidOperationException("Kies eerst een ontwerp.");
    async void Preview()
    {
        if(list.SelectedIndex<0)return;try{string id=Selected().Id;fullTemplate=null;var item=await library.DownloadAsync(id,closing.Token);if(IsDisposed||list.SelectedIndex<0||Selected().Id!=id)return;fullTemplate=item;using var renderer=new DesignRenderer();var image=renderer.Render(item.Design,ScreenReadings.Demo);var old=preview.Image;preview.Image=image;old?.Dispose();status.Text=$"{item.Manifest.Name} · {item.Manifest.Category} · {item.Manifest.Author}\nWS146 · minimaal Qscreen {item.Manifest.MinimumAppVersion}. Voorbeeld vóór import; je huidige project wordt niet overschreven.";}catch(OperationCanceledException){ }catch(Exception ex){if(!IsDisposed)status.Text=ex.Message;}
    }
    DesignTemplate SelectedFull()=>fullTemplate is {} item&&item.Id==Selected().Id?item:throw new InvalidOperationException("Wacht tot het voorbeeld geladen is.");
    void ImportPackage(){using var dialog=new OpenFileDialog{Filter="Qscreen pakket|*.qscreenpack"};if(dialog.ShowDialog(this)!=DialogResult.OK)return;var item=QscreenPackage.Import(dialog.FileName);using var preview=new DesignPreviewWindow(item.Design,$"{item.Manifest.Category} · {DisplayPages.Count(item.Design)} pagina's · WS146");if(preview.ShowDialog(this)!=DialogResult.OK)return;library.Add(item.Design,item.Manifest.Category,item.Manifest);RefreshLibrary();status.Text="Pakket gevalideerd en aan de lokale bibliotheek toegevoegd.";}
    void AddCurrent(){string path=Path.Combine(directory,"custom-draft.json");if(!File.Exists(path))path=Path.Combine(directory,"custom-design.json");library.Add(DesignFiles.Load(path),category.SelectedIndex>0?Enum.Parse<TemplateCategory>((string)category.SelectedItem!):TemplateCategory.Gaming);RefreshLibrary();}
    void ImportProfile(){var item=SelectedFull();var profiles=new ProfileRepository(directory);var design=DesignSnapshot.Capture(item.Design).Restore();design.Name=item.Manifest.Name;profiles.Add(design);status.Text="Nieuw profiel aangemaakt. Het huidige ontwerp is behouden. Activeer het via Profielen.";}
    void ExportPackage(){var item=SelectedFull();using var dialog=new SaveFileDialog{Filter="Qscreen pakket|*.qscreenpack",FileName="Qscreen-ontwerp.qscreenpack"};if(dialog.ShowDialog(this)==DialogResult.OK)QscreenPackage.Export(dialog.FileName,item.Design,item.Manifest);}
    protected override void OnHandleCreated(EventArgs e){base.OnHandleCreated(e);StudioTheme.DarkTitle(this);}
    protected override void Dispose(bool disposing){if(disposing){closing.Cancel();preview.Image?.Dispose();}base.Dispose(disposing);}
}

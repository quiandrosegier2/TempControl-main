using System.Drawing;
using System.Windows.Forms;

sealed class MotionLibraryWindow : Form
{
    readonly string root=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"QPRINTS","Qscreen","Animations");
    readonly ComboBox folders=new(){Dock=DockStyle.Top,DropDownStyle=ComboBoxStyle.DropDownList};
    readonly ListBox assets=new(){Dock=DockStyle.Fill,BackColor=StudioTheme.Surface,ForeColor=Color.White};
    readonly TextBox name=new(){Width=230};
    readonly Label status=new(){Dock=DockStyle.Bottom,Height=40,ForeColor=Color.Silver};
    public string? SelectedPath {get;private set;}
    string Folder=>folders.SelectedIndex<=0?root:Path.Combine(root,(string)folders.SelectedItem!);
    string? Selected=>assets.SelectedItem is string value?Path.Combine(Folder,value):null;
    public MotionLibraryWindow()
    {
        Directory.CreateDirectory(root);Text="Qscreen / Mijn animaties";Size=new(780,570);StartPosition=FormStartPosition.CenterParent;BackColor=StudioTheme.Background;ForeColor=Color.White;Font=new("Segoe UI",10);
        var tools=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=110,Padding=new(12)};tools.Controls.Add(name);
        void Button(string text,Action action){var b=StudioTheme.Button(text);b.AutoSize=true;b.Click+=(_,_)=>{try{action();}catch(Exception ex)when(ex is IOException or ArgumentException or UnauthorizedAccessException){status.Text=ex.Message;}};tools.Controls.Add(b);}
        Button("Hernoemen",()=>{if(Selected is {} path){string target=Path.Combine(Folder,SafeName()+Path.GetExtension(path));File.Move(path,target,false);LoadAssets();}});
        Button("Nieuwe map",()=>{Directory.CreateDirectory(Path.Combine(root,SafeName()));LoadFolders();});
        Button("Verplaats naar map",()=>{if(Selected is not {} path)return;using var pick=new FolderBrowserDialog{InitialDirectory=root,Description="Kies een map binnen Mijn animaties"};if(pick.ShowDialog(this)!=DialogResult.OK)return;string target=Path.GetFullPath(pick.SelectedPath);if(target!=root&&!target.StartsWith(root+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new IOException("Kies een map binnen je persoonlijke animatiebibliotheek.");File.Move(path,Path.Combine(target,Path.GetFileName(path)),false);LoadAssets();});
        Button("Verwijderen",()=>{if(Selected is not {} path)return;string trash=Path.Combine(root,".trash");Directory.CreateDirectory(trash);File.Move(path,Path.Combine(trash,Guid.NewGuid().ToString("N")+Path.GetExtension(path)));LoadAssets();});
        Button("Gebruik deze animatie",()=>{if(Selected is {} path){SelectedPath=path;DialogResult=DialogResult.OK;Close();}});
        assets.SelectedIndexChanged+=(_,_)=>{if(Selected is {} path){name.Text=Path.GetFileNameWithoutExtension(path);status.Text=$"{new FileInfo(path).Length/1024d:0.#} KB · persoonlijk asset";}};
        folders.SelectedIndexChanged+=(_,_)=>LoadAssets();Controls.Add(assets);Controls.Add(folders);Controls.Add(tools);Controls.Add(status);LoadFolders();
    }
    string SafeName(){string value=name.Text.Trim();if(value.Length is <1 or >80||value is "." or ".."||value.IndexOfAny(Path.GetInvalidFileNameChars())>=0||value.EndsWith('.')||value.EndsWith(' '))throw new IOException("Kies een geldige naam van maximaal 80 tekens.");return value;}
    void LoadFolders(){folders.Items.Clear();folders.Items.Add("Mijn animaties");foreach(var folder in Directory.GetDirectories(root).Where(p=>!Path.GetFileName(p).StartsWith('.')).Order())folders.Items.Add(Path.GetFileName(folder));folders.SelectedIndex=0;}
    void LoadAssets(){assets.Items.Clear();foreach(var path in Directory.GetFiles(Folder).Where(p=>Path.GetExtension(p).ToLowerInvariant() is ".gif" or ".png" or ".qanim").Order())assets.Items.Add(Path.GetFileName(path));}
}

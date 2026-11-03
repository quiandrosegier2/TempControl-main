using System.Drawing;
using System.Windows.Forms;

sealed class DesignPreviewWindow : Form
{
    readonly PictureBox preview=new StudioPreview{Dock=DockStyle.Fill};
    public DesignPreviewWindow(ScreenDesign design,string details)
    {
        Text="Qscreen · Voorbeeld vóór import";Size=new(620,700);BackColor=StudioTheme.Background;ForeColor=StudioTheme.Text;StartPosition=FormStartPosition.CenterParent;Font=new("Segoe UI",10);
        var title=StudioTheme.Label(design.Name+"\n"+details,14,true,90);title.Padding=new(18,6,18,0);
        using var renderer=new DesignRenderer();preview.Image=renderer.Render(design,ScreenReadings.Demo);
        var buttons=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=62,Padding=new(12)};var accept=StudioTheme.Button("Importeren",true);accept.Click+=(_,_)=>{DialogResult=DialogResult.OK;Close();};var cancel=StudioTheme.Button("Annuleren");cancel.Click+=(_,_)=>{DialogResult=DialogResult.Cancel;Close();};buttons.Controls.AddRange([accept,cancel]);Controls.Add(preview);Controls.Add(buttons);Controls.Add(title);StudioTheme.FitWindow(this,new(620,700),new(500,500));
    }
    protected override void OnHandleCreated(EventArgs e){base.OnHandleCreated(e);StudioTheme.DarkTitle(this);}
    protected override void Dispose(bool disposing){if(disposing)preview.Image?.Dispose();base.Dispose(disposing);}
}

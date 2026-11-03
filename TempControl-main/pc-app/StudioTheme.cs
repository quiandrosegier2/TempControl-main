using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.ComponentModel;

static class StudioTheme
{
    public static readonly Color Background=Color.FromArgb(13,16,24), Surface=Color.FromArgb(23,27,39), Raised=Color.FromArgb(31,36,51);
    public static readonly Color Border=Color.FromArgb(47,54,73), Text=Color.FromArgb(237,240,248), Muted=Color.FromArgb(145,157,183);
    public static readonly Color Accent=Color.FromArgb(239,91,113), Green=Color.FromArgb(90,218,173);
    [DllImport("dwmapi.dll")] static extern int DwmSetWindowAttribute(IntPtr window,int attribute,ref int value,int size);
    [DllImport("uxtheme.dll",CharSet=CharSet.Unicode)] static extern int SetWindowTheme(IntPtr window,string app,string? list);
    public static void DarkTitle(Form form){int value=1;DwmSetWindowAttribute(form.Handle,20,ref value,sizeof(int));}
    public static void FitWindow(Form form,Size preferred,Size minimum)
    {
        var area=Screen.FromControl(form).WorkingArea;
        form.MinimumSize=new Size(Math.Min(minimum.Width,area.Width),Math.Min(minimum.Height,area.Height));
        form.Size=new Size(Math.Min(preferred.Width,area.Width),Math.Min(preferred.Height,area.Height));
    }
    public static Label Label(string text,int size=10,bool bold=false,int height=28)=>new(){Text=text,ForeColor=bold?Text:Muted,Font=new Font("Segoe UI",size,bold?FontStyle.Bold:FontStyle.Regular),Height=height,Dock=DockStyle.Top,AutoEllipsis=true,TextAlign=ContentAlignment.MiddleLeft};
    public static void Input(Control control,bool drawCombo=true)
    {
        control.BackColor=Raised;control.ForeColor=Text;control.Font=new Font("Segoe UI",10);
        if(control is TextBox box)box.BorderStyle=BorderStyle.FixedSingle;
        if(control is ComboBox combo)
        {
            combo.FlatStyle=FlatStyle.Flat;
            if(drawCombo){combo.DrawMode=DrawMode.OwnerDrawFixed;combo.ItemHeight=26;combo.DrawItem+=(_,e)=>{if(e.Index<0)return;using var brush=new SolidBrush(Raised);e.Graphics.FillRectangle(brush,e.Bounds);TextRenderer.DrawText(e.Graphics,combo.GetItemText(combo.Items[e.Index]),e.Font,e.Bounds,Text,TextFormatFlags.VerticalCenter|TextFormatFlags.Left);};}
        }
        control.HandleCreated+=(_,_)=>SetWindowTheme(control.Handle,"DarkMode_CFD",null);
    }
    public static StudioButton Button(string text,bool primary=false)=>new(){Text=text,Primary=primary,Height=42,Width=180,Margin=new Padding(4),Cursor=Cursors.Hand};
    public static GraphicsPath Rounded(RectangleF r,float radius)
    {
        float d=Math.Min(radius*2,Math.Min(r.Width,r.Height));var path=new GraphicsPath();
        path.AddArc(r.X,r.Y,d,d,180,90);path.AddArc(r.Right-d,r.Y,d,d,270,90);path.AddArc(r.Right-d,r.Bottom-d,d,d,0,90);path.AddArc(r.X,r.Bottom-d,d,d,90,90);path.CloseFigure();return path;
    }
}

sealed class StudioButton : Button
{
    bool hover;
    [DefaultValue(false)] public bool Primary { get; set; }
    public StudioButton(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);FlatStyle=FlatStyle.Flat;FlatAppearance.BorderSize=0;Font=new Font("Segoe UI",10,FontStyle.Bold);ForeColor=StudioTheme.Text;BackColor=StudioTheme.Surface;}
    protected override void OnMouseEnter(EventArgs e){hover=true;Invalidate();base.OnMouseEnter(e);}
    protected override void OnMouseLeave(EventArgs e){hover=false;Invalidate();base.OnMouseLeave(e);}
    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
        var bounds=new RectangleF(1,1,Width-3,Height-3);using var shape=StudioTheme.Rounded(bounds,9);
        Color fill=Primary?StudioTheme.Accent:hover?Color.FromArgb(43,50,69):StudioTheme.Raised;
        if(Primary&&hover)fill=Color.FromArgb(255,113,134);
        if(!Enabled)fill=StudioTheme.Surface;
        using var brush=new SolidBrush(fill);e.Graphics.FillPath(brush,shape);
        using var pen=new Pen(Focused?StudioTheme.Accent:Primary?fill:StudioTheme.Border,Focused?2:1);e.Graphics.DrawPath(pen,shape);
        TextRenderer.DrawText(e.Graphics,Text,Font,ClientRectangle,Enabled?StudioTheme.Text:StudioTheme.Muted,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);
    }
}

sealed class StudioCard : Panel
{
    public StudioCard(){DoubleBuffered=true;BackColor=StudioTheme.Surface;Padding=new Padding(20);ResizeRedraw=true;}
    protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;using var path=StudioTheme.Rounded(new RectangleF(1,1,Width-3,Height-3),14);using var pen=new Pen(StudioTheme.Border);e.Graphics.DrawPath(pen,path);}
}

sealed class StudioPreview : PictureBox
{
    public RectangleF DisplayBounds { get{float size=Math.Max(1,Math.Min(ClientSize.Width,ClientSize.Height)-40);return new((ClientSize.Width-size)/2,(ClientSize.Height-size)/2,size,size);} }
    public StudioPreview(){DoubleBuffered=true;BackColor=StudioTheme.Background;SizeMode=PictureBoxSizeMode.Zoom;}
    protected override void OnPaintBackground(PaintEventArgs e)
    {
        e.Graphics.Clear(BackColor);using var dots=new SolidBrush(Color.FromArgb(36,43,61));
        for(int x=12;x<Width;x+=24)for(int y=12;y<Height;y+=24)e.Graphics.FillEllipse(dots,x,y,2,2);
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        var bounds=DisplayBounds;e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
        using var ring=new Pen(Color.FromArgb(61,71,94),2);e.Graphics.DrawEllipse(ring,bounds.X-6,bounds.Y-6,bounds.Width+12,bounds.Height+12);
        var state=e.Graphics.Save();using var clip=new GraphicsPath();clip.AddEllipse(bounds);e.Graphics.SetClip(clip);
        // Temporarily clip the regular PictureBox drawing to a circle and our exact canvas bounds.
        if(Image!=null)e.Graphics.DrawImage(Image,bounds);
        e.Graphics.Restore(state);
        // Raise Paint for editor selection guides without asking PictureBox to draw the image twice.
        if(SelectionPainter!=null)SelectionPainter(e);
    }
    [Browsable(false),DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] public Action<PaintEventArgs>? SelectionPainter { get; set; }
}

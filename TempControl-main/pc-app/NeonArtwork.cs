using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

static class NeonArtwork
{
    static Stream Resource(string name)=>typeof(NeonArtwork).Assembly.GetManifestResourceStream("Qscreen.neon50."+name+".png")??throw new InvalidDataException("Neon 50 illustratie ontbreekt: "+name);
    public static Bitmap BrandMark()
    {using var source=Resource("qprints-brand-reference");using var image=Image.FromStream(source);return LogoPiece(image,new Rectangle(42,3,90,73),new Size(180,146));}
    static Bitmap LogoPiece(Image image,Rectangle crop,Size size)
    {
        var result=new Bitmap(size.Width,size.Height);using(var g=Graphics.FromImage(result))g.DrawImage(image,new Rectangle(Point.Empty,size),crop,GraphicsUnit.Pixel);
        // The supplied mark stays intact; remove only the poster's dark surrounding field.
        for(int y=0;y<result.Height;y++)for(int x=0;x<result.Width;x++){var c=result.GetPixel(x,y);int brightness=Math.Max(c.R,Math.Max(c.G,c.B));if(brightness<85)result.SetPixel(x,y,Color.Transparent);}
        return result;
    }
    public static string Create(string scene)
    {
        if(scene=="tactical")return TemplateArtwork.Create("tactical");
        if(scene is not ("black" or "weather" or "metal" or "nixie" or "geometry" or "racing" or "circuit" or "signature" or "hud" or "reactor"))
        {using var file=Resource(scene);using var data=new MemoryStream();file.CopyTo(data);return Convert.ToBase64String(data.ToArray());}
        using var image=new Bitmap(412,412);using var g=Graphics.FromImage(image);g.SmoothingMode=SmoothingMode.AntiAlias;g.Clear(Color.Black);
        void Line(Color color,float width,float x,float y,float a,float b){using var pen=new Pen(color,width);g.DrawLine(pen,x,y,a,b);}
        if(scene=="weather")
        {using var sky=new LinearGradientBrush(new Point(0,0),new Point(0,412),Color.FromArgb(4,18,45),Color.FromArgb(25,79,116));g.FillRectangle(sky,0,0,412,412);for(int i=0;i<4;i++){using var cloud=new SolidBrush(Color.FromArgb(12,150,210,250));g.FillEllipse(cloud,-70+i*100,232+i*18,215,62);}}
        if(scene is "metal" or "nixie")
        {for(int i=-20;i<40;i++)Line(Color.FromArgb(25,37,39,46),1,i*13,0,i*13+140,412);}
        if(scene=="nixie")
        {
            for(int i=0;i<4;i++)
            {
                var box=new RectangleF(72+i*69,129,62,116);using var tube=StudioTheme.Rounded(box,17);using var tint=new SolidBrush(Color.FromArgb(25,255,115,30));g.FillPath(tint,tube);using var edge=new Pen(Color.FromArgb(185,182,119,64),2);g.DrawPath(edge,tube);Line(Color.FromArgb(100,255,201,137),1,box.X+8,144,box.X+8,227);using var baseplate=new SolidBrush(Color.FromArgb(73,46,24));g.FillRectangle(baseplate,box.X,239,62,9);
            }
            for(int i=0;i<6;i++){using var bolt=new SolidBrush(Color.FromArgb(82,63,44));g.FillEllipse(bolt,70+i*51,90,9,9);}
        }
        if(scene=="geometry")
        {
            for(int i=0;i<5;i++){float inset=i*11;Color color=i%2==0?Color.FromArgb(190,110,255):Color.FromArgb(45,205,255);var points=new PointF[]{new(206,49+inset),new(60+inset,270-inset/2),new(352-inset,270-inset/2)};using var glow=new Pen(Color.FromArgb(35,color),10);g.DrawPolygon(glow,points);using var pen=new Pen(color,2);g.DrawPolygon(pen,points);}
        }
        if(scene is "hud" or "reactor")
        {
            using var haze=new Pen(Color.FromArgb(30,20,170,220),14);g.DrawEllipse(haze,40,40,332,332);
            for(int layer=0;layer<4;layer++)
            {int inset=30+layer*15;using var p=new Pen(Color.FromArgb(65+layer*14,20,175,215),layer%2+1);g.DrawArc(p,inset,inset,412-inset*2,412-inset*2,20+layer*61,115);g.DrawArc(p,inset,inset,412-inset*2,412-inset*2,205+layer*31,40);}
            for(int i=0;i<60;i++)
            {double a=i*Math.PI/30;float radius=180,length=i%5==0?15:5;Line(Color.FromArgb(i%5==0?170:65,30,205,245),i%5==0?2:1,206+(float)Math.Cos(a)*radius,206+(float)Math.Sin(a)*radius,206+(float)Math.Cos(a)*(radius-length),206+(float)Math.Sin(a)*(radius-length));}
            for(int i=0;i<4;i++){using var p=new Pen(Color.FromArgb(95,30,205,245),2);g.DrawRectangle(p,48+i%2*267,117+i/2*125,48,33);}
        }
        if(scene=="racing")
        {
            var saved=g.Save();g.TranslateTransform(206,93);g.RotateTransform(-12);
            for(int row=0;row<5;row++)for(int col=-5;col<6;col++){using var square=new SolidBrush((row+col)%2==0?Color.FromArgb(210,220,227):Color.FromArgb(24,27,33));g.FillRectangle(square,col*29,row*24-35,29,24);}g.Restore(saved);
            Line(Color.Red,3,75,259,337,259);Line(Color.FromArgb(150,255,25,40),1,55,270,357,270);
        }
        if(scene is "circuit" or "signature")
        {
            // Sample only the user's actual logo from the supplied poster; never redraw the brand.
            using var source=Resource("qprints-brand-reference");using var logo=Image.FromStream(source);
            if(scene=="signature")
            {using var mark=LogoPiece(logo,new Rectangle(42,3,90,73),new Size(166,134));using var word=LogoPiece(logo,new Rectangle(134,8,235,34),new Size(282,41));g.DrawImageUnscaled(mark,122,60);g.DrawImageUnscaled(word,65,202);}
            else
            {
                using var chip=new SolidBrush(Color.FromArgb(31,5,35));g.FillRectangle(chip,138,129,136,136);
                using var edge=new Pen(Color.HotPink,3);g.DrawRectangle(edge,138,129,136,136);
                for(int side=0;side<4;side++)for(int i=0;i<9;i++)
                {var saved=g.Save();g.TranslateTransform(206,197);g.RotateTransform(side*90);float x=-56+i*14;Line(i%2==0?Color.Magenta:Color.FromArgb(150,128,45,250),2,x,-69,x,-90-i%3*13);Line(Color.FromArgb(150,128,45,250),1,x,-90-i%3*13,x+(i%2==0?14:-14),-107-i%3*13);g.Restore(saved);}
                using var mark=LogoPiece(logo,new Rectangle(42,3,90,73),new Size(110,89));g.DrawImageUnscaled(mark,151,150);
            }
        }
        using(var rim=new Pen(Color.FromArgb(86,102,124),2))g.DrawEllipse(rim,7,7,398,398);
        using var png=new MemoryStream();image.Save(png,ImageFormat.Png);return Convert.ToBase64String(png.ToArray());
    }
}

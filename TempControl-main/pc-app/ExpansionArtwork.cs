using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

static class ExpansionArtwork
{
    public static string Create(string scene)
    {
        if(scene is not ("biohazard" or "quantum-core" or "rgb-dream"))
        {
            using var resource=typeof(ExpansionArtwork).Assembly.GetManifestResourceStream("Qscreen.expansion50."+scene+".png")??throw new InvalidDataException("Expansion illustratie ontbreekt: "+scene);
            using var data=new MemoryStream();resource.CopyTo(data);return Convert.ToBase64String(data.ToArray());
        }
        using var image=new Bitmap(412,412);using var g=Graphics.FromImage(image);g.SmoothingMode=SmoothingMode.AntiAlias;g.Clear(Color.Black);
        if(scene=="biohazard")
        {
            for(int i=-8;i<24;i++){using var p=new Pen(Color.FromArgb(28,80,120,85),3);g.DrawLine(p,i*27,0,i*27+80,412);}
            using var font=new Font("Segoe UI Symbol",175,FontStyle.Regular,GraphicsUnit.Pixel);using var format=new StringFormat{Alignment=StringAlignment.Center,LineAlignment=StringAlignment.Center};
            for(int i=7;i>=0;i--){using var glow=new SolidBrush(Color.FromArgb(i==0?255:15,80,255,110));g.DrawString("☣",font,glow,new RectangleF(46-i,100-i,320+i*2,175+i*2),format);}
        }
        if(scene=="quantum-core")
        {
            var saved=g.Save();g.TranslateTransform(206,195);
            for(int i=0;i<3;i++){using var glow=new Pen(Color.FromArgb(25,155,75,255),14);using var p=new Pen(i==1?Color.HotPink:Color.FromArgb(135,120,255),3);g.RotateTransform(60);g.DrawEllipse(glow,-134,-53,268,106);g.DrawEllipse(p,-134,-53,268,106);}
            using(var center=new SolidBrush(Color.FromArgb(255,177,234)))g.FillEllipse(center,-16,-16,32,32);g.Restore(saved);
        }
        using(var rim=new Pen(Color.FromArgb(63,72,98),2))g.DrawEllipse(rim,7,7,398,398);
        using var png=new MemoryStream();image.Save(png,ImageFormat.Png);return Convert.ToBase64String(png.ToArray());
    }
}

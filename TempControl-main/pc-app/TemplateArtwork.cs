using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

// Original procedural decorative artwork. All clocks, meters and sensor labels are separate widgets.
static class TemplateArtwork
{
    public static string Create(string scene)
    {
        if(scene=="tactical")
        {
            using var resource=typeof(TemplateArtwork).Assembly.GetManifestResourceStream("Qscreen.assets.tactical-background.png");
            if(resource!=null){using var data=new MemoryStream();resource.CopyTo(data);return Convert.ToBase64String(data.ToArray());}
        }
        using var image=new Bitmap(824,824);using var g=Graphics.FromImage(image);g.ScaleTransform(2,2);g.SmoothingMode=SmoothingMode.AntiAlias;
        Color top=scene switch{"city"=>Color.FromArgb(22,7,43),"frost"=>Color.FromArgb(22,65,104),"anime"=>Color.FromArgb(100,27,43),"retro"=>Color.FromArgb(28,9,55),"mono"=>Color.FromArgb(27,31,38),"weather"=>Color.FromArgb(7,22,45),_=>Color.FromArgb(5,10,22)};
        Color bottom=scene switch{"frost"=>Color.FromArgb(118,184,211),"anime"=>Color.FromArgb(250,136,57),"retro"=>Color.FromArgb(177,36,112),"mono"=>Color.FromArgb(92,100,110),"aurora"=>Color.FromArgb(13,45,48),_=>Color.FromArgb(13,14,27)};
        using(var sky=new LinearGradientBrush(new Point(0,0),new Point(0,412),top,bottom))g.FillRectangle(sky,0,0,412,412);
        var random=new Random(183);
        void Line(Color c,float width,float x,float y,float a,float b){using var p=new Pen(c,width);g.DrawLine(p,x,y,a,b);}
        void Circle(Color c,float x,float y,float size){using var b=new SolidBrush(c);g.FillEllipse(b,x,y,size,size);}
        void Polygon(Color c,params PointF[] points){using var b=new SolidBrush(c);g.FillPolygon(b,points);}
        if(scene=="city")
        {
            Circle(Color.FromArgb(35,255,40,170),230,64,140);
            for(int layer=0;layer<3;layer++)for(int i=0;i<17;i++){int x=i*27-16+layer*8,h=random.Next(40,150),y=345-h;using var b=new SolidBrush(Color.FromArgb(14+layer*5,12,30+layer*8));g.FillRectangle(b,x,y,22,170);Line(layer%2==0?Color.FromArgb(150,223,35,161):Color.FromArgb(130,45,180,246),1,x,y,x,y+h);for(int k=0;k<7;k++)Line(Color.FromArgb(100,255,75,180),1,x+7,y+8+k*14,x+11,y+8+k*14);}
            for(int i=0;i<8;i++)Line(Color.FromArgb(40,255,45,170),1,206,300,i*65-60,412);
        }
        else if(scene is "frost" or "mono" or "aurora" or "anime" or "retro")
        {
            if(scene is "anime" or "retro"){Circle(scene=="anime"?Color.FromArgb(255,202,109):Color.FromArgb(249,103,159),250,175,108);if(scene=="retro")for(int y=230;y<285;y+=8){using var stripe=new SolidBrush(bottom);g.FillRectangle(stripe,245,y,126,3);}}
            for(int layer=0;layer<4;layer++)
            {
                var points=new List<PointF>{new(0,412),new(0,235+layer*31)};
                for(int x=0;x<=450;x+=scene=="retro"?16:45)points.Add(new(x,217+layer*36+random.Next(-45,36)));
                points.Add(new(450,412));Color c=scene=="frost"?Color.FromArgb(44-layer*8,95-layer*17,125-layer*22):scene=="mono"?Color.FromArgb(58-layer*14,64-layer*15,73-layer*16):Color.FromArgb(24-layer*4,24-layer*4,39-layer*7);Polygon(c,points.ToArray());
            }
            if(scene=="anime")
            { // Original anonymous traveler, cape and windswept hair; no licensed character.
                Circle(Color.FromArgb(13,14,25),269,242,13);Polygon(Color.FromArgb(13,14,25),new(270,253),new(282,253),new(300,295),new(267,282));Line(Color.FromArgb(13,14,25),5,274,279,268,307);Line(Color.FromArgb(13,14,25),4,279,281,289,308);Polygon(Color.FromArgb(13,14,25),new(268,245),new(266,237),new(273,241),new(279,237),new(282,249));}
            if(scene=="aurora")for(int i=0;i<16;i++){float x=i*29;Polygon(Color.FromArgb(6,22,26),new(x,326),new(x+16,380),new(x-16,380));}
        }
        else if(scene=="space")
        {
            for(int i=0;i<100;i++)Circle(Color.FromArgb(random.Next(60,210),Color.LightBlue),random.Next(412),random.Next(412),random.Next(1,3));
            using(var planet=new LinearGradientBrush(new Point(260,50),new Point(380,150),Color.FromArgb(73,114,167),Color.FromArgb(13,22,40)))g.FillEllipse(planet,274,55,110,110);
            using var orbit=new Pen(Color.FromArgb(100,125,180,210),2);g.DrawArc(orbit,244,75,165,64,10,170);Circle(Color.FromArgb(12,30,55),-90,337,650);
            using var helmet=new Pen(Color.FromArgb(100,170,210,235),2);g.DrawArc(helmet,43,206,60,68,170,210);g.DrawArc(helmet,51,217,43,39,190,170);Line(helmet.Color,2,56,270,85,270);
        }
        else if(scene is "hardware" or "metal" or "tactical")
        {
            for(int i=-8;i<26;i++)Line(Color.FromArgb(38,60,65,77),scene=="metal"?1:8,i*22,0,i*22+130,412);
            if(scene=="hardware")
            {for(int fan=0;fan<2;fan++){float x=fan*240-50;using var p=new Pen(Color.FromArgb(58,98,80,160),4);g.DrawEllipse(p,x,130,220,220);for(int k=0;k<10;k++){double a=k*Math.PI/5;Line(Color.FromArgb(55,100,120,150),11,x+110+(float)Math.Sin(a)*24,240+(float)Math.Cos(a)*24,x+110+(float)Math.Sin(a+.4)*90,240+(float)Math.Cos(a+.4)*90);}}}
            if(scene=="tactical"){Polygon(Color.FromArgb(45,47,52),new(0,68),new(98,88),new(60,114),new(0,97));Polygon(Color.FromArgb(45,47,52),new(412,300),new(317,321),new(350,346),new(412,330));for(int i=0;i<4;i++)Line(Color.FromArgb(130,180,28,37),3,44+i*8,125,55+i*8,115);}
        }
        else if(scene=="weather")
        {Circle(Color.FromArgb(15,133,199,255),220,30,200);for(int i=0;i<4;i++){using var cloud=new SolidBrush(Color.FromArgb(8,170,210,245));g.FillEllipse(cloud,-60+i*100,270+i*17,220,60);} }
        if(scene=="tactical")
        {
            // Original utility aircraft, skyline and anonymous helmet; no game insignia or character artwork.
            using(var haze=new LinearGradientBrush(new Point(0,45),new Point(0,215),Color.FromArgb(150,148,151,153),Color.FromArgb(0,15,15,18)))g.FillEllipse(haze,35,42,340,185);
            for(int i=0;i<15;i++){int x=37+i*25,h=random.Next(15,54);using var building=new SolidBrush(Color.FromArgb(100,10,12,15));g.FillRectangle(building,x,182-h,17,h);g.FillRectangle(building,x,399-h,20,h);for(int k=0;k<3;k++)Line(Color.FromArgb(40,160,163,168),1,x+4,185-h+k*8,x+6,185-h+k*8);}
            Polygon(Color.FromArgb(15,16,18),new(68,155),new(96,155),new(93,116),new(73,116));Polygon(Color.FromArgb(15,16,18),new(62,116),new(101,116),new(82,103));Line(Color.FromArgb(15,16,18),2,77,107,77,95);
            using(var craft=new SolidBrush(Color.FromArgb(32,33,35)))g.FillEllipse(craft,104,104,27,9);Line(Color.FromArgb(32,33,35),2,126,108,150,100);Line(Color.FromArgb(32,33,35),2,118,104,118,98);Line(Color.FromArgb(32,33,35),1,94,98,143,98);Line(Color.FromArgb(32,33,35),1,110,116,129,116);
            for(int i=0;i<10;i++){float x=i%2==0?35:338;float y=120+i*19;Line(Color.FromArgb(110,165,13,23),1,x,y,x+random.Next(-20,21),y+13);}
            // Raised helmet at the upper right, with a wraparound visor and a separate respirator.
            using(var helmet=new GraphicsPath())
            {helmet.AddBezier(261,102,254,66,277,49,300,53);helmet.AddBezier(300,53,329,55,341,80,333,106);helmet.AddLine(333,106,319,132);helmet.AddLine(319,132,272,125);helmet.CloseFigure();using var steel=new LinearGradientBrush(new Point(260,55),new Point(331,130),Color.FromArgb(142,147,151),Color.FromArgb(23,26,30));g.FillPath(steel,helmet);using var edge=new Pen(Color.FromArgb(180,190,194),1);g.DrawPath(edge,helmet);}
            Polygon(Color.FromArgb(13,16,20),new(263,87),new(325,82),new(334,98),new(315,112),new(270,105));Line(Color.FromArgb(100,160,170,180),2,271,89,317,86);
            Polygon(Color.FromArgb(48,52,57),new(280,110),new(309,109),new(319,129),new(292,141),new(277,127));for(int i=0;i<5;i++)Line(Color.FromArgb(14,15,17),2,284+i*5,117,288+i*5,128);Circle(Color.FromArgb(80,87,94),270,111,14);Circle(Color.FromArgb(16,18,22),273,114,8);
            Line(Color.FromArgb(35,39,44),5,276,132,265,158);Line(Color.FromArgb(35,39,44),5,318,130,334,154);
            for(int i=0;i<450;i++){int x=random.Next(25,388),y=random.Next(30,395);using var grain=new SolidBrush(Color.FromArgb(random.Next(4,27),Color.Silver));g.FillRectangle(grain,x,y,1,1);}
            for(int ring=0;ring<7;ring++){Color c=ring switch{0=>Color.FromArgb(44,48,54),1=>Color.FromArgb(154,163,175),2=>Color.FromArgb(65,73,84),3=>Color.FromArgb(28,32,40),_=>Color.FromArgb(16+ring*3,19+ring*3,24+ring*3)};using var p=new Pen(c,2);g.DrawEllipse(p,8+ring*1.5f,8+ring*1.5f,396-ring*3,396-ring*3);}
            using var highlight=new Pen(Color.FromArgb(130,210,220,235),1);g.DrawArc(highlight,9,9,394,394,195,105);
        }
        using var output=new MemoryStream();image.Save(output,ImageFormat.Png);return Convert.ToBase64String(output.ToArray());
    }
}

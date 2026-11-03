using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text.Json.Serialization;

enum WidgetMotion { None, Pulse, Rotate, RgbCycle, Breathe }
enum PanelShape { Rounded, Angular, Outline }
[TypeConverter(typeof(ExpandableObjectConverter))]
sealed class WidgetColorRule
{
    public float Minimum { get; set; }
    [Browsable(false)]public int ColorArgb { get; set; }=Color.Orange.ToArgb();
    [JsonIgnore]public Color Color {get=>Color.FromArgb(ColorArgb);set=>ColorArgb=value.ToArgb();}
    public bool Pulse { get; set; }
}
static class PremiumDrawing
{
    static readonly Color[] spectrumColors=[Color.Red,Color.Yellow,Color.Lime,Color.Cyan,Color.Blue,Color.Magenta,Color.Red];
    public static void Validate(ScreenWidget w)
    {
        if(w.ClockCharacter is <-1 or >31)throw new InvalidDataException("Klokteken moet -1 tot 31 zijn.");
        if(!Enum.IsDefined(w.Motion)||!Enum.IsDefined(w.PanelShape)||w.Opacity is <0 or >100||!float.IsFinite(w.MotionSpeed)||w.MotionSpeed is <0.1f or >5||!float.IsFinite(w.StrokeWidth)||w.StrokeWidth is <1 or >50||!float.IsFinite(w.StartAngle)||Math.Abs(w.StartAngle)>3600||!float.IsFinite(w.SweepAngle)||w.SweepAngle is <1 or >360||w.Segments is <0 or >120||w.ColorRules==null||w.ColorRules.Count>16||w.ColorRules.Any(r=>r==null||!float.IsFinite(r.Minimum)))throw new InvalidDataException("Ongeldige meter, kleurregel of animatie.");
    }
    public static Color Alpha(Color c,int opacity)=>Color.FromArgb(c.A*Math.Clamp(opacity,0,100)/100,c.R,c.G,c.B);
    public static Color Mix(Color a,Color b,float f)=>Color.FromArgb((int)(a.A+(b.A-a.A)*f),(int)(a.R+(b.R-a.R)*f),(int)(a.G+(b.G-a.G)*f),(int)(a.B+(b.B-a.B)*f));
    public static Color ColorFor(ScreenWidget w,float? value,Color fallback,DateTime now,bool animated)
    {
        var rule=value.HasValue?w.ColorRules.Where(r=>value>=r.Minimum).OrderByDescending(r=>r.Minimum).FirstOrDefault():null;
        Color c=rule==null?fallback:Color.FromArgb(rule.ColorArgb);
        double phase=now.TimeOfDay.TotalSeconds*w.MotionSpeed;
        if(animated&&w.Motion==WidgetMotion.RgbCycle)c=Mix(w.Foreground,w.Accent,(float)(.5+.5*Math.Sin(phase)));
        if(animated&&(w.Motion is WidgetMotion.Pulse or WidgetMotion.Breathe||rule?.Pulse==true))c=Alpha(c,(int)(65+35*Math.Sin(phase*2)));
        return Alpha(c,w.Opacity);
    }
    public static bool Draw(Graphics g,ScreenWidget w,RectangleF b,ScreenReadings r,Color c,bool animated,float? eased=null)
    {
        using var brush=new SolidBrush(c);using var fill=new SolidBrush(Alpha(w.Fill,w.Opacity));
        if(w.Kind==WidgetKind.NeonLine)
        {
            using var glow=new Pen(Alpha(c,25),w.StrokeWidth*4){StartCap=LineCap.Round,EndCap=LineCap.Round};using var edge=new Pen(c,w.StrokeWidth){StartCap=LineCap.Round,EndCap=LineCap.Round};
            g.DrawLine(glow,b.Left,b.Bottom,b.Right,b.Top);g.DrawLine(edge,b.Left,b.Bottom,b.Right,b.Top);return true;
        }
        if(w.Kind==WidgetKind.InfinityRibbon)
        {
            b.Inflate(-w.StrokeWidth*1.5f,-w.StrokeWidth*1.5f);if(b.Width<=0||b.Height<=0)return true;
            double phase=animated&&w.Motion!=WidgetMotion.None?r.Now.TimeOfDay.TotalSeconds*w.MotionSpeed*.12:0;
            PointF Point(double t)=>new(b.X+b.Width/2+(float)Math.Sin(t)*b.Width/2,b.Y+b.Height/2+(float)Math.Sin(t*2)*b.Height/2);
            using var glow=new Pen(Color.Transparent,w.StrokeWidth*3){StartCap=LineCap.Round,EndCap=LineCap.Round};using var light=new Pen(Color.Transparent,w.StrokeWidth){StartCap=LineCap.Round,EndCap=LineCap.Round};
            var a=Point(0);
            for(int i=0;i<160;i++)
            {
                float t=(float)((i/160d+phase)%1);Color color=w.Rainbow?Spectrum(t):Mix(w.Foreground,w.Accent,(float)(.5+.5*Math.Sin(t*Math.PI*2)));
                glow.Color=Alpha(color,w.Opacity*18/100);light.Color=Alpha(color,w.Opacity);
                var z=Point((i+1)*Math.PI*2/160);g.DrawLine(glow,a,z);g.DrawLine(light,a,z);a=z;
            }
            return true;
        }
        if(w.Kind==WidgetKind.ThermalFace)
        {
            float? temperature=r.Value(w.BarSource);int mood=temperature==null?-1:temperature>=80?2:temperature>=60?1:0;
            using var face=new SolidBrush(Alpha(mood==2?Color.OrangeRed:w.Fill,w.Opacity));using var path=StudioTheme.Rounded(b,10);g.FillPath(face,path);
            using var ink=new Pen(c,Math.Max(2,b.Width/27)){StartCap=LineCap.Round,EndCap=LineCap.Round};
            float x=b.X,y=b.Y,s=b.Width,h=b.Height;
            g.DrawLine(ink,x+s*.2f,y+h*.4f,x+s*.3f,y+h*.4f);g.DrawLine(ink,x+s*.7f,y+h*.4f,x+s*.8f,y+h*.4f);
            if(mood>=1){g.DrawLine(ink,x+s*.15f,y+h*.2f,x+s*.36f,y+h*.3f);g.DrawLine(ink,x+s*.64f,y+h*.3f,x+s*.85f,y+h*.2f);}
            if(mood<0)g.DrawLine(ink,x+s*.32f,y+h*.69f,x+s*.68f,y+h*.69f);
            else if(mood==0)g.DrawArc(ink,x+s*.28f,y+h*.43f,s*.44f,h*.4f,0,180);
            else g.DrawArc(ink,x+s*.28f,y+h*.64f,s*.44f,h*.3f,180,180);
            return true;
        }
        if(w.Kind==WidgetKind.Panel)
        {
            using var path=w.PanelShape==PanelShape.Angular?Angular(b):StudioTheme.Rounded(b,14);
            if(w.PanelShape!=PanelShape.Outline)g.FillPath(fill,path);using var p=new Pen(c,w.StrokeWidth);g.DrawPath(p,path);return true;
        }
        if(w.Kind==WidgetKind.CircularGauge)
        {
            float stroke=Math.Min(w.StrokeWidth,Math.Min(b.Width,b.Height)/3);b.Inflate(-stroke/2,-stroke/2);
            if(b.Width<=0||b.Height<=0)return true;
            float angle=w.StartAngle+(animated&&w.Motion==WidgetMotion.Rotate?(float)(r.Now.TimeOfDay.TotalSeconds*w.MotionSpeed*25%360):0);
            using var track=new Pen(Alpha(w.Fill,w.Opacity),stroke);using var pen=new Pen(c,stroke){StartCap=LineCap.Round,EndCap=LineCap.Round};
            float? value=r.Value(w.BarSource);float fraction=w.DecorativeRing?1:eased??Math.Clamp((value??0)/Math.Max(1,w.Maximum),0,1);
            if(w.Rainbow||w.Motion==WidgetMotion.RgbCycle)
            {
                g.DrawArc(track,b,angle,w.SweepAngle);float phase=animated?(float)(r.Now.TimeOfDay.TotalSeconds*w.MotionSpeed*.1%1):0;
                using var spectrum=new Pen(c,stroke);for(int i=0;i<72*fraction;i++){float t=(i/72f+phase)%1;Color channel=w.Rainbow?Spectrum(t):t<.5f?Mix(w.Foreground,w.Accent,t*2):Mix(w.Accent,w.Foreground,(t-.5f)*2);spectrum.Color=Alpha(channel,w.Opacity);g.DrawArc(spectrum,b,angle+w.SweepAngle*i/72,w.SweepAngle/72+1);}
            }
            else if(w.Segments==0){g.DrawArc(track,b,angle,w.SweepAngle);if(fraction>0)g.DrawArc(pen,b,angle,w.SweepAngle*fraction);}
            else for(int i=0;i<w.Segments;i++){float sweep=w.SweepAngle/w.Segments;using var segment=new Pen(i<Math.Floor(fraction*w.Segments)?Mix(c,Alpha(w.Accent,w.Opacity),(float)i/Math.Max(1,w.Segments-1)):track.Color,stroke);g.DrawArc(segment,b,angle+i*sweep,Math.Max(.1f,sweep*.72f));}
            if(w.Needle&&value.HasValue){double a=(angle+fraction*w.SweepAngle)*Math.PI/180;float x=b.X+b.Width/2,y=b.Y+b.Height/2;using var needle=new Pen(c,2);g.DrawLine(needle,x,y,x+(float)Math.Cos(a)*b.Width*.43f,y+(float)Math.Sin(a)*b.Height*.43f);g.FillEllipse(brush,x-4,y-4,8,8);}
            return true;
        }
        if(w.Kind==WidgetKind.AnalogClock)
        {
            g.FillEllipse(fill,b);using var p=new Pen(c,1);g.DrawEllipse(p,b);float x=b.X+b.Width/2,y=b.Y+b.Height/2,rad=Math.Min(b.Width,b.Height)/2;
            for(int i=0;i<60;i++){double a=i*Math.PI/30;float inner=rad-(i%5==0?12:5);g.DrawLine(p,x+(float)Math.Sin(a)*inner,y-(float)Math.Cos(a)*inner,x+(float)Math.Sin(a)*(rad-2),y-(float)Math.Cos(a)*(rad-2));}
            double second=r.Now.Second+(animated?r.Now.Millisecond/1000d:0),minute=r.Now.Minute+second/60,hour=r.Now.Hour%12+minute/60;
            void Hand(double a,float length,float width,Color color){using var pen=new Pen(color,width){EndCap=LineCap.Round};g.DrawLine(pen,x,y,x+(float)Math.Sin(a)*length,y-(float)Math.Cos(a)*length);}
            Hand(hour*Math.PI/6,rad*.48f,5,c);Hand(minute*Math.PI/30,rad*.73f,3,c);Hand(second*Math.PI/30,rad*.82f,1.5f,Alpha(w.Accent,w.Opacity));g.FillEllipse(brush,x-4,y-4,8,8);return true;
        }
        if(w.Kind==WidgetKind.WeatherIcon)
        {
            using var pen=new Pen(c,3);if(r.Weather==null){g.DrawEllipse(pen,b.X+b.Width*.3f,b.Y+b.Height*.2f,b.Width*.4f,b.Height*.4f);g.DrawLine(pen,b.X+8,b.Bottom-8,b.Right-8,b.Y+8);return true;}
            if(r.Weather.Code<=1){g.DrawEllipse(pen,b.X+10,b.Y+10,b.Width-20,b.Height-20);if(!r.Weather.Day){using var night=new SolidBrush(Alpha(w.Fill,w.Opacity));g.FillEllipse(night,b.X+b.Width*.4f,b.Y+4,b.Width*.55f,b.Height*.6f);}}
            else{g.FillEllipse(brush,b.X+4,b.Y+b.Height*.4f,b.Width*.9f,b.Height*.4f);g.FillEllipse(brush,b.X+b.Width*.3f,b.Y+5,b.Width*.45f,b.Height*.65f);if(r.Weather.Code>=50)for(int i=0;i<3;i++)g.DrawLine(pen,b.X+15+i*13,b.Bottom-6,b.X+11+i*13,b.Bottom+2);}
            return true;
        }
        return false;
    }
    static GraphicsPath Angular(RectangleF b){var p=new GraphicsPath();float d=Math.Min(14,Math.Min(b.Width,b.Height)/3);p.AddPolygon(new PointF[]{new(b.X+d,b.Y),new(b.Right,b.Y),new(b.Right,b.Bottom-d),new(b.Right-d,b.Bottom),new(b.X,b.Bottom),new(b.X,b.Y+d)});return p;}
    public static bool Animation(Graphics g,PageAnimation type,double phase)
    {
        if(type==PageAnimation.Ripples)
        {for(int i=0;i<3;i++){float p=(float)((phase*.14+i/3d)%1),radius=25+p*130;using var pen=new Pen(Color.FromArgb((int)((1-p)*55),100,205,240),1);g.DrawEllipse(pen,206-radius,238-radius*.35f,radius*2,radius*.7f);}}
        else if(type==PageAnimation.SpectralFire)
        {
            for(int i=0;i<7;i++){using var pen=new Pen(Color.FromArgb(60,90,220,255),2);var points=Enumerable.Range(0,13).Select(j=>new PointF(46+i*52+(float)Math.Sin(j*.4+phase*1.4+i)*12,280-j*13+(float)Math.Sin(phase+i)*8)).ToArray();g.DrawCurve(pen,points);}
        }
        else if(type is PageAnimation.Petals or PageAnimation.Hearts)
        {
            using var font=new Font("Segoe UI Symbol",12,FontStyle.Regular,GraphicsUnit.Pixel);
            for(int i=0;i<17;i++){float x=(float)((i*71+Math.Sin(phase*.7+i)*18+460)%412),y=(float)(((i*53+phase*(type==PageAnimation.Hearts?-13:18))%470+470)%470)-25;using var b=new SolidBrush(Color.FromArgb(110,255,130,190));if(type==PageAnimation.Hearts)g.DrawString("♥",font,b,x,y);else g.FillEllipse(b,x,y,4+i%3,7+i%3);}
        }
        else if(type==PageAnimation.Lightning)
        {int alpha=(int)(Math.Pow(Math.Max(0,Math.Sin(phase*.8)),24)*115);using var p=new Pen(Color.FromArgb(alpha,180,155,255),2);g.DrawLines(p,new PointF[]{new(268,38),new(230,113),new(259,108),new(205,223),new(226,152),new(201,156)});}
        else if(type is PageAnimation.Snow or PageAnimation.Embers or PageAnimation.Bubbles)
        {
            for(int i=0;i<28;i++)
            {
                float x=(float)((i*79+Math.Sin(phase*.7+i)*12+450)%412);
                float y=(float)(((i*53+phase*(type==PageAnimation.Snow?19:-23))%470+470)%470)-28;
                float radius=type==PageAnimation.Bubbles?3+i%4:1+i%2;
                using var pen=new Pen(Color.FromArgb(95,type==PageAnimation.Embers?Color.Orange:type==PageAnimation.Snow?Color.White:Color.Cyan),1);
                if(type==PageAnimation.Bubbles)g.DrawEllipse(pen,x,y,radius*2,radius*2);else{using var dot=new SolidBrush(pen.Color);g.FillEllipse(dot,x,y,radius*2,radius*2);}
            }
        }
        else if(type==PageAnimation.Scanlines)
        {using var pen=new Pen(Color.FromArgb(35,Color.Cyan),1);for(int i=0;i<18;i++){float y=(float)((phase*26+i*24)%440)-14;g.DrawLine(pen,45,y,367,y);}}
        else if(type==PageAnimation.Steam)
        {for(int band=0;band<3;band++){using var pen=new Pen(Color.FromArgb(45,Color.Bisque),3);var points=Enumerable.Range(0,15).Select(i=>new PointF(166+band*36+(float)Math.Sin(i*.4+phase+band)*10,180-i*7)).ToArray();g.DrawCurve(pen,points);}}
        else if(type==PageAnimation.Waves||type==PageAnimation.Aurora)
        {
            for(int band=0;band<5;band++){bool aurora=type==PageAnimation.Aurora;using var pen=new Pen(Color.FromArgb(aurora?24:70,band%2==0?Color.MediumPurple:Color.Cyan),aurora?10:2);var points=Enumerable.Range(0,53).Select(i=>new PointF(i*8,(aurora?150:230)+band*16+(float)Math.Sin(i*.12+phase*.6+band)*35+(float)Math.Cos(i*.09-phase*.4)*20)).ToArray();g.DrawCurve(pen,points);}
        }
        else if(type==PageAnimation.DigitalRain)
        {using var font=new Font("Consolas",11,FontStyle.Regular,GraphicsUnit.Pixel);for(int col=0;col<23;col++)for(int row=0;row<7;row++){int y=(int)((phase*28+col*47+row*17)%470)-30;using var b=new SolidBrush(Color.FromArgb(25+row*16,40,255,125));g.DrawString(((col*7+row)%10).ToString(),font,b,col*19,y);}}
        else if(type is PageAnimation.CityLights or PageAnimation.PixelStars)
        {for(int i=0;i<45;i++){int alpha=(int)(60+55*Math.Sin(phase+i));using var b=new SolidBrush(Color.FromArgb(alpha,Color.HotPink));g.FillRectangle(b,25+i*37%360,65+i*53%260,type==PageAnimation.PixelStars?3:2,4);}}
        else if(type==PageAnimation.Smoke)
        {for(int i=0;i<8;i++){using var b=new SolidBrush(Color.FromArgb(8,160,170,185));float x=(float)((phase*8+i*65)%550)-80;g.FillEllipse(b,x,220+(float)Math.Sin(phase*.3+i)*40,150,80);}}
        else return false;
        return true;
    }
    static Color Spectrum(float t)
    {
        float p=(t%1)*6;int index=(int)p;return Mix(spectrumColors[index],spectrumColors[index+1],p-index);
    }
}

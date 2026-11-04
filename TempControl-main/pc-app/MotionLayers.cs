using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;

sealed class MotionKeyframe
{
    public double Seconds {get;set;}
    public float X {get;set;}=206;
    public float Y {get;set;}=206;
    public float Scale {get;set;}=1;
    public float Rotation {get;set;}
    public float Opacity {get;set;}=1;
}
sealed class MotionReactiveRule
{
    public bool Enabled {get;set;}
    public WidgetKind Sensor {get;set;}=WidgetKind.CpuTemperature;
    public float Threshold {get;set;}=80;
    public float Hysteresis {get;set;}=3;
    public double CooldownSeconds {get;set;}=3;
    public float ActiveSpeed {get;set;}=2;
    public float ActiveOpacity {get;set;}=1;
}
sealed class MotionLayer
{
    public string Name {get;set;}="Animatielaag";
    [Browsable(false)]public string? ImageData {get;set;}
    public bool Visible {get;set;}=true;
    public bool Locked {get;set;}
    public int Width {get;set;}=220;
    public int Height {get;set;}=220;
    public float X {get;set;}=206;
    public float Y {get;set;}=206;
    public float Rotation {get;set;}
    public float Scale {get;set;}=1;
    public float Opacity {get;set;}=1;
    public double Duration {get;set;}=5;
    public double StartDelay {get;set;}
    public float Speed {get;set;}=1;
    public bool Loop {get;set;}=true;
    public double FadeIn {get;set;}
    public double FadeOut {get;set;}
    [Browsable(false)]public MotionReactiveRule Reactive {get;set;}=new();
    [Browsable(false)]public List<MotionKeyframe> Keyframes {get;set;}=[];
    string? checkedData;
    bool sourceAnimated;
    [Browsable(false),System.Text.Json.Serialization.JsonIgnore]public bool IsAnimated
    {
        get
        {
            if(checkedData!=ImageData){checkedData=ImageData;sourceAnimated=false;if(ImageData!=null){byte[] bytes=Convert.FromBase64String(ImageData);sourceAnimated=MotionAsset.IsAsset(bytes)?MotionAsset.Inspect(bytes).DelaysMs.Length>1:GifAnimation.IsGif(bytes)&&GifAnimation.Inspect(bytes).FrameCount>1;}}
            return Visible&&(sourceAnimated||Keyframes.Count>1||FadeIn>0||FadeOut>0||Reactive.Enabled);
        }
    }
    public override string ToString()=>(Visible?"● ":"○ ")+(Locked?"🔒 ":"")+Name;
    public MotionLayer MetadataCopy(){var copy=(MotionLayer)MemberwiseClone();copy.ImageData=null;return copy;}
    public static void Validate(List<MotionLayer> layers)
    {
        if(layers==null||layers.Count>12)throw new InvalidDataException("Maximaal 12 animatielagen.");
        long pixels=0,bytesTotal=0;var unique=new HashSet<string>(StringComparer.Ordinal);
        foreach(var l in layers)
        {
            if(l==null||l.Name==null||l.Name.Length>120||l.Width is <1 or >824||l.Height is <1 or >824||!double.IsFinite(l.Duration)||l.Duration is <.02 or >3600||!double.IsFinite(l.StartDelay)||l.StartDelay is <0 or >3600||!float.IsFinite(l.Speed)||l.Speed is <.1f or >5||!double.IsFinite(l.FadeIn)||l.FadeIn<0||l.FadeIn>l.Duration||!double.IsFinite(l.FadeOut)||l.FadeOut<0||l.FadeOut>l.Duration||l.Keyframes==null||l.Keyframes.Count>128)throw new InvalidDataException("Ongeldige animatielaag.");
            static bool Pose(float x,float y,float s,float r,float o)=>float.IsFinite(x)&&float.IsFinite(y)&&Math.Abs(x)<=1236&&Math.Abs(y)<=1236&&float.IsFinite(s)&&s is >=.01f and <=8&&float.IsFinite(r)&&Math.Abs(r)<=36000&&float.IsFinite(o)&&o is >=0 and <=1;
            if(!Pose(l.X,l.Y,l.Scale,l.Rotation,l.Opacity))throw new InvalidDataException("Ongeldige laagtransformatie.");
            var r=l.Reactive;
            if(r==null||!SensorHistory.Metrics.Contains(r.Sensor)||!float.IsFinite(r.Threshold)||Math.Abs(r.Threshold)>10000||!float.IsFinite(r.Hysteresis)||r.Hysteresis is <0 or >1000||!double.IsFinite(r.CooldownSeconds)||r.CooldownSeconds is <0 or >3600||!float.IsFinite(r.ActiveSpeed)||r.ActiveSpeed is <.1f or >5||!float.IsFinite(r.ActiveOpacity)||r.ActiveOpacity is <0 or >1)throw new InvalidDataException("Ongeldige sensorregel.");
            double previous=-1;
            foreach(var k in l.Keyframes){if(k==null||!double.IsFinite(k.Seconds)||k.Seconds<0||k.Seconds>l.Duration||k.Seconds<=previous||!Pose(k.X,k.Y,k.Scale,k.Rotation,k.Opacity))throw new InvalidDataException("Keyframes moeten unieke, oplopende tijden hebben.");previous=k.Seconds;}
            if(l.ImageData!=null)
            {
                if(l.ImageData.Length>32_000_000)throw new InvalidDataException("Animatiebron te groot.");
                byte[] bytes=Convert.FromBase64String(l.ImageData);
                if(MotionAsset.IsAsset(bytes)){MotionAsset.Inspect(bytes);bytesTotal+=bytes.Length;if(bytesTotal>24_000_000)throw new InvalidDataException("Animatiebronnen samen groter dan 24 MB.");continue;}
                int frames=GifAnimation.IsGif(bytes)?GifAnimation.Inspect(bytes).FrameCount:1;
                using var stream=new MemoryStream(bytes);using var image=Image.FromStream(stream);
                if(image.Width>2048||image.Height>2048)throw new InvalidDataException("Laagafbeelding te groot.");
                if(unique.Add(l.ImageData)){pixels+=(long)image.Width*image.Height*frames;bytesTotal+=bytes.Length;}
                if(pixels>32_000_000||bytesTotal>24_000_000)throw new InvalidDataException("De lagen gebruiken samen te veel geheugen: maximaal 32 miljoen bronpixels / 24 MB brondata.");
            }
        }
    }
    public MotionKeyframe PoseAt(double seconds)
    {
        var pose=new MotionKeyframe{X=X,Y=Y,Scale=Scale,Rotation=Rotation,Opacity=Opacity};
        if(Keyframes.Count>0)
        {
            var a=Keyframes.LastOrDefault(k=>k.Seconds<=seconds)??Keyframes[0];var b=Keyframes.FirstOrDefault(k=>k.Seconds>=seconds)??Keyframes[^1];
            float t=a==b?0:(float)((seconds-a.Seconds)/(b.Seconds-a.Seconds));
            static float L(float a,float b,float t)=>a+(b-a)*t;
            pose=new(){X=L(a.X,b.X,t),Y=L(a.Y,b.Y,t),Scale=L(a.Scale,b.Scale,t),Rotation=L(a.Rotation,b.Rotation,t),Opacity=L(a.Opacity,b.Opacity,t)*Opacity};
        }
        if(FadeIn>0)pose.Opacity*=(float)Math.Clamp(seconds/FadeIn,0,1);
        if(FadeOut>0)pose.Opacity*=(float)Math.Clamp((Duration-seconds)/FadeOut,0,1);
        return pose;
    }
}

sealed class MotionLayerRenderer : IDisposable
{
    sealed class Source : IDisposable
    {
        public readonly MemoryStream Stream;
        Image? image;
        readonly System.IO.Compression.ZipArchive? archive;
        int cachedIndex=-1;
        public readonly int[] Delays;
        public Source(string data)
        {
            byte[] bytes=Convert.FromBase64String(data);Stream=new(bytes);
            try{if(MotionAsset.IsAsset(bytes)){Delays=MotionAsset.Inspect(bytes).DelaysMs;archive=new(Stream,System.IO.Compression.ZipArchiveMode.Read,true);}else{Delays=GifAnimation.IsGif(bytes)?GifAnimation.Inspect(bytes).DelaysMs:[];image=Image.FromStream(Stream);}}
            catch{Stream.Dispose();throw;}
        }
        public Image Frame(int index)
        {
            if(archive!=null&&cachedIndex!=index){using var source=archive.GetEntry($"{index:D4}.png")!.Open();using var decoded=Image.FromStream(source);var next=new Bitmap(decoded);image?.Dispose();image=next;cachedIndex=index;}
            else if(archive==null&&Delays.Length>1)image!.SelectActiveFrame(FrameDimension.Time,index);
            return image!;
        }
        public void Dispose(){image?.Dispose();archive?.Dispose();Stream.Dispose();}
    }
    readonly Dictionary<string,Source> sources=new(StringComparer.Ordinal);
    readonly Dictionary<MotionLayer,(bool Active,DateTime Changed,float Speed,float Opacity,DateTime Last,double Phase)> rules=new();
    public bool? ReactiveActive(MotionLayer layer)=>rules.TryGetValue(layer,out var state)?state.Active:null;
    DateTime? epoch;
    List<MotionLayer>? lastLayers;
    public int[] CurrentFrames {get;private set;}=[];
    public void Draw(Graphics g,ScreenDesign design,ScreenReadings readings,double? seek,bool reactiveSeek=false)
    {
        var now=readings.Now;
        if(!ReferenceEquals(lastLayers,design.MotionLayers)){lastLayers=design.MotionLayers;epoch=now;rules.Clear();}
        var selectedFrames=new List<int>();
        epoch??=now;double elapsed=seek??Math.Max(0,(now-epoch.Value).TotalSeconds);
        var active=design.MotionLayers.Where(l=>l.ImageData!=null).Select(l=>l.ImageData!).ToHashSet(StringComparer.Ordinal);
        foreach(var key in sources.Keys.Where(k=>!active.Contains(k)).ToArray()){sources[key].Dispose();sources.Remove(key);}
        foreach(var key in rules.Keys.Where(k=>!design.MotionLayers.Contains(k)).ToArray())rules.Remove(key);
        foreach(var l in design.MotionLayers.Where(l=>l.Visible&&l.ImageData!=null))
        {
            double local=(design.AnimationsEnabled?elapsed:0)-l.StartDelay;if(local<0)continue;
            float opacity=1;
            if(l.Reactive.Enabled&&(seek==null||reactiveSeek)&&design.AnimationsEnabled)
            {
                var r=l.Reactive;var value=readings.Value(r.Sensor);
                if(!rules.TryGetValue(l,out var state))state=(false,DateTime.MinValue,1,1,now,local*l.Speed);
                bool next=value.HasValue&&(state.Active?value.Value>=r.Threshold-r.Hysteresis:value.Value>=r.Threshold);
                if(next!=state.Active&&(!value.HasValue||(now-state.Changed).TotalSeconds>=r.CooldownSeconds))state=(next,now,state.Speed,state.Opacity,state.Last,state.Phase);
                float blend=(float)Math.Clamp((now-state.Last).TotalSeconds/.4,0,1);
                float speed=state.Speed+((state.Active?r.ActiveSpeed:1)-state.Speed)*blend;
                opacity=state.Opacity+((state.Active?r.ActiveOpacity:1)-state.Opacity)*blend;
                double phase=state.Phase+Math.Max(0,(now-state.Last).TotalSeconds)*l.Speed*speed;
                rules[l]=(state.Active,state.Changed,speed,opacity,now,phase);local=seek.HasValue?local*l.Speed*speed:phase;
            }
            else local*=l.Speed;
            local=l.Loop?local%l.Duration:Math.Min(local,l.Duration);
            if(!sources.TryGetValue(l.ImageData!,out var source)){source=new(l.ImageData!);sources.Add(l.ImageData!,source);}
            int index=0;if(source.Delays.Length>1){double phase=local*1000;phase=l.Loop?phase%source.Delays.Sum():Math.Min(phase,source.Delays.Sum()-1);while(index<source.Delays.Length-1&&phase>=source.Delays[index])phase-=source.Delays[index++];}
            var frame=source.Frame(index);var pose=l.PoseAt(local);pose.Opacity*=opacity;var stateGraphics=g.Save();
            selectedFrames.Add(index);
            try{g.TranslateTransform(pose.X,pose.Y);g.RotateTransform(pose.Rotation);g.ScaleTransform(pose.Scale,pose.Scale);using var attrs=new ImageAttributes();attrs.SetColorMatrix(new ColorMatrix{Matrix33=pose.Opacity});g.DrawImage(frame,new Rectangle(-l.Width/2,-l.Height/2,l.Width,l.Height),0,0,frame.Width,frame.Height,GraphicsUnit.Pixel,attrs);}finally{g.Restore(stateGraphics);}
        }
        CurrentFrames=selectedFrames.ToArray();
    }
    public void Dispose(){foreach(var s in sources.Values)s.Dispose();sources.Clear();}
}

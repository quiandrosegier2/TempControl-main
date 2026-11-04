using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

sealed class MotionStudioWindow : Form
{
    public ScreenDesign Project {get;}
    readonly DesignRenderer renderer=new();
    readonly PictureBox preview=new StudioPreview{Dock=DockStyle.Fill};
    readonly ListBox layers=new(){Dock=DockStyle.Fill,BackColor=StudioTheme.Surface,ForeColor=Color.White,BorderStyle=BorderStyle.None};
    readonly PropertyGrid properties=new(){Dock=DockStyle.Right,Width=280,ToolbarVisible=false,ViewBackColor=StudioTheme.Surface,ViewForeColor=Color.White,HelpBackColor=StudioTheme.Background,HelpForeColor=Color.Silver};
    readonly TrackBar timeline=new(){Dock=DockStyle.Bottom,Minimum=0,Maximum=5000,TickFrequency=1000,Height=55};
    readonly NumericUpDown duration=new(){Minimum=1,Maximum=3600,Value=5,DecimalPlaces=1,Width=78};
    readonly Label info=new(){Dock=DockStyle.Bottom,Height=42,ForeColor=Color.Silver};
    readonly CheckBox loop=new(){Text="Herhalen",Checked=true,AutoSize=true};
    readonly System.Windows.Forms.Timer timer=new(){Interval=83};
    readonly Stopwatch playback=new();
    readonly Func<ScreenReadings> readings;
    double position;
    bool syncing;
    CancellationTokenSource? conversion;
    readonly CheckBox guide=new(){Text="Veilige zone",AutoSize=true};
    readonly CheckBox sensors=new(){Text="Sensoren",Checked=true,AutoSize=true};
    readonly CheckBox animate=new(){Text="Animatie",Checked=true,AutoSize=true};
    readonly CheckBox devicePace=new(){Text="Schermsnelheid",Checked=true,AutoSize=true};
    readonly CheckBox performanceOverlay=new(){Text="Prestaties",AutoSize=true};
    readonly Func<AnimationTransferMetrics?>? performance;
    AnimationTransferMetrics? measured;
    DateTime nextPerformance;
    MotionLayer? Selected=>layers.SelectedItem as MotionLayer;
    public MotionStudioWindow(ScreenDesign design,Func<ScreenReadings> readings,Func<AnimationTransferMetrics?>? performance=null)
    {
        Project=DesignSnapshot.Capture(design).Restore();this.readings=readings;this.performance=performance;
        Text="Qscreen / Motion Studio";Size=new(1160,810);MinimumSize=new(1000,700);StartPosition=FormStartPosition.CenterParent;
        BackColor=StudioTheme.Background;ForeColor=Color.White;Font=new("Segoe UI",10);
        HandleCreated+=(_,_)=>StudioTheme.DarkTitle(this);
        var tools=new FlowLayoutPanel{Dock=DockStyle.Top,Height=100,Padding=new(12),AutoScroll=true,AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,MinimumSize=new Size(0,100)};
        void Button(string text,Action action){var b=StudioTheme.Button(text);b.Width=text.Length>19?170:132;b.Height=34;b.Font=new Font("Segoe UI",9,FontStyle.Bold);b.Click+=(_,_)=>Safe(action);tools.Controls.Add(b);}
        Button("Importeer laag",Import);Button("Video / WebP / APNG",()=>ConvertClip());Button("PNG-reeks",ImportSequence);Button("Sprite sheet",ImportSprites);Button("Afspelen",()=>{if(playback.IsRunning)position=Current;playback.Restart();});Button("Pauze",()=>{position=Current;playback.Reset();Draw();});Button("Stop",()=>{position=0;playback.Reset();Draw();});
        Button("Vergrendel / vrij",()=>{if(Selected is {} l){l.Locked=!l.Locked;RefreshLayers();}});Button("Keyframes...",EditKeyframes);Button("Sensorreactie...",EditReactive);Button("Keyframe +",Keyframe);Button("Keyframe −",()=>{if(Selected is {Locked:false} l)l.Keyframes.RemoveAll(k=>Math.Abs(k.Seconds-Current)<.1);});
        Button("Naar voren",()=>MoveLayer(1));Button("Naar achter",()=>MoveLayer(-1));Button("Verwijder laag",()=>{if(Selected is {Locked:false} l){Project.MotionLayers.Remove(l);RefreshLayers();}});
        Button("Bewaar wijzigingen",()=>{MotionLayer.Validate(Project.MotionLayers);DialogResult=DialogResult.OK;Close();});
        Button("Conversie annuleren",()=>conversion?.Cancel());
        Button("Ghost Cute voorbeeld",()=>{using var source=typeof(MotionStudioWindow).Assembly.GetManifestResourceStream("Qscreen.motion.ghost-cute.gif")??throw new IOException("Voorbeeld ontbreekt.");using var memory=new MemoryStream();source.CopyTo(memory);byte[] bytes=memory.ToArray();var gif=GifAnimation.Inspect(bytes);var layer=new MotionLayer{Name="Ghost Cute",ImageData=Convert.ToBase64String(bytes),Duration=gif.DurationMs/1000d};MotionLayer.Validate(Project.MotionLayers.Concat([layer]).ToList());Project.MotionLayers.Add(layer);RefreshLayers();layers.SelectedItem=layer;duration.Value=(decimal)Math.Max(1,layer.Duration);});
        Button("Mijn animaties",()=>{using var library=new MotionLibraryWindow();if(library.ShowDialog(this)!=DialogResult.OK||library.SelectedPath==null)return;byte[] bytes=File.ReadAllBytes(library.SelectedPath);if(MotionAsset.IsAsset(bytes))AddAsset(bytes,Path.GetFileNameWithoutExtension(library.SelectedPath));else{var infoGif=GifAnimation.Inspect(bytes);var l=new MotionLayer{Name=Path.GetFileNameWithoutExtension(library.SelectedPath),ImageData=Convert.ToBase64String(bytes),Duration=infoGif.DurationMs/1000d};MotionLayer.Validate(Project.MotionLayers.Concat([l]).ToList());Project.MotionLayers.Add(l);RefreshLayers();layers.SelectedItem=l;}});
        tools.Controls.Add(new Label{Text="Tijdlijn (s)",AutoSize=true,Padding=new(0,8,0,0)});tools.Controls.Add(duration);tools.Controls.Add(loop);
        tools.Controls.Add(guide);tools.Controls.Add(sensors);tools.Controls.Add(animate);
        tools.Controls.Add(devicePace);tools.Controls.Add(performanceOverlay);
        StudioTheme.Input(duration);StudioTheme.Input(timeline);
        duration.ValueChanged+=(_,_)=>{timeline.Maximum=(int)(duration.Value*1000);position=Math.Min(position,(double)duration.Value);Draw();};
        var sidebar=new Panel{Dock=DockStyle.Left,Width=210,Padding=new(12)};sidebar.Controls.Add(layers);
        layers.SelectedIndexChanged+=(_,_)=>{properties.SelectedObject=Selected;properties.Enabled=Selected?.Locked!=true;};
        properties.PropertyValueChanged+=(_,e)=>Safe(()=>{try{MotionLayer.Validate(Project.MotionLayers);}catch{e.ChangedItem?.PropertyDescriptor?.SetValue(Selected,e.OldValue);properties.Refresh();throw;}RefreshLayers();Draw();});
        timeline.Scroll+=(_,_)=>{if(syncing)return;position=timeline.Value/1000d;playback.Reset();Draw();};
        Controls.Add(preview);Controls.Add(properties);Controls.Add(sidebar);Controls.Add(timeline);Controls.Add(info);Controls.Add(tools);
        timer.Tick+=(_,_)=>Safe(Draw);timer.Start();RefreshLayers();Draw();
        FormClosing+=(_,_)=>conversion?.Cancel();
        FormClosed+=(_,_)=>{timer.Stop();timer.Dispose();renderer.Dispose();preview.Image?.Dispose();};
    }
    double Current {get{double value=position+playback.Elapsed.TotalSeconds;double end=(double)duration.Value;return loop.Checked?value%end:Math.Min(value,end);}}
    void Safe(Action action){try{action();}catch(Exception ex)when(ex is IOException or ArgumentException or InvalidOperationException or FormatException){info.Text=ex.Message;}}
    void RefreshLayers(){int index=layers.SelectedIndex;layers.Items.Clear();foreach(var l in Project.MotionLayers)layers.Items.Add(l);if(layers.Items.Count>0)layers.SelectedIndex=Math.Clamp(index,0,layers.Items.Count-1);}
    void MoveLayer(int offset){if(Selected is not {Locked:false} l)return;int old=Project.MotionLayers.IndexOf(l),next=Math.Clamp(old+offset,0,Project.MotionLayers.Count-1);Project.MotionLayers.RemoveAt(old);Project.MotionLayers.Insert(next,l);RefreshLayers();layers.SelectedItem=l;}
    void Keyframe()
    {
        if(Selected is not {Locked:false} l)return;
        double t=Math.Round(Current,2);if(t>l.Duration)throw new InvalidOperationException("Keyframe valt buiten de duur van deze laag.");
        l.Keyframes.RemoveAll(k=>Math.Abs(k.Seconds-t)<.01);l.Keyframes.Add(new(){Seconds=t,X=l.X,Y=l.Y,Scale=l.Scale,Rotation=l.Rotation,Opacity=1});l.Keyframes.Sort((a,b)=>a.Seconds.CompareTo(b.Seconds));
        info.Text=$"Keyframe op {t:0.00} s vastgelegd · wijzig positie / schaal en leg het volgende keyframe vast.";
    }
    void EditReactive()
    {
        if(Selected is not {Locked:false} l)return;
        using var window=new Form{Text="Sensorreactie ? "+l.Name,Size=new(520,470),StartPosition=FormStartPosition.CenterParent,BackColor=StudioTheme.Background};
        var copy=System.Text.Json.JsonSerializer.Deserialize<MotionReactiveRule>(System.Text.Json.JsonSerializer.Serialize(l.Reactive,DesignFiles.Json),DesignFiles.Json)!;
        var grid=new PropertyGrid{Dock=DockStyle.Fill,SelectedObject=copy,ToolbarVisible=false};
        var save=StudioTheme.Button("Bewaar sensorregel",true);save.Dock=DockStyle.Bottom;save.Click+=(_,_)=>Safe(()=>{var previous=l.Reactive;l.Reactive=copy;try{MotionLayer.Validate([l]);}catch{l.Reactive=previous;throw;}window.DialogResult=DialogResult.OK;window.Close();});
        window.Controls.Add(grid);window.Controls.Add(save);window.ShowDialog(this);
    }
    void EditKeyframes()
    {
        if(Selected is not {Locked:false} l)return;
        using var window=new Form{Text="Keyframes ? "+l.Name,Size=new(830,430),StartPosition=FormStartPosition.CenterParent,BackColor=StudioTheme.Background};
        var rows=new System.ComponentModel.BindingList<MotionKeyframe>(l.Keyframes.Select(k=>new MotionKeyframe{Seconds=k.Seconds,X=k.X,Y=k.Y,Scale=k.Scale,Rotation=k.Rotation,Opacity=k.Opacity}).ToList());
        var grid=new DataGridView{Dock=DockStyle.Fill,DataSource=rows,AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill,BackgroundColor=StudioTheme.Surface};
        grid.DataError+=(_,e)=>{e.ThrowException=false;};
        var save=StudioTheme.Button("Bewaar keyframes",true);save.Dock=DockStyle.Bottom;save.Click+=(_,_)=>Safe(()=>{grid.EndEdit();var previous=l.Keyframes;l.Keyframes=rows.OrderBy(k=>k.Seconds).ToList();try{MotionLayer.Validate([l]);}catch{l.Keyframes=previous;throw;}window.DialogResult=DialogResult.OK;window.Close();});
        window.Controls.Add(grid);window.Controls.Add(save);window.ShowDialog(this);Draw();
    }
    void Import()
    {
        if(Project.MotionLayers.Count>=12)throw new InvalidOperationException("Maximaal 12 lagen.");
        using var dialog=new OpenFileDialog{Filter="GIF / PNG / Qscreen animatie|*.gif;*.png;*.qanim"};if(dialog.ShowDialog(this)!=DialogResult.OK)return;
        var file=new FileInfo(dialog.FileName);if(file.Length>24_000_000)throw new InvalidDataException("Bron groter dan 24 MB.");
        byte[] bytes=File.ReadAllBytes(file.FullName);double seconds=5;
        if(MotionAsset.IsAsset(bytes)){seconds=MotionAsset.Inspect(bytes).DelaysMs.Sum()/1000d;}
        else if(GifAnimation.IsGif(bytes)){var gif=GifAnimation.Inspect(bytes);seconds=gif.DurationMs/1000d;GifAnimation.CachePersonal(bytes);}
        else{if(MotionAsset.IsApng(bytes))throw new InvalidDataException("Importeer APNG via Video / WebP / APNG om alle beelden te behouden.");if(bytes.Length<8||!bytes.AsSpan(0,8).SequenceEqual(new byte[]{137,80,78,71,13,10,26,10}))throw new InvalidDataException("Verwacht GIF of PNG.");using var stream=new MemoryStream(bytes);using var image=Image.FromStream(stream);if(image.Width>2048||image.Height>2048)throw new InvalidDataException("Maximaal 2048 × 2048.");}
        var layer=new MotionLayer{Name=Path.GetFileNameWithoutExtension(file.Name),ImageData=Convert.ToBase64String(bytes),Duration=Math.Max(.02,seconds)};
        MotionLayer.Validate([layer]);Project.MotionLayers.Add(layer);RefreshLayers();layers.SelectedItem=layer;duration.Value=(decimal)Math.Max(1,seconds);Draw();
    }
    void AddAsset(byte[] bytes,string name)
    {
        if(Project.MotionLayers.Count>=12)throw new InvalidOperationException("Maximaal 12 lagen.");
        var asset=MotionAsset.Inspect(bytes);var l=new MotionLayer{Name=name,ImageData=Convert.ToBase64String(bytes),Width=asset.Width,Height=asset.Height,Duration=asset.DelaysMs.Sum()/1000d};
        MotionLayer.Validate(Project.MotionLayers.Concat([l]).ToList());Project.MotionLayers.Add(l);RefreshLayers();layers.SelectedItem=l;duration.Value=(decimal)Math.Max(1,l.Duration);Draw();
        string root=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"QPRINTS","Qscreen","Animations");Directory.CreateDirectory(root);string path=Path.Combine(root,Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes))+".qanim");if(!File.Exists(path))File.WriteAllBytes(path,bytes);
    }
    async void ConvertClip()
    {
        if(conversion!=null)return;
        using var file=new OpenFileDialog{Filter="Video / GIF / WebP / APNG|*.mp4;*.webm;*.webp;*.apng;*.png;*.gif"};if(file.ShowDialog(this)!=DialogResult.OK)return;
        using var options=new Form{Text="Clip converteren",Size=new(460,230),StartPosition=FormStartPosition.CenterParent,BackColor=StudioTheme.Background,ForeColor=Color.White};
        var panel=new FlowLayoutPanel{Dock=DockStyle.Fill,Padding=new(16)};var seconds=new NumericUpDown{Minimum=1,Maximum=25,Value=5,Width=90};var fps=new NumericUpDown{Minimum=1,Maximum=25,Value=12,Width=90};
        panel.Controls.Add(new Label{Text="Eerste seconden",AutoSize=true});panel.Controls.Add(seconds);panel.Controls.Add(new Label{Text="Bronbeelden/s",AutoSize=true});panel.Controls.Add(fps);panel.Controls.Add(new Label{Text="Max. 300 beelden. Dit is de gekozen clipduur; USB-snelheid wordt afzonderlijk gemeten.",AutoSize=false,Width=390,Height=50});
        var convert=StudioTheme.Button("Converteren",true);convert.Click+=(_,_)=>{if(seconds.Value*fps.Value>300){info.Text="Maximaal 300 beelden.";return;}options.DialogResult=DialogResult.OK;options.Close();};panel.Controls.Add(convert);options.Controls.Add(panel);if(options.ShowDialog(this)!=DialogResult.OK)return;
        conversion=new();UseWaitCursor=true;info.Text="Clip wordt op Windows geconverteerd...";
        try{string converter=Path.Combine(AppContext.BaseDirectory,"tools","ffmpeg.exe");var bytes=await MotionAsset.ConvertVideo(file.FileName,converter,(int)seconds.Value,(int)fps.Value,conversion.Token);if(IsDisposed)return;AddAsset(bytes,Path.GetFileNameWithoutExtension(file.FileName));info.Text="Clip geconverteerd en aan je persoonlijke bibliotheek toegevoegd.";}
        catch(Exception ex)when(ex is IOException or InvalidOperationException or OperationCanceledException){info.Text=ex.Message;}
        finally{conversion.Dispose();conversion=null;if(!IsDisposed)UseWaitCursor=false;}
    }
    void ImportSequence()
    {
        using var files=new OpenFileDialog{Filter="PNG-beelden|*.png",Multiselect=true};if(files.ShowDialog(this)!=DialogResult.OK)return;
        AddAsset(MotionAsset.Pack(files.FileNames.Order(StringComparer.OrdinalIgnoreCase).ToArray(),100),"PNG-reeks (10 beelden/s)");
    }
    void ImportSprites()
    {
        using var file=new OpenFileDialog{Filter="PNG sprite sheet|*.png"};if(file.ShowDialog(this)!=DialogResult.OK)return;
        if(new FileInfo(file.FileName).Length>16_000_000)throw new InvalidDataException("Sprite sheet te groot.");
        using var sheet=Image.FromFile(file.FileName);if(sheet.Width>8192||sheet.Height>8192||(long)sheet.Width*sheet.Height>16_000_000)throw new InvalidDataException("Sprite sheet maximaal 16 miljoen pixels.");
        using var options=new Form{Text="Sprite-raster (links naar rechts, boven naar onder)",Size=new(440,190),StartPosition=FormStartPosition.CenterParent};var panel=new FlowLayoutPanel{Dock=DockStyle.Fill};var columns=new NumericUpDown{Minimum=1,Maximum=30,Value=4};var rows=new NumericUpDown{Minimum=1,Maximum=30,Value=4};panel.Controls.Add(new Label{Text="Kolommen"});panel.Controls.Add(columns);panel.Controls.Add(new Label{Text="Rijen"});panel.Controls.Add(rows);var ok=new Button{Text="Importeer",DialogResult=DialogResult.OK};panel.Controls.Add(ok);options.Controls.Add(panel);if(options.ShowDialog(this)!=DialogResult.OK)return;
        int c=(int)columns.Value,r=(int)rows.Value;if(c*r>300||sheet.Width%c!=0||sheet.Height%r!=0)throw new InvalidDataException("Raster moet exact passen; maximaal 300 sprites.");int w=sheet.Width/c,h=sheet.Height/r;if(w>412||h>412)throw new InvalidDataException("Sprite maximaal 412 × 412.");
        string temporary=Path.Combine(Path.GetTempPath(),"Qscreen-Sprites-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(temporary);
        try{var paths=new List<string>();for(int i=0;i<c*r;i++){using var frame=new Bitmap(w,h);using(var g=Graphics.FromImage(frame))g.DrawImage(sheet,new Rectangle(0,0,w,h),new Rectangle(i%c*w,i/c*h,w,h),GraphicsUnit.Pixel);string path=Path.Combine(temporary,$"{i:D4}.png");frame.Save(path,ImageFormat.Png);paths.Add(path);}AddAsset(MotionAsset.Pack(paths,100),Path.GetFileNameWithoutExtension(file.FileName));}finally{Directory.Delete(temporary,true);}
    }
    void Draw()
    {
        if(DateTime.UtcNow>=nextPerformance){measured=performance?.Invoke();if(measured!=null&&DateTime.UtcNow-measured.UpdatedUtc>TimeSpan.FromSeconds(10))measured=null;nextPerformance=DateTime.UtcNow.AddSeconds(1);}
        timer.Interval=devicePace.Checked&&measured?.AcknowledgedFps is >0?Math.Clamp((int)(1000/measured.AcknowledgedFps.Value),40,2000):83;
        if(performanceOverlay.Checked){info.Height=110;info.Text=measured?.Description??"Nog geen actuele meting van het scherm. De editor gebruikt 12 Hz; USB-prestaties zijn onbekend.";}else info.Height=42;
        double t=animate.Checked?Current:0;renderer.MotionTimeSeconds=t;renderer.ReactivePreview=playback.IsRunning&&sensors.Checked&&animate.Checked;var live=readings();if(!sensors.Checked)live=new(null,null,null,null,null,null,live.Now);var next=renderer.Render(Project,live);
        if(guide.Checked){using var g=Graphics.FromImage(next);using var pen=new Pen(Color.FromArgb(190,255,40,110),1){DashStyle=System.Drawing.Drawing2D.DashStyle.Dash};g.DrawEllipse(pen,24,24,364,364);g.DrawLine(pen,196,206,216,206);g.DrawLine(pen,206,196,206,216);}
        var old=preview.Image;preview.Image=next;old?.Dispose();
        syncing=true;timeline.Value=Math.Clamp((int)(t*1000),0,timeline.Maximum);syncing=false;
        Text=$"Qscreen / Motion Studio · {t:0.00} s · {Project.MotionLayers.Count} lagen · voorbeeld {1000d/timer.Interval:0.#} Hz"+(measured==null?" · USB nog onbekend":" · USB gemeten");
    }
}

using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Text.Json;
using System.Windows.Forms;

sealed class MonitorPreferences
{
    public List<string> Order {get;set;}=["cpu","gpu","ram","gaming","uptime","network","disks","fans","board","storage","cpu-power","gpu-power"];
    public List<string> Hidden {get;set;}=["cpu-power","gpu-power"];
    public List<WidgetKind> Graphs {get;set;}=[WidgetKind.CpuTemperature,WidgetKind.CpuLoad,WidgetKind.MemoryLoad,WidgetKind.Fps];
    public int Minutes {get;set;}=5;
    public bool Compare {get;set;}=true;
}
sealed record MonitorCardContent(string Title,string Model,string Value,string Unit,List<(string Label,string Value)> Rows,float? Progress,HistorySeries? Spark,Color Accent);
static class MonitorFormat
{
    public static readonly Color Background=ColorTranslator.FromHtml("#10131B"),Card=ColorTranslator.FromHtml("#1C2230"),Muted=ColorTranslator.FromHtml("#9CA8BE"),Pink=ColorTranslator.FromHtml("#F05B79"),Blue=ColorTranslator.FromHtml("#43C8EA");
    public static string Value(float? value,string unit="",string format="0.#")=>value.HasValue&&float.IsFinite(value.Value)?value.Value.ToString(format)+(unit.Length>0?" "+unit:""):"Niet beschikbaar";
    public static string Unit(WidgetKind kind)=>kind is WidgetKind.CpuTemperature or WidgetKind.GpuTemperature?"°C":kind==WidgetKind.Fps?"FPS":kind==WidgetKind.FrameTime?"ms":"%";
    public static string Title(WidgetKind kind)=>kind switch{WidgetKind.CpuLoad=>"CPU-belasting",WidgetKind.GpuLoad=>"GPU-belasting",WidgetKind.MemoryLoad=>"RAM-gebruik",WidgetKind.FrameTime=>"Frametijd",_=>WidgetLabels.Title(kind)};
    public static readonly WidgetKind[] Metrics=[WidgetKind.CpuTemperature,WidgetKind.GpuTemperature,WidgetKind.CpuLoad,WidgetKind.GpuLoad,WidgetKind.MemoryLoad,WidgetKind.Fps,WidgetKind.FrameTime];
    public static Color ColorFor(WidgetKind kind)=>kind is WidgetKind.GpuTemperature or WidgetKind.GpuLoad?Blue:kind==WidgetKind.MemoryLoad?Color.FromArgb(166,139,250):kind==WidgetKind.Fps?Color.FromArgb(89,218,174):Pink;
}
sealed class MonitorCard : Control
{
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public MonitorCardContent Content {get;set;}=new("","","","",[],null,null,MonitorFormat.Pink);
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public bool Compact {get;init;}
    float progress;
    public MonitorCard(){DoubleBuffered=true;BackColor=MonitorFormat.Background;AccessibleRole=AccessibleRole.Grouping;}
    public void Animate(){float target=Math.Clamp(Content.Progress??0,0,100);if(Math.Abs(target-progress)>.1){progress+=(target-progress)*.22f;Invalidate();}}
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);float scale=DeviceDpi/96f;var g=e.Graphics;g.ScaleTransform(scale,scale);g.SmoothingMode=SmoothingMode.AntiAlias;g.TextRenderingHint=TextRenderingHint.AntiAliasGridFit;float width=Width/scale,height=Height/scale;
        using var shape=StudioTheme.Rounded(new RectangleF(0,0,width-1,height-1),16);using var fill=new SolidBrush(MonitorFormat.Card);g.FillPath(fill,shape);using var edge=new Pen(Color.FromArgb(49,58,78));g.DrawPath(edge,shape);
        void Text(string text,float x,float y,float w,float h,float size,Color color,bool bold=false){using var font=new Font("Segoe UI",size,bold?FontStyle.Bold:FontStyle.Regular,GraphicsUnit.Pixel);using var brush=new SolidBrush(color);using var format=new StringFormat{Trimming=StringTrimming.EllipsisCharacter};g.DrawString(text,font,brush,new RectangleF(x,y,w,h),format);}
        var data=Content;Text(data.Title,18,16,width-36,23,13,data.Accent,true);
        if(Compact){Text(data.Model,18,42,width-36,20,11,MonitorFormat.Muted);Text(data.Value,18,65,width-36,30,22,Color.White,true);return;}
        Text(data.Model,18,43,width-36,38,13,MonitorFormat.Muted);
        Text(data.Value,16,82,width-82,56,data.Value.Length>12?19:42,Color.White,true);Text(data.Unit,width-64,103,49,26,14,data.Accent,true);
        using(var track=new SolidBrush(Color.FromArgb(40,49,68)))g.FillRectangle(track,18,145,width-36,4);
        if(data.Progress.HasValue){using var bar=new SolidBrush(data.Accent);g.FillRectangle(bar,18,145,(width-36)*progress/100,4);}
        float row=163;foreach(var item in data.Rows.Take(6)){Text(item.Label,18,row,width*.47f-18,22,12,MonitorFormat.Muted);Text(item.Value,width*.47f,row,width*.53f-18,22,13,Color.White,true);row+=23;}
        if(data.Spark is {} spark){using var clip=new Region(new RectangleF(18,height-61,width-36,34));var state=g.Save();g.SetClip(clip,CombineMode.Replace);SensorHistory.Draw(g,new(18,height-58,width-36,29),spark,data.Accent,false);g.Restore(state);}
        Text(data.Spark==null?"Wachten op historie":"LIVE · laatste minuut",18,height-19,width-36,16,9,MonitorFormat.Muted);
    }
}
sealed class MonitorHistoryChart : Control
{
    public WidgetKind Metric;
    public float? Current;
    public List<(WidgetKind Metric,HistorySeries Series)> Series=[];
    readonly ToolTip tip=new();
    public MonitorHistoryChart(){DoubleBuffered=true;BackColor=MonitorFormat.Background;MouseMove+=Hover;MouseLeave+=(_,_)=>tip.Hide(this);}
    RectangleF Plot=>new(52,78,Math.Max(1,Width-73),Math.Max(1,Height-132));
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;using var fill=new SolidBrush(MonitorFormat.Card);using var path=StudioTheme.Rounded(new RectangleF(0,0,Width-1,Height-1),14);g.FillPath(fill,path);
        using var titleFont=new Font("Segoe UI",11,FontStyle.Bold);using var small=new Font("Segoe UI",8);using var white=new SolidBrush(Color.White);using var muted=new SolidBrush(MonitorFormat.Muted);
        g.DrawString(MonitorFormat.Title(Metric),titleFont,white,16,12);
        var primary=Series.FirstOrDefault(s=>s.Metric==Metric).Series;string unit=MonitorFormat.Unit(Metric);
        string stats=primary?.Minimum==null?"Nog geen meetgegevens":$"Nu {MonitorFormat.Value(Current,unit)}  ·  min {primary.Minimum:0.#}  ·  max {primary.Maximum:0.#}  ·  gem. {primary.Average:0.#} {unit}";
        g.DrawString(stats,small,muted,new RectangleF(16,38,Width-28,24));
        float minimum=Series.Where(s=>s.Series.Minimum.HasValue).Select(s=>s.Series.Minimum!.Value).DefaultIfEmpty(0).Min(),maximum=Series.Where(s=>s.Series.Maximum.HasValue).Select(s=>s.Series.Maximum!.Value).DefaultIfEmpty(100).Max();float pad=Math.Max(1,(maximum-minimum)*.12f);minimum=Math.Max(0,minimum-pad);maximum+=pad;
        var plot=Plot;using var grid=new Pen(Color.FromArgb(46,56,76));for(int i=0;i<4;i++){float y=plot.Top+i*plot.Height/3;g.DrawLine(grid,plot.Left,y,plot.Right,y);g.DrawString((maximum-i*(maximum-minimum)/3).ToString("0.#"),small,muted,3,y-5);}
        float legend=16;
        foreach(var entry in Series)
        {
            Color color=MonitorFormat.ColorFor(entry.Metric);using var line=new Pen(color,2);PointF? previous=null;
            foreach(var point in entry.Series.Points){if(!point.Value.HasValue){previous=null;continue;}float x=plot.Left+(float)((point.Time-entry.Series.From).TotalSeconds/Math.Max(1,(entry.Series.To-entry.Series.From).TotalSeconds))*plot.Width;float y=plot.Bottom-(point.Value.Value-minimum)/(maximum-minimum)*plot.Height;var next=new PointF(x,y);if(previous is {} prior)g.DrawLine(line,prior,next);previous=next;}
            using var brush=new SolidBrush(color);string label=MonitorFormat.Title(entry.Metric);g.DrawString(label,small,brush,legend,Height-27);legend+=g.MeasureString(label,small).Width+18;
        }
        if(primary!=null)g.DrawString(primary.From.ToString("HH:mm")+" — "+primary.To.ToString("HH:mm"),small,muted,plot.Left,plot.Bottom+3);
    }
    void Hover(object? sender,MouseEventArgs e)
    {
        if(!Plot.Contains(e.Location)||Series.Count==0)return;var lines=new List<string>();foreach(var entry in Series){DateTime time=entry.Series.From.AddSeconds((e.X-Plot.Left)/Plot.Width*(entry.Series.To-entry.Series.From).TotalSeconds);var point=entry.Series.Points.MinBy(p=>Math.Abs((p.Time-time).TotalSeconds));if(point!=null)lines.Add($"{point.Time:HH:mm:ss} · {MonitorFormat.Title(entry.Metric)}: {MonitorFormat.Value(point.Value,MonitorFormat.Unit(entry.Metric))}");}tip.SetToolTip(this,string.Join("\n",lines));
    }
    protected override void Dispose(bool disposing){if(disposing)tip.Dispose();base.Dispose(disposing);}
}

sealed class MonitorDashboard : UserControl
{
    readonly string directory,preferencesFile;
    readonly Panel scroll=new(){Dock=DockStyle.Fill,AutoScroll=true};
    readonly Panel surface=new();
    readonly Label source=new(){Dock=DockStyle.Top,Height=38,ForeColor=MonitorFormat.Muted,Padding=new(12,8,12,0)};
    readonly FlowLayoutPanel tools=new(){Dock=DockStyle.Top,AutoSize=true,Padding=new(8),WrapContents=true};
    readonly Dictionary<string,MonitorCard> cards=[];
    readonly Dictionary<WidgetKind,MonitorHistoryChart> charts=[];
    readonly SensorHistory local=new();
    readonly System.Windows.Forms.Timer timer=new(){Interval=1000},animation=new(){Interval=50};
    readonly ToolTip tip=new();
    MonitorPreferences preferences=new();
    DateTime lastSample,nextRequest;
    DashboardHistorySnapshot? persisted;
    public MonitorDashboard(string directory,string? preferencesFile=null,Action<int>? samplingChanged=null)
    {
        this.directory=directory;this.preferencesFile=preferencesFile??Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"QPRINTS","Qscreen","monitor-dashboard.json");
        Dock=DockStyle.Fill;BackColor=MonitorFormat.Background;Font=new("Segoe UI",10);AutoScaleMode=AutoScaleMode.Dpi;
        try{if(File.Exists(this.preferencesFile)&&new FileInfo(this.preferencesFile).Length<16384)preferences=JsonSerializer.Deserialize<MonitorPreferences>(File.ReadAllText(this.preferencesFile),DesignFiles.Json)??new();}catch(Exception ex)when(ex is IOException or JsonException){}
        if(preferences.Order==null||preferences.Hidden==null||preferences.Graphs==null)preferences=new();
        preferences.Order=preferences.Order.Intersect(new MonitorPreferences().Order).Concat(new MonitorPreferences().Order).Distinct().ToList();preferences.Graphs=preferences.Graphs.Intersect(MonitorFormat.Metrics).ToList();if(!new[]{1,5,15,30,60}.Contains(preferences.Minutes))preferences.Minutes=5;
        var window=new ComboBox{Width=170,DropDownStyle=ComboBoxStyle.DropDownList};window.Items.AddRange(new object[]{1,5,15,30,60});window.SelectedItem=preferences.Minutes;window.FormattingEnabled=true;window.Format+=(_,e)=>e.Value="Laatste "+e.ListItem+" minuten";StudioTheme.Input(window);tools.Controls.Add(window);window.SelectedIndexChanged+=(_,_)=>{preferences.Minutes=(int)window.SelectedItem!;nextRequest=default;Save();RefreshNow();};
        var sampling=new NumericUpDown{Minimum=1,Maximum=60,Value=Math.Clamp(StudioConfiguration.Load(directory).SamplingSeconds,1,60),Width=65};StudioTheme.Input(sampling);
        tools.Controls.Add(new Label{Text="Historie-interval (s)",AutoSize=true,ForeColor=MonitorFormat.Muted,Padding=new(4,8,0,0)});tools.Controls.Add(sampling);
        sampling.ValueChanged+=(_,_)=>{if(samplingChanged!=null)samplingChanged((int)sampling.Value);else{var config=StudioConfiguration.Load(directory);config.SamplingSeconds=(int)sampling.Value;config.Save(directory);}};
        var start=StudioTheme.Button("Metingen starten");start.Width=160;start.Height=34;start.Click+=(_,_)=>{try{System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("powershell.exe","-NoProfile -Command \"Start-ScheduledTask -TaskName TempMonitor -ErrorAction Stop\""){UseShellExecute=false,CreateNoWindow=true});}catch(Exception ex)when(ex is InvalidOperationException or System.ComponentModel.Win32Exception){source.Text=ex.Message;}};tools.Controls.Add(start);
        void Menu(string text,Action<ContextMenuStrip> fill){var button=StudioTheme.Button(text);button.Width=150;button.Height=34;button.Click+=(_,_)=>{var menu=new ContextMenuStrip{BackColor=MonitorFormat.Card,ForeColor=Color.White};fill(menu);menu.Closed+=(_,_)=>menu.Dispose();menu.Show(button,0,button.Height);};tools.Controls.Add(button);}
        Menu("Grafieken",menu=>{foreach(var metric in MonitorFormat.Metrics){var item=new ToolStripMenuItem(MonitorFormat.Title(metric)){Checked=preferences.Graphs.Contains(metric),CheckOnClick=true};item.CheckedChanged+=(_,_)=>{if(item.Checked)preferences.Graphs.Add(metric);else preferences.Graphs.Remove(metric);Save();RefreshNow();};menu.Items.Add(item);}});
        Menu("Kaarten",menu=>{foreach(string id in preferences.Order){var item=new ToolStripMenuItem(CardName(id)){Checked=!preferences.Hidden.Contains(id),CheckOnClick=true};item.CheckedChanged+=(_,_)=>{if(item.Checked)preferences.Hidden.Remove(id);else preferences.Hidden.Add(id);Save();LayoutDashboard();};menu.Items.Add(item);}});
        var compare=new CheckBox{Text="Vergelijk gelijke eenheden",Checked=preferences.Compare,AutoSize=true,ForeColor=MonitorFormat.Muted,Margin=new(8,10,0,0)};compare.CheckedChanged+=(_,_)=>{preferences.Compare=compare.Checked;Save();RefreshNow();};tools.Controls.Add(compare);
        foreach(string id in preferences.Order){var card=new MonitorCard{Compact=!new[]{"cpu","gpu","ram","gaming"}.Contains(id),Tag=id};cards[id]=card;surface.Controls.Add(card);var menu=new ContextMenuStrip();menu.Items.Add("Naar voren",null,(_,_)=>MoveCard(id,-1));menu.Items.Add("Naar achteren",null,(_,_)=>MoveCard(id,1));menu.Items.Add("Verbergen",null,(_,_)=>{preferences.Hidden.Add(id);Save();LayoutDashboard();});card.ContextMenuStrip=menu;card.Disposed+=(_,_)=>menu.Dispose();}
        foreach(var metric in MonitorFormat.Metrics){var chart=new MonitorHistoryChart{Metric=metric};charts[metric]=chart;surface.Controls.Add(chart);}
        scroll.Controls.Add(surface);Controls.Add(scroll);Controls.Add(tools);Controls.Add(source);scroll.ClientSizeChanged+=(_,_)=>LayoutDashboard();
        timer.Tick+=(_,_)=>{if(Visible)RefreshNow();};animation.Tick+=(_,_)=>{if(Visible)foreach(var card in cards.Values.Where(c=>c.Visible))card.Animate();};timer.Start();animation.Start();VisibleChanged+=(_,_)=>{if(Visible)RefreshNow();};RefreshNow();
    }
    static string CardName(string id)=>id switch{"cpu"=>"CPU","gpu"=>"GPU","ram"=>"RAM","gaming"=>"Gaming / FPS","uptime"=>"Systeem actief","network"=>"Netwerk","disks"=>"Vrije schijfruimte","fans"=>"Ventilatoren","board"=>"Moederbord","storage"=>"Opslagtemperatuur","cpu-power"=>"CPU-vermogen","gpu-power"=>"GPU-vermogen",_=>id};
    void Save(){try{Directory.CreateDirectory(Path.GetDirectoryName(preferencesFile)!);AtomicJson.Write(preferencesFile,preferences);}catch(Exception ex)when(ex is IOException or UnauthorizedAccessException){source.Text="Indeling niet opgeslagen: "+ex.Message;}}
    void MoveCard(string id,int delta){var group=preferences.Order.Where(key=>cards[key].Compact==cards[id].Compact&&!preferences.Hidden.Contains(key)&&(string?)cards[key].Tag!="unsupported").ToList();int old=group.IndexOf(id);if(old<0)return;int next=Math.Clamp(old+delta,0,group.Count-1);string target=group[next];if(target==id)return;int insertion=preferences.Order.IndexOf(target);preferences.Order.Remove(id);preferences.Order.Insert(insertion,id);Save();LayoutDashboard();}
    public void RefreshNow()
    {
        var snapshot=LiveTelemetry.Read(directory,DateTime.UtcNow);var r=snapshot?.Readings??LiveTelemetry.Unavailable(DateTime.Now);var h=r.Hardware;
        source.Text=snapshot==null?"PC MONITOR · Geen recente meting. Start de achtergrondmeting.":"PC MONITOR  /  LIVE · dezelfde meetbron als je Qscreen · "+snapshot.UpdatedUtc.ToLocalTime().ToString("HH:mm:ss");
        if(snapshot!=null&&snapshot.UpdatedUtc!=lastSample){local.Add(r);lastSample=snapshot.UpdatedUtc;}
        persisted=null;
        try{if(DateTime.UtcNow>=nextRequest){AtomicJson.Write(Path.Combine(directory,"dashboard-history-request.json"),new DashboardHistoryRequest(preferences.Minutes,DateTime.UtcNow));nextRequest=DateTime.UtcNow.AddSeconds(5);}string path=Path.Combine(directory,"dashboard-history.json");if(File.Exists(path)&&new FileInfo(path).Length<2_000_000){var data=JsonSerializer.Deserialize<DashboardHistorySnapshot>(File.ReadAllText(path),DesignFiles.Json);persisted=data?.Series!=null&&data.Minutes==preferences.Minutes&&DateTime.UtcNow-data.UpdatedUtc<TimeSpan.FromSeconds(15)?data:null;}}catch(Exception ex)when(ex is IOException or JsonException or UnauthorizedAccessException){persisted=null;}
        HistorySeries Series(WidgetKind metric)=>snapshot==null?new(DateTime.Now.AddMinutes(-preferences.Minutes),DateTime.Now,[],null,null,null):persisted?.Series.GetValueOrDefault(metric)??local.Window(metric,preferences.Minutes,DateTime.Now,240);
        string F(float? value,string unit="",string format="0.#")=>MonitorFormat.Value(value,unit,format);
        void Set(string id,string model,float? primary,string unit,List<(string,string)> rows,float? progress,WidgetKind spark){var card=cards[id];card.Content=new(CardName(id),model,primary.HasValue?F(primary):"Niet beschikbaar",unit,rows,progress,snapshot==null?null:persisted?.Recent?.GetValueOrDefault(spark)??local.Window(spark,1,DateTime.Now),id=="gpu"?MonitorFormat.Blue:id=="ram"?Color.FromArgb(166,139,250):id=="gaming"?Color.FromArgb(89,218,174):MonitorFormat.Pink);card.AccessibleName=CardName(id)+": "+card.Content.Value+" "+unit;card.AccessibleDescription=model+"\n"+string.Join("\n",rows.Select(v=>v.Item1+": "+v.Item2));tip.SetToolTip(card,card.AccessibleDescription);card.Invalidate();}
        var cpu=new List<(string,string)>{("Belasting",F(r.CpuLoad,"%")),("Kloksnelheid",F(r.CpuClock/1000,"GHz","0.00")),("Sessiemaximum",F(h?.CpuSessionMax,"°C"))};if(h?.CpuPowerWatts is {} cpuPower)cpu.Add(("Package power",F(cpuPower,"W")));
        Set("cpu",h?.CpuModel??"CPU-model niet beschikbaar",r.Cpu,"°C",cpu,r.CpuLoad,WidgetKind.CpuLoad);
        var gpu=new List<(string,string)>{("Belasting",F(r.GpuLoad,"%")),("Core clock",F(r.GpuClock,"MHz")),("Geheugenklok",F(h?.GpuMemoryClockMhz,"MHz")),("VRAM",F(h?.VramUsedGb,"GB")+" / "+F(h?.VramTotalGb,"GB"))};if(h?.GpuPowerWatts is {} gpuPower)gpu.Add(("Vermogen",F(gpuPower,"W")));
        Set("gpu",h?.GpuModel??"GPU-model niet beschikbaar",r.Gpu,"°C",gpu,r.GpuLoad,WidgetKind.GpuLoad);
        Set("ram",h?.RamInstalledGb is {} installed?F(installed,"GB geïnstalleerd"):"Geheugen",r.MemoryLoad,"%",[("Gebruikt",F(h?.RamUsedGb,"GB")),("Beschikbaar",F(h?.RamAvailableGb,"GB")),("Geconfigureerd¹",F(h?.RamConfiguredClockMhz,"MHz"))],r.MemoryLoad,WidgetKind.MemoryLoad);tip.SetToolTip(cards["ram"],"¹ ConfiguredClockSpeed, zoals WMI/SMBIOS rapporteert in MHz; geen verdubbeling of conversie naar MT/s.");
        Set("gaming",h?.FrameProcess is {} process?"Voorgrondproces: "+process:r.Fps.HasValue?"Procesnaam niet beschikbaar":"Geen game gedetecteerd",r.Fps,"FPS",[("Gemiddeld (≤60s)",F(h?.AverageFps,"FPS")),("Frametijd",F(h?.FrameTimeMs,"ms","0.00")),("GPU-belasting",F(r.GpuLoad,"%"))],null,WidgetKind.Fps);
        void Optional(string id,string model,string? value){var card=cards[id];card.Tag=value==null?"unsupported":id;card.Content=new(CardName(id),model,value??"","",[],null,null,MonitorFormat.Blue);tip.SetToolTip(card,model+"\n"+value);card.Invalidate();}
        Optional("uptime","Sinds Windows is gestart",h==null?null:TimeSpan.FromSeconds(h.UptimeSeconds).ToString(@"d\d\ hh\:mm\:ss"));Optional("network",h?.NetworkName??"",h?.ReceiveBytesPerSecond is {} rx&&h.SendBytesPerSecond is {} tx?$"↓ {rx/1000000:0.00}  ↑ {tx/1000000:0.00} MB/s":null);
        void Extra(string id,HardwareExtraReading[]? values){var list=values??[];Optional(id,string.Join(" · ",list.Select(v=>v.Name)),list.Length>0?string.Join(" · ",list.Select(v=>$"{v.Value:0.#} {v.Unit}")):null);}
        Extra("disks",h?.FreeDisks);Extra("fans",h?.Fans);Extra("board",h?.Motherboard);Extra("storage",h?.Storage);Optional("cpu-power",h?.CpuModel??"",h?.CpuPowerWatts.HasValue==true?F(h.CpuPowerWatts,"W"):null);Optional("gpu-power",h?.GpuModel??"",h?.GpuPowerWatts.HasValue==true?F(h.GpuPowerWatts,"W"):null);
        foreach(var pair in charts){var chart=pair.Value;chart.Current=r.Value(pair.Key);chart.Series=[(pair.Key,Series(pair.Key))];if(preferences.Compare){foreach(var other in MonitorFormat.Metrics.Where(m=>m!=pair.Key&&MonitorFormat.Unit(m)==MonitorFormat.Unit(pair.Key)))chart.Series.Add((other,Series(other)));}chart.Invalidate();}
        LayoutDashboard();
    }
    void LayoutDashboard()
    {
        float scale=DeviceDpi/96f;int gap=(int)(14*scale),available=Math.Max(1,scroll.ClientSize.Width-gap*2-SystemInformation.VerticalScrollBarWidth),mainColumns=available/scale>=1080?4:available/scale>=530?2:1;int y=gap;
        surface.SuspendLayout();
        var main=preferences.Order.Where(id=>!cards[id].Compact&&!preferences.Hidden.Contains(id)).ToArray();Place(main.Select(id=>(Control)cards[id]).ToArray(),Math.Min(mainColumns,Math.Max(1,main.Length)),(int)(355*scale));
        var extra=preferences.Order.Where(id=>cards[id].Compact&&!preferences.Hidden.Contains(id)&&(string?)cards[id].Tag!="unsupported").ToArray();
        int extraRows=Math.Max(1,(int)Math.Ceiling(extra.Length/(double)mainColumns));
        int extraColumns=Math.Min(mainColumns,Math.Max(1,(int)Math.Ceiling(extra.Length/(double)extraRows)));
        Place(extra.Select(id=>(Control)cards[id]).ToArray(),extraColumns,(int)(112*scale));
        foreach(var pair in cards)pair.Value.Visible=main.Contains(pair.Key)||extra.Contains(pair.Key);
        var visibleCharts=MonitorFormat.Metrics.Where(preferences.Graphs.Contains).Select(m=>(Control)charts[m]).ToArray();Place(visibleCharts,available/scale>=900?2:1,(int)(255*scale));foreach(var pair in charts)pair.Value.Visible=preferences.Graphs.Contains(pair.Key);
        surface.Size=new(scroll.ClientSize.Width-SystemInformation.VerticalScrollBarWidth,Math.Max(scroll.ClientSize.Height,y));surface.ResumeLayout();
        void Place(Control[] controls,int columns,int height){int width=Math.Max(1,(available-gap*(columns-1))/columns);for(int i=0;i<controls.Length;i++)controls[i].Bounds=new(gap+i%columns*(width+gap),y+i/columns*(height+gap),width,height);if(controls.Length>0)y+=(int)Math.Ceiling(controls.Length/(double)columns)*(height+gap);}
    }
    protected override void Dispose(bool disposing){if(disposing){timer.Dispose();animation.Dispose();tip.Dispose();}base.Dispose(disposing);}
}

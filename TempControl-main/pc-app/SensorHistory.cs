using System.Drawing;

sealed record SensorSample(DateTime Time,IReadOnlyDictionary<WidgetKind,float?> Values);
sealed record HistorySeries(DateTime From,DateTime To,List<HistoryPoint> Points,float? Minimum,float? Maximum,float? Average);
sealed record HistoryPoint(DateTime Time,float? Value);
sealed class SensorHistory
{
    public static readonly WidgetKind[] Metrics=[WidgetKind.CpuTemperature,WidgetKind.GpuTemperature,WidgetKind.CpuLoad,WidgetKind.GpuLoad,WidgetKind.CpuClock,WidgetKind.GpuClock,WidgetKind.MemoryLoad,WidgetKind.Fps,WidgetKind.FrameTime];
    readonly Queue<SensorSample> samples=new();readonly object sync=new();
    public int Count{get{lock(sync)return samples.Count;}}
    public void Add(ScreenReadings readings)
    {
        var values=Metrics.ToDictionary(k=>k,k=>readings.Value(k) is {} v&&float.IsFinite(v)?(float?)v:null);
        lock(sync){samples.Enqueue(new(readings.Now,values));while(samples.Count>3601||samples.Count>0&&readings.Now-samples.Peek().Time>TimeSpan.FromHours(1))samples.Dequeue();}
    }
    public HistorySeries Window(WidgetKind metric,int minutes,DateTime now,int maxPoints=120)
    {
        SensorSample[] snapshot;lock(sync)snapshot=samples.Where(s=>s.Time>=now.AddMinutes(-Math.Clamp(minutes,1,60))&&s.Time<=now).ToArray();
        var valid=snapshot.Select(s=>s.Values.GetValueOrDefault(metric)).Where(v=>v.HasValue).Select(v=>v!.Value).ToArray();
        int stride=Math.Max(1,(int)Math.Ceiling(snapshot.Length/(double)Math.Clamp(maxPoints,2,412)));
        var points=new List<HistoryPoint>();
        for(int i=0;i<snapshot.Length;i+=stride){var bucket=snapshot.Skip(i).Take(stride).ToArray();float? value=bucket.All(s=>s.Values.GetValueOrDefault(metric).HasValue)?bucket.Average(s=>s.Values.GetValueOrDefault(metric)!.Value):null;points.Add(new(bucket[^1].Time,value));}
        return new(now.AddMinutes(-Math.Clamp(minutes,1,60)),now,points,valid.Length==0?null:valid.Min(),valid.Length==0?null:valid.Max(),valid.Length==0?null:valid.Average());
    }
    public static void Draw(Graphics g,RectangleF bounds,HistorySeries series,Color color,bool statistics=true)
    {
        using var grid=new Pen(Color.FromArgb(55,65,85));using var line=new Pen(color,2);
        for(int i=0;i<4;i++)g.DrawLine(grid,bounds.Left,bounds.Top+i*bounds.Height/3,bounds.Right,bounds.Top+i*bounds.Height/3);
        float min=series.Minimum??0,max=series.Maximum??100;if(max-min<1)max=min+1;
        PointF? previous=null;
        foreach(var point in series.Points)
        {
            if(point.Value==null){previous=null;continue;}
            float x=bounds.Left+(float)((point.Time-series.From).TotalSeconds/Math.Max(1,(series.To-series.From).TotalSeconds))*bounds.Width;
            float y=bounds.Bottom-(point.Value.Value-min)/(max-min)*bounds.Height;
            var current=new PointF(x,y);if(previous is {} p)g.DrawLine(line,p,current);previous=current;
        }
        if(statistics){using var font=new Font("Segoe UI",9,FontStyle.Regular,GraphicsUnit.Pixel);using var brush=new SolidBrush(color);string text=series.Minimum is null?"--":$"min {series.Minimum:0.#}   avg {series.Average:0.#}   max {series.Maximum:0.#}";g.DrawString(text,font,brush,bounds.Left,bounds.Bottom+2);}
    }
}

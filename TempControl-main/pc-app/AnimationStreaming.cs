using System.Diagnostics;

enum AnimationQuality { Eco, Balanced, Smooth, Maximum, Ultra }
sealed record AnimationTransferMetrics(double RenderMs,double TransferMs,double? AcknowledgedFps,double BytesPerSecond,long AcknowledgedFrames,long SkippedUpdates,int FrameBytes,double TargetFps,DateTime UpdatedUtc,int? GifFrameIndex=null,uint? FrameHash=null)
{
    public DeviceMeasurements? Device {get;init;}
    public int[]? LayerFrameIndices {get;init;}
    public double? MinimumFps {get;init;}
    public double? AverageFrameMs {get;init;}
    public double? FrameTimeStdDevMs {get;init;}
    public double? P95FrameMs {get;init;}
    public double DecodeMs {get;init;}
    public string RecommendedPreset {get;init;}="Nog niet gemeten";
    public long ReplacedFrames {get;init;}
    public string PacingDescription=>$"Min. {(MinimumFps.HasValue?MinimumFps.Value.ToString("0.##"):"--")} FPS / spreiding {(FrameTimeStdDevMs.HasValue?FrameTimeStdDevMs.Value.ToString("0.#"):"--")} ms / decode {DecodeMs:0.#} ms / vervangen {ReplacedFrames} / advies: {RecommendedPreset}";
    public string Description=>$"USB ACK {(AcknowledgedFps.HasValue?AcknowledgedFps.Value.ToString("0.##"):"--")} beelden/s · render {RenderMs:0.#} ms · overdracht {TransferMs:0.#} ms · {BytesPerSecond/1024:0.#} KB/s\nDoel {TargetFps:0.#} FPS · overgeslagen geplande updates {SkippedUpdates}\n"+PacingDescription+"\n"+(Device?.Description??"ESP-geheugen/render-FPS: niet beschikbaar");
}
sealed class AnimationStreamMeter
{
    readonly Stopwatch clock=Stopwatch.StartNew();long frames,bytes,skipped;double averageWork;
    readonly Queue<double> intervals=new();double? lastAck;
    public AnimationTransferMetrics? Latest {get;private set;}
    public static double Requested(AnimationQuality quality)=>quality switch{AnimationQuality.Eco=>10,AnimationQuality.Smooth=>25,AnimationQuality.Maximum or AnimationQuality.Ultra=>30,_=>15};
    public double EffectiveTarget(AnimationQuality quality)=>averageWork>0?Math.Min(Requested(quality),1000/(averageWork*1.05)):quality==AnimationQuality.Maximum?12:Requested(quality);
    public void Received(double renderMs,double transferMs,int frameBytes,AnimationQuality quality,int? gifFrameIndex=null,uint? frameHash=null,int[]? layerFrames=null,double decodeMs=0,long replacedFrames=0)
    {
        double now=clock.Elapsed.TotalMilliseconds;if(lastAck.HasValue){intervals.Enqueue(now-lastAck.Value);if(intervals.Count>240)intervals.Dequeue();}lastAck=now;
        frames++;bytes+=frameBytes;double work=Math.Max(.1,Math.Max(renderMs,transferMs));averageWork=averageWork==0?work:averageWork*.8+work*.2;
        skipped+=Math.Max(0,(long)Math.Floor(work/(1000/Requested(quality)))-1);
        double seconds=Math.Max(.001,clock.Elapsed.TotalSeconds);
        double[] sorted=intervals.Order().ToArray();double mean=sorted.Length>0?sorted.Average():0;
        Latest=new(renderMs,transferMs,seconds>=1?frames/seconds:null,bytes/seconds,frames,skipped,frameBytes,EffectiveTarget(quality),DateTime.UtcNow,gifFrameIndex,frameHash){LayerFrameIndices=layerFrames,MinimumFps=sorted.Length>0?1000/sorted[^1]:null,AverageFrameMs=mean>0?mean:null,FrameTimeStdDevMs=sorted.Length>0?Math.Sqrt(sorted.Average(v=>(v-mean)*(v-mean))):null,P95FrameMs=sorted.Length>0?sorted[(int)Math.Ceiling(sorted.Length*.95)-1]:null,DecodeMs=decodeMs,ReplacedFrames=replacedFrames,RecommendedPreset=1000/(averageWork*1.1) is var sustainable?sustainable>=30?"ULTRA":sustainable>=25?"SMOOTH":sustainable>=15?"BALANCED":sustainable>=10?"ECO":"ECO (automatisch begrensd onder 10 FPS)":"Nog niet gemeten"};
    }
    public int DelayMilliseconds(AnimationQuality quality,double workMs)=>Math.Clamp((int)(1000/EffectiveTarget(quality)-workMs),0,1000);
}
interface IProgressDisplaySession
{
    bool SendWithProgress(byte[] frame,Action<int> progress);
}

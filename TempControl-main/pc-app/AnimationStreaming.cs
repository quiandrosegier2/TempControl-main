using System.Diagnostics;

enum AnimationQuality { Eco, Balanced, Smooth, Maximum }
sealed record AnimationTransferMetrics(double RenderMs,double TransferMs,double? AcknowledgedFps,double BytesPerSecond,long AcknowledgedFrames,long SkippedUpdates,int FrameBytes,double TargetFps,DateTime UpdatedUtc,int? GifFrameIndex=null,uint? FrameHash=null)
{
    public DeviceMeasurements? Device {get;init;}
    public int[]? LayerFrameIndices {get;init;}
    public string Description=>$"USB ACK {(AcknowledgedFps.HasValue?AcknowledgedFps.Value.ToString("0.##"):"--")} beelden/s · render {RenderMs:0.#} ms · overdracht {TransferMs:0.#} ms · {BytesPerSecond/1024:0.#} KB/s\nDoel {TargetFps:0.#} FPS · overgeslagen geplande updates {SkippedUpdates}\n"+(Device?.Description??"ESP-geheugen/render-FPS: niet beschikbaar");
}
sealed class AnimationStreamMeter
{
    readonly Stopwatch clock=Stopwatch.StartNew();long frames,bytes,skipped;double averageWork;
    public AnimationTransferMetrics? Latest {get;private set;}
    public static double Requested(AnimationQuality quality)=>quality switch{AnimationQuality.Eco=>6,AnimationQuality.Smooth=>20,AnimationQuality.Maximum=>25,_=>12};
    public double EffectiveTarget(AnimationQuality quality)=>averageWork>0?Math.Min(Requested(quality),1000/(averageWork*1.05)):quality==AnimationQuality.Maximum?12:Requested(quality);
    public void Received(double renderMs,double transferMs,int frameBytes,AnimationQuality quality,int? gifFrameIndex=null,uint? frameHash=null,int[]? layerFrames=null)
    {
        frames++;bytes+=frameBytes;double work=Math.Max(.1,renderMs+transferMs);averageWork=averageWork==0?work:averageWork*.8+work*.2;
        skipped+=Math.Max(0,(long)Math.Floor(work/(1000/Requested(quality)))-1);
        double seconds=Math.Max(.001,clock.Elapsed.TotalSeconds);
        Latest=new(renderMs,transferMs,seconds>=1?frames/seconds:null,bytes/seconds,frames,skipped,frameBytes,EffectiveTarget(quality),DateTime.UtcNow,gifFrameIndex,frameHash){LayerFrameIndices=layerFrames};
    }
    public int DelayMilliseconds(AnimationQuality quality,double workMs)=>Math.Clamp((int)(1000/EffectiveTarget(quality)-workMs),0,1000);
}
interface IProgressDisplaySession
{
    bool SendWithProgress(byte[] frame,Action<int> progress);
}

using System.Diagnostics;
using System.Drawing;
using System.Threading.Channels;

// One compositor owns the renderer. USB never runs on this task; only the newest
// completed image is retained while the independent transport task is busy.
sealed record ComposedFrame(long Request,Bitmap Image,byte[] Bytes,uint Hash,double RenderMs,double DecodeMs,int? GifFrame,int[] LayerFrames,bool Animated,double PlaybackSeconds) : IDisposable
{
    public void Dispose()=>Image.Dispose();
}
sealed class AnimationComposer : IDisposable
{
    sealed record Request(long Id,string Key,ScreenDesign Design,ScreenReadings Readings);
    readonly CancellationTokenSource stopped=new();
    readonly SemaphoreSlim signal=new(0,1);
    readonly Channel<ComposedFrame> ready;
    readonly Task work;
    Request? request;
    long sequence,replaced;
    public long ReplacedFrames=>Interlocked.Read(ref replaced);
    public void Pause()=>Volatile.Write(ref request,null);
    public AnimationComposer()
    {
        ready=Channel.CreateBounded<ComposedFrame>(new BoundedChannelOptions(1){SingleWriter=true,SingleReader=true,FullMode=BoundedChannelFullMode.DropOldest},frame=>{Interlocked.Increment(ref replaced);frame.Dispose();});
        work=Task.Run(Run);
    }
    public long Submit(string key,ScreenDesign design,ScreenReadings readings)
    {
        long id=Interlocked.Increment(ref sequence);Volatile.Write(ref request,new(id,key,design,readings));if(signal.CurrentCount==0)try{signal.Release();}catch(SemaphoreFullException){}return id;
    }
    public async Task<ComposedFrame> Next(long minimum,CancellationToken cancel)
    {
        while(true){var frame=await ready.Reader.ReadAsync(cancel).ConfigureAwait(false);if(frame.Request>=minimum)return frame;frame.Dispose();}
    }
    async Task Run()
    {
        using var renderer=new DesignRenderer();var clock=Stopwatch.StartNew();double origin=0,next=0;string key="";long published=0;uint lastHash=0;
        try
        {
            while(!stopped.IsCancellationRequested)
            {
                var current=Volatile.Read(ref request);if(current==null){await signal.WaitAsync(stopped.Token);continue;}
                double now=clock.Elapsed.TotalMilliseconds;
                if(key!=current.Key){key=current.Key;origin=now;next=now;published=0;}
                if(now<next){await Task.Delay(Math.Max(1,(int)Math.Ceiling(next-now)),stopped.Token);continue;}
                double period=1000/AnimationStreamMeter.Requested(current.Design.StreamingQuality);
                // Absolute deadlines skip elapsed slots. Work never adds another period.
                next+=Math.Max(1,Math.Floor((now-next)/period)+1)*period;
                renderer.PlaybackSeconds=(now-origin)/1000;
                var started=Stopwatch.GetTimestamp();var image=renderer.Render(current.Design,current.Readings with{Now=DateTime.Now});
                byte[] bytes=DesignRenderer.Rgb565(image);uint hash=CustomScreenSender.Hash(bytes);
                bool animated=DesignRenderer.IsAnimated(current.Design)||renderer.HasPendingMotion||current.Design.AnimationsEnabled&&renderer.HasAnimatedBackground;
                if(hash!=lastHash||published!=current.Id){ready.Writer.TryWrite(new(current.Id,image,bytes,hash,Stopwatch.GetElapsedTime(started).TotalMilliseconds,renderer.DecodeMilliseconds,renderer.CurrentGifFrameIndex,renderer.CurrentMotionFrames,animated,(now-origin)/1000));lastHash=hash;published=current.Id;}else image.Dispose();
                if(!animated)await signal.WaitAsync(stopped.Token);
            }
        }
        catch(OperationCanceledException){}
        catch(Exception ex){ready.Writer.TryComplete(ex);}
        finally{ready.Writer.TryComplete();}
    }
    public void Dispose(){stopped.Cancel();try{work.GetAwaiter().GetResult();}catch(OperationCanceledException){}while(ready.Reader.TryRead(out var frame))frame.Dispose();signal.Dispose();stopped.Dispose();}
}

using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO.Compression;
using System.Text.Json;

sealed record MotionAssetInfo(int Version,int Width,int Height,int[] DelaysMs);
static class MotionAsset
{
    public static bool IsApng(ReadOnlySpan<byte> bytes)
    {
        if(bytes.Length<8||!bytes[..8].SequenceEqual(new byte[]{137,80,78,71,13,10,26,10}))return false;
        int offset=8;while(offset<=bytes.Length-12){uint count=System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(bytes.Slice(offset,4));if(count>bytes.Length-offset-12)return false;var type=bytes.Slice(offset+4,4);if(type.SequenceEqual("acTL"u8))return true;if(type.SequenceEqual("IDAT"u8))return false;offset+=12+(int)count;}return false;
    }
    public static bool IsAsset(byte[] data)=>data.Length>4&&data[0]==80&&data[1]==75&&data[2]==3&&data[3]==4;
    public static MotionAssetInfo Inspect(byte[] bytes)
    {
        if(bytes.Length>24_000_000)throw new InvalidDataException("Animatie groter dan 24 MB.");
        using var stream=new MemoryStream(bytes);using var zip=new ZipArchive(stream,ZipArchiveMode.Read);
        var manifest=zip.GetEntry("animation.json")??throw new InvalidDataException("Animatiemanifest ontbreekt.");if(manifest.Length>16384)throw new InvalidDataException("Manifest te groot.");
        using var reader=new StreamReader(manifest.Open());var info=JsonSerializer.Deserialize<MotionAssetInfo>(reader.ReadToEnd())??throw new InvalidDataException("Leeg manifest.");
        if(info.Version!=1||info.Width is <1 or >412||info.Height is <1 or >412||info.DelaysMs==null||info.DelaysMs.Length is <1 or >300||info.DelaysMs.Any(d=>d is <20 or >10000)||zip.Entries.Count!=info.DelaysMs.Length+1)throw new InvalidDataException("Ongeldige animatiemetadata.");
        long expanded=0;
        Span<byte> header=stackalloc byte[24];
        for(int i=0;i<info.DelaysMs.Length;i++)
        {
            var frame=zip.GetEntry($"{i:D4}.png")??throw new InvalidDataException("Animatiebeeld ontbreekt.");expanded+=frame.Length;
            if(frame.Length>1_000_000||expanded>96_000_000)throw new InvalidDataException("Uitgepakte animatie te groot.");
            using var source=frame.Open();source.ReadExactly(header);
            if(!header[..8].SequenceEqual(new byte[]{137,80,78,71,13,10,26,10})||System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(header[16..20])!=info.Width||System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(header[20..24])!=info.Height)throw new InvalidDataException("Ongeldig PNG-frame of afwijkende afmetingen.");
        }
        return info;
    }
    public static byte[] Pack(IReadOnlyList<string> frames,int delay)
    {
        if(frames.Count is <1 or >300||delay is <20 or >10000)throw new InvalidDataException("1–300 beelden; 20–10000 ms per beeld.");
        using var first=Image.FromFile(frames[0]);int sourceWidth=first.Width,sourceHeight=first.Height;
        if(sourceWidth>2048||sourceHeight>2048)throw new InvalidDataException("Bronbeelden maximaal 2048 × 2048.");
        float scale=Math.Min(1,412f/Math.Max(sourceWidth,sourceHeight));int width=Math.Max(1,(int)(sourceWidth*scale)),height=Math.Max(1,(int)(sourceHeight*scale));
        using var memory=new MemoryStream();
        using(var zip=new ZipArchive(memory,ZipArchiveMode.Create,true))
        {
            var manifest=zip.CreateEntry("animation.json");using(var writer=new StreamWriter(manifest.Open()))writer.Write(JsonSerializer.Serialize(new MotionAssetInfo(1,width,height,Enumerable.Repeat(delay,frames.Count).ToArray())));
            for(int i=0;i<frames.Count;i++){if(new FileInfo(frames[i]).Length>8_000_000)throw new InvalidDataException("PNG groter dan 8 MB.");using var image=Image.FromFile(frames[i]);if(image.Width!=sourceWidth||image.Height!=sourceHeight)throw new InvalidDataException("Afmetingen van beelden verschillen.");using var frame=new Bitmap(width,height,PixelFormat.Format32bppArgb);using(var graphics=Graphics.FromImage(frame)){graphics.CompositingMode=System.Drawing.Drawing2D.CompositingMode.SourceCopy;graphics.InterpolationMode=System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;graphics.DrawImage(image,0,0,width,height);}var entry=zip.CreateEntry($"{i:D4}.png",CompressionLevel.NoCompression);using var target=entry.Open();frame.Save(target,ImageFormat.Png);if(memory.Length>24_000_000)throw new InvalidDataException("Animatie groter dan 24 MB.");}
        }
        byte[] bytes=memory.ToArray();Inspect(bytes);return bytes;
    }
    // Windows performs decoding. Imported files never select a process, protocol or command.
    public static async Task<byte[]> ConvertVideo(string file,string converter,int seconds,int fps,CancellationToken cancel)
    {
        var source=new FileInfo(file);if(!source.Exists||source.Length>256_000_000)throw new InvalidDataException("Videobron ontbreekt of is groter dan 256 MB.");
        if(seconds is <1 or >25||fps is <1 or >25||seconds*fps>300)throw new InvalidDataException("Maximaal 300 geconverteerde beelden.");
        if(!File.Exists(converter))throw new InvalidOperationException("Videoconverter ontbreekt in deze Qscreen-build.");
        string temporary=Path.Combine(Path.GetTempPath(),"Qscreen-Motion-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(temporary);
        try
        {
            var start=new ProcessStartInfo(converter){UseShellExecute=false,CreateNoWindow=true,RedirectStandardError=true,RedirectStandardOutput=true};
            foreach(var arg in new[]{"-nostdin","-v","error","-max_alloc","134217728","-threads","2","-protocol_whitelist","file,pipe","-format_whitelist","mov,matroska,webm,gif,apng,png_pipe,webp_pipe,webp_anim,image2","-i",source.FullName,"-t",seconds.ToString(System.Globalization.CultureInfo.InvariantCulture),"-an","-sn","-dn","-vf",$"fps={fps},scale=412:412:force_original_aspect_ratio=decrease,pad=412:412:(ow-iw)/2:(oh-ih)/2:color=black@0,format=rgba","-frames:v","300","-threads","2",Path.Combine(temporary,"%04d.png")})start.ArgumentList.Add(arg);
            using var process=Process.Start(start)??throw new IOException("Videoconverter kon niet starten.");
            var error=ReadError(process.StandardError);var stdout=process.StandardOutput.ReadToEndAsync(cancel);
            using var timeout=CancellationTokenSource.CreateLinkedTokenSource(cancel);timeout.CancelAfter(TimeSpan.FromMinutes(2));
            try{await process.WaitForExitAsync(timeout.Token);}catch(OperationCanceledException){try{process.Kill(true);await process.WaitForExitAsync();}catch(InvalidOperationException){}throw;}
            await stdout;string message=await error;if(process.ExitCode!=0)throw new InvalidDataException("Videoconversie mislukt: "+message);
            var frames=Directory.GetFiles(temporary,"*.png").Order(StringComparer.Ordinal).ToArray();return Pack(frames,(int)Math.Round(1000d/fps));
        }
        finally{Directory.Delete(temporary,true);}
    }
    static async Task<string> ReadError(StreamReader reader){char[] chunk=new char[1024];var text=new System.Text.StringBuilder();int count;while((count=await reader.ReadAsync(chunk))>0)if(text.Length<4096)text.Append(chunk,0,Math.Min(count,4096-text.Length));return text.ToString();}
}

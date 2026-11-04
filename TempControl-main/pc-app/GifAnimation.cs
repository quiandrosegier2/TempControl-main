using System.Security.Cryptography;

sealed record GifAnimationInfo(int Width,int Height,int[] DelaysMs,bool Transparent,int LoopCount,long Bytes)
{
    public int FrameCount=>DelaysMs.Length;
    public int DurationMs=>DelaysMs.Sum();
    public string Description=>$"GIF · {Width} × {Height} · {FrameCount} frames · {DurationMs/1000d:0.##} s · {Bytes/1024d:0.#} KB";
}
static class GifAnimation
{
    public const int MaxBytes=8_000_000,MaxFrames=60;
    public static bool IsGif(ReadOnlySpan<byte> data)=>data.Length>=6&&(data[..6].SequenceEqual("GIF89a"u8)||data[..6].SequenceEqual("GIF87a"u8));
    // Parse bounded metadata before handing any compressed pixels to the Windows decoder.
    public static GifAnimationInfo Inspect(byte[] bytes)
    {
        if(bytes.Length>MaxBytes||bytes.Length<14||!IsGif(bytes))throw new InvalidDataException("Ongeldige GIF of groter dan 8 MB.");
        int offset=6;
        int Byte(){if(offset>=bytes.Length)throw new InvalidDataException("Afgebroken GIF.");return bytes[offset++];}
        int Word()=>Byte()|Byte()<<8;
        void Skip(int count){if(count<0||offset>bytes.Length-count)throw new InvalidDataException("Afgebroken GIF-blok.");offset+=count;}
        byte[] Blocks(int capture=0){var result=new List<byte>();int n;while((n=Byte())!=0){if(result.Count+n<=capture)result.AddRange(bytes.AsSpan(offset,Math.Min(n,bytes.Length-offset)).ToArray());Skip(n);}return result.ToArray();}
        int width=Word(),height=Word(),flags=Byte();Byte();Byte();if(width<1||height<1||width>2048||height>2048)throw new InvalidDataException("GIF-resolutie: maximaal 2048 × 2048.");
        if((flags&128)!=0)Skip(3*(1<<((flags&7)+1)));
        var delays=new List<int>();int delay=100,loops=0;bool transparent=false,trailer=false;
        while(offset<bytes.Length)
        {
            int kind=Byte();if(kind==0x3b){trailer=true;break;}
            if(kind==0x21)
            {
                int extension=Byte();
                if(extension==0xf9){if(Byte()!=4)throw new InvalidDataException("Ongeldig GIF-framebeheer.");int control=Byte();int cs=Word();Byte();if(Byte()!=0)throw new InvalidDataException("Ongeldig GIF-framebeheer.");delay=cs==0?100:Math.Clamp(cs*10,20,10000);transparent|=(control&1)!=0;}
                else if(extension==0xff){int n=Byte();if(offset>bytes.Length-n)throw new InvalidDataException("Afgebroken GIF-extensie.");string application=System.Text.Encoding.ASCII.GetString(bytes,offset,n);Skip(n);var value=Blocks(32);if(application is "NETSCAPE2.0" or "ANIMEXTS1.0"&&value.Length>=3&&value[0]==1)loops=value[1]|value[2]<<8;}
                else Blocks();
            }
            else if(kind==0x2c)
            {
                int x=Word(),y=Word(),w=Word(),h=Word(),local=Byte();if(w<1||h<1||x+w>width||y+h>height)throw new InvalidDataException("GIF-frame buiten het canvas.");
                if((local&128)!=0)Skip(3*(1<<((local&7)+1)));
                int codeSize=Byte();if(codeSize is <2 or >8)throw new InvalidDataException("Ongeldige GIF-compressie.");Blocks();delays.Add(delay);delay=100;
                if(delays.Count>MaxFrames||(long)width*height*delays.Count>16_000_000)throw new InvalidDataException("GIF-budget overschreden: maximaal 60 frames en 16 miljoen bronpixels. Lange video's komen in de volgende conversiefase.");
            }
            else throw new InvalidDataException("Onbekend GIF-blok.");
        }
        if(!trailer||delays.Count==0)throw new InvalidDataException("GIF mist frames of afsluiting.");
        return new(width,height,delays.ToArray(),transparent,loops,bytes.Length);
    }
    public static (string Data,GifAnimationInfo Info) Import(string path)
    {if(new FileInfo(path).Length>MaxBytes)throw new InvalidDataException("GIF groter dan 8 MB.");var bytes=File.ReadAllBytes(path);var info=Inspect(bytes);return(Convert.ToBase64String(bytes),info);}
    public static string CachePersonal(byte[] data,string? root=null)
    {
        Inspect(data);root??=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"QPRINTS","Qscreen","Animations");Directory.CreateDirectory(root);
        string path=Path.Combine(root,Convert.ToHexString(SHA256.HashData(data)).ToLowerInvariant()+".gif");
        if(!File.Exists(path)){string temp=path+"."+Guid.NewGuid().ToString("N")+".tmp";try{File.WriteAllBytes(temp,data);File.Move(temp,path,true);}finally{if(File.Exists(temp))File.Delete(temp);}}
        return path;
    }
}

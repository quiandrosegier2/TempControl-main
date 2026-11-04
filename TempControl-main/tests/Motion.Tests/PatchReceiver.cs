using System.Buffers.Binary;

sealed class PatchReceiver : IFrameConnection
{
    readonly Queue<string> replies=new();
    readonly MemoryStream incoming=new();
    int length;uint hash,frameHash;bool patch,compressed;
    public bool RejectNext;
    public int LargestChunk;
    public byte[] Pixels=new byte[CustomScreenSender.FrameBytes];
    public readonly List<string> Commands=[];
    public void WriteLine(string line)
    {
        Commands.Add(line);if(line=="CAPS"){replies.Enqueue("QSCREEN_FRAME2");return;}
        patch=line.StartsWith("PATCH1=")||line.StartsWith("PATCH2=")||line.StartsWith("RLEP=");compressed=line.StartsWith("RLE");var fields=line.Split('=')[1].Split(',');length=int.Parse(fields[0]);hash=uint.Parse(fields[1]);frameHash=fields.Length==3?uint.Parse(fields[2]):hash;incoming.SetLength(0);replies.Enqueue("FRAMEREADY");
    }
    public void Write(byte[] bytes,int offset,int count)
    {
        LargestChunk=Math.Max(LargestChunk,count);incoming.Write(bytes,offset,count);if(incoming.Length<length){replies.Enqueue("FRAMECHUNK");return;}
        var data=incoming.ToArray();if(CustomScreenSender.Hash(data)!=hash)throw new Exception("Transport payload checksum mismatch");
        if(compressed)data=DecodeRuns(data);
        if(RejectNext){RejectNext=false;replies.Enqueue("FRAMEERR simulated checksum rejection");return;}
        if(!patch)Pixels=data;
        else
        {
            int position=0;int U16(){int n=BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(position,2));position+=2;return n;}
            int rectangles=U16();for(int i=0;i<rectangles;i++){int x=U16(),y=U16(),w=U16(),h=U16();if(x+w>412||y+h>412)throw new Exception("Out-of-bounds patch");for(int row=0;row<h;row++){Buffer.BlockCopy(data,position,Pixels,((y+row)*412+x)*2,w*2);position+=w*2;}}
            if(position!=data.Length)throw new Exception("Trailing patch bytes");
        }
        if(CustomScreenSender.Hash(Pixels)!=frameHash)throw new Exception("Final frame checksum mismatch");replies.Enqueue("FRAMEOK");
    }
    public string ReadLine()=>replies.Dequeue();
    public static byte[] DecodeRuns(byte[] data)
    {
        using var decoded=new MemoryStream();int cursor=0;
        while(cursor<data.Length){int control=BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(cursor,2));cursor+=2;int count=control&32767;if(count==0||decoded.Length+count*2>CustomScreenSender.FrameBytes+514)throw new InvalidDataException("Invalid decoded length");if((control&32768)!=0){byte a=data[cursor++],b=data[cursor++];for(int i=0;i<count;i++){decoded.WriteByte(a);decoded.WriteByte(b);}}else{decoded.Write(data,cursor,count*2);cursor+=count*2;}}
        return decoded.ToArray();
    }
}

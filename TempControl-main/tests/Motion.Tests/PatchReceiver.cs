using System.Buffers.Binary;

sealed class PatchReceiver : IFrameConnection
{
    readonly Queue<string> replies=new();
    readonly MemoryStream incoming=new();
    int length;uint hash,frameHash;bool patch;
    public bool RejectNext;
    public byte[] Pixels=new byte[CustomScreenSender.FrameBytes];
    public readonly List<string> Commands=[];
    public void WriteLine(string line)
    {
        Commands.Add(line);if(line=="CAPS"){replies.Enqueue("QSCREEN_FRAME2");return;}
        patch=line.StartsWith("PATCH1=");var fields=line.Split('=')[1].Split(',');length=int.Parse(fields[0]);hash=uint.Parse(fields[1]);frameHash=patch?uint.Parse(fields[2]):hash;incoming.SetLength(0);replies.Enqueue("FRAMEREADY");
    }
    public void Write(byte[] bytes,int offset,int count)
    {
        incoming.Write(bytes,offset,count);if(incoming.Length<length){replies.Enqueue("FRAMECHUNK");return;}
        var data=incoming.ToArray();if(CustomScreenSender.Hash(data)!=hash)throw new Exception("Transport payload checksum mismatch");
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
}

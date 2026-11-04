using System.Buffers.Binary;
using System.Drawing;

static class FramePatch
{
    public static byte[]? Create(byte[] before,byte[] after)
    {
        if(before.Length!=CustomScreenSender.FrameBytes||after.Length!=before.Length)throw new ArgumentException("Invalid RGB565 frame.");
        var rectangles=new List<Rectangle>();
        const int size=16,width=412;
        for(int y=0;y<width;y+=size)
        {
            int run=-1;
            for(int x=0;x<=width;x+=size)
            {
                bool dirty=false;
                if(x<width)for(int row=y;row<Math.Min(y+size,width)&&!dirty;row++){int start=(row*width+x)*2,count=Math.Min(size,width-x)*2;dirty=!before.AsSpan(start,count).SequenceEqual(after.AsSpan(start,count));}
                if(dirty&&run<0)run=x;
                if(!dirty&&run>=0){Add(new(run,y,Math.Min(x,width)-run,Math.Min(size,width-y)));run=-1;}
            }
            if(run>=0)Add(new(run,y,width-run,Math.Min(size,width-y)));
        }
        void Add(Rectangle r){int previous=rectangles.FindLastIndex(a=>a.X==r.X&&a.Width==r.Width&&a.Bottom==r.Y);if(previous>=0){var old=rectangles[previous];rectangles[previous]=new(old.X,old.Y,old.Width,old.Height+r.Height);}else rectangles.Add(r);}
        int bytes=rectangles.Sum(r=>8+r.Width*r.Height*2)+2;
        if(rectangles.Count==0||rectangles.Count>64||bytes>after.Length*3/4)return null;
        byte[] payload=new byte[bytes];int offset=0;
        void U16(int value){BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(offset,2),(ushort)value);offset+=2;}
        U16(rectangles.Count);
        foreach(var r in rectangles){U16(r.X);U16(r.Y);U16(r.Width);U16(r.Height);for(int y=r.Y;y<r.Bottom;y++){after.AsSpan((y*width+r.X)*2,r.Width*2).CopyTo(payload.AsSpan(offset));offset+=r.Width*2;}}
        return payload;
    }
}

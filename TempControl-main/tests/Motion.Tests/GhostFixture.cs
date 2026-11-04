// Original code-drawn test character. No external illustration or executable asset content.
static class GhostFixture
{
    public static byte[] Create()
    {
        using var stream=new MemoryStream();using var writer=new BinaryWriter(stream);
        writer.Write("GIF89a"u8.ToArray());writer.Write((ushort)64);writer.Write((ushort)64);writer.Write((byte)0xf2);writer.Write((byte)0);writer.Write((byte)0);
        writer.Write(new byte[]{0,0,0,245,249,255,10,15,30,255,95,170,70,210,255,170,100,255,255,220,100,15,25,50});
        writer.Write(new byte[]{0x21,0xff,11});writer.Write("NETSCAPE2.0"u8.ToArray());writer.Write(new byte[]{3,1,0,0,0});
        for(int frame=0;frame<12;frame++)
        {
            writer.Write(new byte[]{0x21,0xf9,4,9,12,0,0,0,0x2c});writer.Write((ushort)0);writer.Write((ushort)0);writer.Write((ushort)64);writer.Write((ushort)64);writer.Write((byte)0);writer.Write((byte)3);
            var pixels=new byte[4096];double phase=frame*Math.PI/6;int cx=30+(int)Math.Round(Math.Sin(phase)*3),cy=28+(int)Math.Round(Math.Cos(phase)*4);
            for(int y=0;y<64;y++)for(int x=0;x<64;x++)
            {
                double dx=x-cx,dy=y-cy;bool body=dx*dx/169+dy*dy/196<1||(Math.Abs(dx)<13&&y>=cy&&y<cy+20+(int)(Math.Sin(x*.6+phase)*2));
                if(body)pixels[y*64+x]=1;
                bool eye=(Math.Abs(x-(cx-5))<2||Math.Abs(x-(cx+5))<2)&&Math.Abs(y-(cy-2))<(frame==5?1:3);if(eye&&body)pixels[y*64+x]=2;
                if(body&&Math.Abs(x-cx)<3&&Math.Abs(y-(cy+5))<1)pixels[y*64+x]=3;
                int ax=cx+16,ay=cy+3+(int)(Math.Sin(phase)*6);if((x-ax)*(x-ax)+(y-ay)*(y-ay)<12)pixels[y*64+x]=1;
                int hx=15,hy=(55-frame*3+64)%64;if(Math.Abs(x-hx)+Math.Abs(y-hy)<3)pixels[y*64+x]=3;
            }
            // Frequent clear codes keep a fixed 4-bit code width; valid GIF LZW, deliberately simple fixture.
            var data=new List<byte>();int accumulator=0,bits=0;
            void Code(int value){accumulator|=value<<bits;bits+=4;while(bits>=8){data.Add((byte)accumulator);accumulator>>=8;bits-=8;}}
            for(int i=0;i<pixels.Length;i+=2){Code(8);Code(pixels[i]);if(i+1<pixels.Length)Code(pixels[i+1]);}Code(9);if(bits>0)data.Add((byte)accumulator);
            for(int i=0;i<data.Count;i+=255){int count=Math.Min(255,data.Count-i);writer.Write((byte)count);writer.Write(data.GetRange(i,count).ToArray());}writer.Write((byte)0);
        }
        writer.Write((byte)0x3b);return stream.ToArray();
    }
}

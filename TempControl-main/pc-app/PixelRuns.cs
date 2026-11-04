using System.Buffers.Binary;

// Lossless RGB565 PackBits: a 16-bit control followed by a repeated word or
// literal words. The high control bit selects a run; the low 15 bits are count.
static class PixelRuns
{
    public static byte[] Encode(byte[] input)
    {
        if(input.Length%2!=0)throw new ArgumentException("Expected 16-bit words.");
        using var output=new MemoryStream();int words=input.Length/2,index=0;
        ushort At(int i)=>BinaryPrimitives.ReadUInt16LittleEndian(input.AsSpan(i*2,2));
        void Put(ushort value){output.WriteByte((byte)value);output.WriteByte((byte)(value>>8));}
        int Run(int start){int n=1;while(start+n<words&&n<32767&&At(start+n)==At(start))n++;return n;}
        while(index<words)
        {
            int run=Run(index);
            if(run>=3){Put((ushort)(32768|run));Put(At(index));index+=run;}
            else{int begin=index;index+=run;while(index<words&&index-begin<32765){int next=Run(index);if(next>=3)break;index+=next;}Put((ushort)(index-begin));output.Write(input,begin*2,(index-begin)*2);}
        }
        return output.ToArray();
    }
}

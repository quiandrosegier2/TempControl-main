using System.Globalization;
using System.IO.Ports;

interface IFrameConnection
{
    void WriteLine(string line);
    void Write(byte[] bytes, int offset, int count);
    string ReadLine();
}

interface IEventFrameConnection : IFrameConnection { bool HasData { get; } }
sealed class SerialFrameConnection(SerialPort port) : IEventFrameConnection
{
    public bool HasData=>port.IsOpen&&port.BytesToRead>0;
    public void WriteLine(string line) => port.WriteLine(line);
    public void Write(byte[] bytes,int offset,int count) => port.Write(bytes,offset,count);
    public string ReadLine() => port.ReadLine().Trim();
}

sealed class CustomScreenSender(IFrameConnection connection,Action<string>? onEvent=null,bool patchSupported=false,bool burstSupported=false,bool compressionSupported=false)
{
    bool? supported;
    byte[]? previousFrame;
    public int LastPayloadBytes {get;private set;}
    public void InvalidateBaseline()=>previousFrame=null;
    public const int FrameBytes = 412 * 412 * 2;
    public static uint Hash(byte[] bytes)
    {
        uint hash=2166136261;
        foreach(byte b in bytes)hash=unchecked((hash^b)*16777619);
        return hash;
    }
    void Expect(string expected)
    {
        // Ignore startup messages and old PING replies, but never an explicit rejection.
        for(int i=0;i<128;i++)
        {
            string line=connection.ReadLine().Trim();
            if(line.StartsWith("EVENT;",StringComparison.Ordinal)){onEvent?.Invoke(line);continue;}
            if(line==expected)return;
            if(expected=="QSCREEN_FRAME2" && line.StartsWith("QSCREEN_FRAME",StringComparison.Ordinal))throw new NotSupportedException("Oud beeldprotocol.");
            if(line.StartsWith("FRAMEERR",StringComparison.Ordinal))throw new IOException("Qscreen heeft het beeld geweigerd: "+line);
        }
        throw new IOException("Onverwacht antwoord van Qscreen.");
    }
    public void Control(string command,string expected){connection.WriteLine(command);Expect(expected);}
    public string ReadStatistics()
    {
        connection.WriteLine("STATS");
        for(int i=0;i<128;i++){string line=connection.ReadLine().Trim();if(line.StartsWith("QSTATS;",StringComparison.Ordinal))return line;if(line.StartsWith("EVENT;",StringComparison.Ordinal))onEvent?.Invoke(line);}
        throw new IOException("Schermstatistieken ontbreken.");
    }
    public void PollEvents()
    {
        if(connection is not IEventFrameConnection events)return;
        for(int i=0;i<16&&events.HasData;i++){string line=events.ReadLine().Trim();if(line.StartsWith("EVENT;",StringComparison.Ordinal))onEvent?.Invoke(line);}
    }
    public bool Send(byte[] bytes,Action<int>? progress=null)
    {
        if(bytes.Length!=FrameBytes)throw new ArgumentException("Invalid frame length.");
        if(supported==null)
        {
            connection.WriteLine("CAPS");
            try { Expect("QSCREEN_FRAME2"); supported=true; }
            catch(Exception ex) when(ex is TimeoutException or NotSupportedException) { supported=false; }
        }
        if(supported!=true)return false;
        byte[] payload=patchSupported&&previousFrame!=null?FramePatch.Create(previousFrame,bytes)??bytes:bytes;
        bool patch=!ReferenceEquals(payload,bytes),compressed=false;
        if(compressionSupported&&burstSupported){var packed=PixelRuns.Encode(payload);if(packed.Length<payload.Length*.9){payload=packed;compressed=true;}}
        LastPayloadBytes=payload.Length;
        // A failed transfer invalidates the delta baseline. Next attempt is a complete frame.
        previousFrame=null;
        connection.WriteLine(compressed?(patch?"RLEP=":"RLEF=")+payload.Length.ToString(CultureInfo.InvariantCulture)+","+Hash(payload).ToString(CultureInfo.InvariantCulture)+","+Hash(bytes).ToString(CultureInfo.InvariantCulture):patch?(burstSupported?"PATCH2=":"PATCH1=")+payload.Length.ToString(CultureInfo.InvariantCulture)+","+Hash(payload).ToString(CultureInfo.InvariantCulture)+","+Hash(bytes).ToString(CultureInfo.InvariantCulture):(burstSupported?"FRAME3=":"FRAME2=")+FrameBytes.ToString(CultureInfo.InvariantCulture)+","+Hash(bytes).ToString(CultureInfo.InvariantCulture));
        Expect("FRAMEREADY");
        progress?.Invoke(0);
        // Larger windows are used only when the receiver explicitly advertises BURST8K.
        int chunkBytes=burstSupported?8192:1024;
        for(int offset=0;offset<payload.Length;offset+=chunkBytes)
        {
            int count=Math.Min(chunkBytes,payload.Length-offset);
            connection.Write(payload,offset,count);
            if(offset+count<payload.Length)Expect("FRAMECHUNK");
            if(offset+count<payload.Length)progress?.Invoke((offset+count)*100/payload.Length);
        }
        Expect("FRAMEOK");
        progress?.Invoke(100);
        if(patchSupported)previousFrame=(byte[])bytes.Clone();
        return true;
    }
}

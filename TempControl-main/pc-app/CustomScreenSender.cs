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

sealed class CustomScreenSender(IFrameConnection connection,Action<string>? onEvent=null)
{
    bool? supported;
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
    public void PollEvents()
    {
        if(connection is not IEventFrameConnection events)return;
        for(int i=0;i<16&&events.HasData;i++){string line=events.ReadLine().Trim();if(line.StartsWith("EVENT;",StringComparison.Ordinal))onEvent?.Invoke(line);}
    }
    public bool Send(byte[] bytes)
    {
        if(bytes.Length!=FrameBytes)throw new ArgumentException("Invalid frame length.");
        if(supported==null)
        {
            connection.WriteLine("CAPS");
            try { Expect("QSCREEN_FRAME2"); supported=true; }
            catch(Exception ex) when(ex is TimeoutException or NotSupportedException) { supported=false; }
        }
        if(supported!=true)return false;
        connection.WriteLine("FRAME2="+FrameBytes.ToString(CultureInfo.InvariantCulture)+","+Hash(bytes).ToString(CultureInfo.InvariantCulture));
        Expect("FRAMEREADY");
        // Never outrun the ESP32 RX queue: wait after each complete 1024-byte block.
        for(int offset=0;offset<bytes.Length;offset+=1024)
        {
            int count=Math.Min(1024,bytes.Length-offset);
            connection.Write(bytes,offset,count);
            if(offset+count<bytes.Length)Expect("FRAMECHUNK");
        }
        Expect("FRAMEOK");
        return true;
    }
}

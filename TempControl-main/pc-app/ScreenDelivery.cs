using System.Security.Cryptography;
using System.Text.Json;

sealed record ScreenDelivery(DateTime UpdatedUtc, string Revision, string State, string Message)
{
    public static string RevisionOf(string directory) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(directory,"custom-design.json"))));
    public static void Write(string directory,string revision,string state,string message)
    {
        string path=Path.Combine(directory,"screen-delivery.json");
        string temporary=path+"."+Guid.NewGuid().ToString("N")+".tmp";
        try
        {
            File.WriteAllText(temporary,JsonSerializer.Serialize(new ScreenDelivery(DateTime.UtcNow,revision,state,message)));
            File.Move(temporary,path,true);
        }
        catch(Exception ex) when(ex is IOException or UnauthorizedAccessException) { }
        finally { try { if(File.Exists(temporary))File.Delete(temporary); } catch(IOException) { } }
    }
    public static ScreenDelivery? Read(string directory)
    {
        try { return JsonSerializer.Deserialize<ScreenDelivery>(File.ReadAllText(Path.Combine(directory,"screen-delivery.json"))); }
        catch(Exception ex) when(ex is IOException or UnauthorizedAccessException or JsonException) { return null; }
    }
    public static string Describe(ScreenDelivery? delivery,string revision,DateTime requestedUtc,DateTime now)
    {
        if(delivery!=null && delivery.UpdatedUtc>=requestedUtc && now-delivery.UpdatedUtc<TimeSpan.FromSeconds(30))
        {
            if(delivery.State=="disconnected")return "Geen schermverbinding. Sluit het scherm aan met een USB-datakabel. "+delivery.Message;
            if(delivery.State=="firmware-required")return "Het scherm ondersteunt eigen ontwerpen nog niet. Klik op Firmware bijwerken en installeer de meegeleverde firmware.";
            if(delivery.Revision==revision)
            {
                if(delivery.State=="received")return "Ontwerp ontvangen en weergegeven op het scherm.";
                if(delivery.State=="invalid-design")return "Ontwerp kon niet worden verstuurd: "+delivery.Message;
                if(delivery.State=="sending")return "Verbonden. Ontwerp wordt naar het scherm verstuurd; wachten op ontvangstbevestiging…";
            }
        }
        return now-requestedUtc>TimeSpan.FromSeconds(30)
            ? "Nog geen bevestiging van het scherm. Controleer de achtergrondmeting en werk de firmware bij. Je ontwerp is wel opgeslagen."
            : "Ontwerp opgeslagen. Achtergrondmeting wordt gestart; wachten op bevestiging van het scherm…";
    }
}

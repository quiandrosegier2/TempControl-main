using System.Globalization;
using System.Net.Http;
using System.Text.Json;

sealed class WeatherSettings
{
    public bool Enabled { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public void Validate(){if(!double.IsFinite(Latitude)||!double.IsFinite(Longitude)||Latitude is < -90 or >90||Longitude is < -180 or >180)throw new InvalidDataException("Ongeldige weercoördinaten.");}
}
sealed record WeatherReading(float Temperature,int Code,bool Day,DateTime UpdatedUtc);
sealed class WeatherService : IDisposable
{
    readonly string directory;readonly HttpClient http=new(){Timeout=TimeSpan.FromSeconds(8)};
    Task<WeatherReading?>? pending;WeatherReading? latest;DateTime next,lastConfig;string key="",pendingKey="";
    public WeatherService(string directory){this.directory=directory;}
    public WeatherReading? Poll(DateTime now)
    {
        try
        {

            string path=Path.Combine(directory,"weather-settings.json");DateTime modified=File.GetLastWriteTimeUtc(path);
            if(modified!=lastConfig){lastConfig=modified;latest=null;next=default;key="";}
            if(pending?.IsCompleted==true){var result=pending.GetAwaiter().GetResult();if(key==pendingKey)latest=result;pending=null;}
            if(now>=next&&pending==null)
            {
                next=now.AddMinutes(10);if(!File.Exists(path))return null;
                if(new FileInfo(path).Length>16384)throw new InvalidDataException("Weerinstellingen te groot.");
                var settings=JsonSerializer.Deserialize<WeatherSettings>(File.ReadAllText(path),DesignFiles.Json)??new();settings.Validate();
                if(!settings.Enabled){latest=null;return null;}
                string current=settings.Latitude.ToString(CultureInfo.InvariantCulture)+","+settings.Longitude.ToString(CultureInfo.InvariantCulture);key=current;
                pendingKey=current;pending=Fetch(settings,current,now);
            }
            return latest!=null&&now-latest.UpdatedUtc<TimeSpan.FromMinutes(20)?latest:null;
        }
        catch(Exception ex)when(ex is IOException or JsonException or InvalidDataException or UnauthorizedAccessException){latest=null;return null;}
    }
    async Task<WeatherReading?> Fetch(WeatherSettings settings,string requestKey,DateTime now)
    {
        try
        {
            using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(8));
            string url=FormattableString.Invariant($"https://api.open-meteo.com/v1/forecast?latitude={settings.Latitude}&longitude={settings.Longitude}&current=temperature_2m,weather_code,is_day&forecast_days=1");
            using var response=await http.GetAsync(url,HttpCompletionOption.ResponseHeadersRead,timeout.Token);response.EnsureSuccessStatusCode();
            if(response.Content.Headers.ContentLength>65536)return null;
            using var stream=await response.Content.ReadAsStreamAsync();using var bounded=new MemoryStream();var buffer=new byte[4096];int count;
            while((count=await stream.ReadAsync(buffer,timeout.Token))>0){if(bounded.Length+count>65536)return null;bounded.Write(buffer,0,count);}
            var result=Parse(bounded.ToArray(),now);return key==requestKey?result:null;
        }
        catch(Exception ex)when(ex is HttpRequestException or OperationCanceledException or JsonException or IOException or InvalidOperationException or KeyNotFoundException or FormatException or ArgumentException){return null;}
    }
    internal static WeatherReading Parse(byte[] json,DateTime now)
    {using var doc=JsonDocument.Parse(json);var c=doc.RootElement.GetProperty("current");float temperature=c.GetProperty("temperature_2m").GetSingle();if(!float.IsFinite(temperature)||temperature is < -100 or >70)throw new InvalidDataException("Ongeldige weertemperatuur.");DateTime observed=DateTime.Parse(c.GetProperty("time").GetString()!,CultureInfo.InvariantCulture,DateTimeStyles.AssumeUniversal|DateTimeStyles.AdjustToUniversal);if(Math.Abs((now-observed).TotalMinutes)>30)throw new InvalidDataException("Weergegevens zijn niet actueel.");return new(temperature,c.GetProperty("weather_code").GetInt32(),c.GetProperty("is_day").GetInt32()==1,observed);}
    public void Dispose()=>http.Dispose();
}

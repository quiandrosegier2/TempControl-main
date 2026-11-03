using System.ComponentModel;
using System.Runtime.InteropServices;

sealed class SensorSampler(Func<ScreenReadings> read,Action<string> log)
{
    bool failed;
    public ScreenReadings Read(DateTime now)
    {
        try
        {
            var readings=read();
            if(failed){failed=false;log("Sensormeting hersteld.");}
            return readings;
        }
        catch(Exception ex)when(ex is IOException or InvalidOperationException or Win32Exception or ExternalException)
        {
            if(!failed)log("Sensormeting tijdelijk niet beschikbaar: "+ex.Message);
            failed=true;
            return new(null,null,null,null,null,null,now);
        }
    }
}

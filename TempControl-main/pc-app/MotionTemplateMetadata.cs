static class MotionTemplateMetadata
{
    public static string Describe(ScreenDesign design)
    {
        var pages=new[]{DisplayPage.From(design)}.Concat(design.Pages).ToArray();
        var sources=pages.Select(p=>p.BackgroundPng).Concat(pages.SelectMany(p=>p.MotionLayers).Select(l=>l.ImageData)).Where(s=>s!=null).Distinct(StringComparer.Ordinal).ToArray();
        long bytes=0;bool frameAnimation=false;
        foreach(var source in sources){byte[] data=Convert.FromBase64String(source!);bytes+=data.Length;frameAnimation|=MotionAsset.IsAsset(data)?MotionAsset.Inspect(data).DelaysMs.Length>1:GifAnimation.IsGif(data)&&GifAnimation.Inspect(data).FrameCount>1;}
        string type=frameAnimation?"Frameanimatie":pages.Any(p=>DesignRenderer.IsAnimated(p.Design()))?"Procedurele beweging":"Statisch";
        return $"{type} · bronbeelden {bytes/1024d:0.#} KB op pc\nUSB-streaming · pc blijft verbonden · snelheid via Apparaten gemeten";
    }
}

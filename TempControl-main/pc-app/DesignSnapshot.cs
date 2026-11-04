sealed record DesignSnapshot(string Metadata,string? Background,string?[] PageBackgrounds,string?[] LayerImages,string?[][] PageLayerImages)
{
    public static DesignSnapshot Capture(ScreenDesign design)
    {
        // Background data is immutable. Share its reference instead of serializing megabytes per edit.
        var metadata=new ScreenDesign { Version=design.Version,Name=design.Name,FirstPageName=design.FirstPageName,BackgroundArgb=design.BackgroundArgb,Fit=design.Fit,Darken=design.Darken,MotionLayers=design.MotionLayers.Select(l=>l.MetadataCopy()).ToList(),Widgets=design.Widgets,TouchZones=design.TouchZones,RotatePages=design.RotatePages,RotationSeconds=design.RotationSeconds,Transition=design.Transition,Animation=design.Animation,AnimationSpeed=design.AnimationSpeed,AnimationsEnabled=design.AnimationsEnabled,StreamingQuality=design.StreamingQuality,
            Pages=design.Pages.Select(p=>new DisplayPage{Name=p.Name,BackgroundArgb=p.BackgroundArgb,Fit=p.Fit,Darken=p.Darken,MotionLayers=p.MotionLayers.Select(l=>l.MetadataCopy()).ToList(),Widgets=p.Widgets,TouchZones=p.TouchZones,Animation=p.Animation,AnimationSpeed=p.AnimationSpeed,AnimationsEnabled=p.AnimationsEnabled,StreamingQuality=p.StreamingQuality}).ToList() };
        return new(DesignFiles.Serialize(metadata),design.BackgroundPng,design.Pages.Select(p=>p.BackgroundPng).ToArray(),design.MotionLayers.Select(l=>l.ImageData).ToArray(),design.Pages.Select(p=>p.MotionLayers.Select(l=>l.ImageData).ToArray()).ToArray());
    }
    public bool Equivalent(DesignSnapshot other)=>Metadata==other.Metadata&&Background==other.Background&&PageBackgrounds.SequenceEqual(other.PageBackgrounds)&&LayerImages.SequenceEqual(other.LayerImages)&&PageLayerImages.Length==other.PageLayerImages.Length&&PageLayerImages.Zip(other.PageLayerImages).All(p=>p.First.SequenceEqual(p.Second));
    public ScreenDesign Restore(){var design=DesignFiles.Parse(Metadata);design.BackgroundPng=Background;for(int n=0;n<design.MotionLayers.Count;n++)design.MotionLayers[n].ImageData=LayerImages[n];for(int i=0;i<design.Pages.Count;i++){design.Pages[i].BackgroundPng=PageBackgrounds[i];for(int n=0;n<design.Pages[i].MotionLayers.Count;n++)design.Pages[i].MotionLayers[n].ImageData=PageLayerImages[i][n];}return design;}
}

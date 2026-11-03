sealed record DesignSnapshot(string Metadata,string? Background,string?[] PageBackgrounds)
{
    public static DesignSnapshot Capture(ScreenDesign design)
    {
        // Background data is immutable. Share its reference instead of serializing megabytes per edit.
        var metadata=new ScreenDesign { Version=design.Version,Name=design.Name,FirstPageName=design.FirstPageName,BackgroundArgb=design.BackgroundArgb,Fit=design.Fit,Darken=design.Darken,Widgets=design.Widgets,TouchZones=design.TouchZones,RotatePages=design.RotatePages,RotationSeconds=design.RotationSeconds,Transition=design.Transition,Animation=design.Animation,AnimationSpeed=design.AnimationSpeed,AnimationsEnabled=design.AnimationsEnabled,
            Pages=design.Pages.Select(p=>new DisplayPage{Name=p.Name,BackgroundArgb=p.BackgroundArgb,Fit=p.Fit,Darken=p.Darken,Widgets=p.Widgets,TouchZones=p.TouchZones,Animation=p.Animation,AnimationSpeed=p.AnimationSpeed,AnimationsEnabled=p.AnimationsEnabled}).ToList() };
        return new(DesignFiles.Serialize(metadata),design.BackgroundPng,design.Pages.Select(p=>p.BackgroundPng).ToArray());
    }
    public bool Equivalent(DesignSnapshot other)=>Metadata==other.Metadata&&Background==other.Background&&PageBackgrounds.SequenceEqual(other.PageBackgrounds);
    public ScreenDesign Restore(){var design=DesignFiles.Parse(Metadata);design.BackgroundPng=Background;for(int i=0;i<design.Pages.Count;i++)design.Pages[i].BackgroundPng=PageBackgrounds[i];return design;}
}

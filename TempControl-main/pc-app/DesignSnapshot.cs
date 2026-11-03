sealed record DesignSnapshot(string Metadata,string? Background)
{
    public static DesignSnapshot Capture(ScreenDesign design)
    {
        // Background data is immutable. Share its reference instead of serializing megabytes per edit.
        var metadata=new ScreenDesign { Version=design.Version,Name=design.Name,BackgroundArgb=design.BackgroundArgb,Fit=design.Fit,Darken=design.Darken,Widgets=design.Widgets };
        return new(DesignFiles.Serialize(metadata),design.BackgroundPng);
    }
    public ScreenDesign Restore(){var design=DesignFiles.Parse(Metadata);design.BackgroundPng=Background;return design;}
}

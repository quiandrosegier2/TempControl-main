using System.ComponentModel;

sealed class WidgetInspector(ScreenWidget widget) : CustomTypeDescriptor(TypeDescriptor.GetProvider(widget).GetTypeDescriptor(widget))
{
    public override object GetPropertyOwner(PropertyDescriptor? pd)=>widget;
    public override PropertyDescriptorCollection GetProperties()=>GetProperties(null);
    public override PropertyDescriptorCollection GetProperties(Attribute[]? attributes)
    {
        bool shape=widget.Kind is WidgetKind.Rectangle or WidgetKind.Ellipse or WidgetKind.ProgressBar or WidgetKind.HistoryGraph or WidgetKind.CircularGauge or WidgetKind.AnalogClock or WidgetKind.Panel or WidgetKind.WeatherIcon;
        bool temperature=widget.Kind is WidgetKind.CpuTemperature or WidgetKind.GpuTemperature;
        var visible=TypeDescriptor.GetProperties(widget).Cast<PropertyDescriptor>().Where(p=>p.IsBrowsable)
            .Where(p=>p.Name switch {
                "BarSource"=>widget.Kind is WidgetKind.ProgressBar or WidgetKind.HistoryGraph or WidgetKind.CircularGauge,
                "Fill"=>widget.Kind is WidgetKind.ProgressBar or WidgetKind.HistoryGraph or WidgetKind.CircularGauge or WidgetKind.AnalogClock or WidgetKind.Panel or WidgetKind.WeatherIcon,
                "Maximum"=>widget.Kind is WidgetKind.ProgressBar or WidgetKind.CircularGauge,
                "StartAngle" or "SweepAngle" or "Segments" or "Needle"=>widget.Kind==WidgetKind.CircularGauge,
                "StrokeWidth"=>widget.Kind is WidgetKind.CircularGauge or WidgetKind.Panel,
                "PanelShape"=>widget.Kind==WidgetKind.Panel,
                "WeatherDayNight"=>widget.Kind==WidgetKind.WeatherIcon,
                "HistoryMinutes"=>widget.Kind==WidgetKind.HistoryGraph,
                "TemperatureColors"=>temperature,
                "Format" or "FontFamily" or "FontSize" or "Bold" or "Italic" or "Alignment"=>!shape,
                _=>true
            }).ToArray();
        return new PropertyDescriptorCollection(visible,true);
    }
}

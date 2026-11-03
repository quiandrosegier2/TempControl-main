using System.Drawing;

sealed record AlertNotice(string Id,string Name,WidgetKind Sensor,int ColorArgb,int Priority,bool Pulse,bool FullScreen,bool DesktopNotification);
sealed class AlertEngine
{
    sealed class State{public bool Active;public DateTime ShownUntil,LastNotification=DateTime.MinValue;public (AlertCondition,WidgetKind,float,float)? Policy;}
    readonly Dictionary<string,State> states=new();
    public Action<AlertNotice>? Notify { get; set; }
    public IReadOnlyList<AlertNotice> Evaluate(IEnumerable<AlertRule> rules,ScreenReadings readings,bool connected,DateTime now)
    {
        var result=new List<AlertNotice>();var ids=new HashSet<string>();
        foreach(var rule in rules.Where(r=>r.Enabled).OrderByDescending(r=>r.Priority))
        {
            ids.Add(rule.Id);if(!states.TryGetValue(rule.Id,out var state))states[rule.Id]=state=new();
            var policy=(rule.Condition,rule.Sensor,rule.Threshold,rule.Hysteresis);
            if(state.Policy!=policy){state.Policy=policy;state.Active=false;state.ShownUntil=DateTime.MinValue;state.LastNotification=DateTime.MinValue;}
            float? value=readings.Value(rule.Sensor);
            bool triggered=rule.Condition switch{
                AlertCondition.Above=>value.HasValue&&value.Value>=(state.Active?rule.Threshold-rule.Hysteresis:rule.Threshold),
                AlertCondition.Below=>value.HasValue&&value.Value<(state.Active?rule.Threshold+rule.Hysteresis:rule.Threshold),
                AlertCondition.Unavailable=>!value.HasValue,
                AlertCondition.Disconnected=>!connected,_=>false};
            var notice=new AlertNotice(rule.Id,rule.Name,rule.Sensor,rule.ColorArgb,rule.Priority,rule.Pulse,rule.FullScreen,rule.DesktopNotification);
            if(triggered&&now-state.LastNotification>=TimeSpan.FromSeconds(rule.CooldownSeconds))
            {
                state.LastNotification=now;state.ShownUntil=now.AddSeconds(rule.DurationSeconds);
                if(rule.DesktopNotification)Notify?.Invoke(notice);
            }
            state.Active=triggered;
            // Dynamic color follows the hysteresis state; overlays have a finite lifetime.
            if(triggered&&rule.DynamicColor||now<state.ShownUntil)result.Add(notice with{FullScreen=rule.FullScreen&&now<state.ShownUntil});
        }
        foreach(string id in states.Keys.Where(id=>!ids.Contains(id)).ToArray())states.Remove(id);
        return result.OrderByDescending(n=>n.Priority).ToList();
    }
    public static void Overlay(Graphics g,Rectangle bounds,IReadOnlyList<AlertNotice>? notices,DateTime now)
    {
        var alert=notices?.FirstOrDefault(n=>n.FullScreen);if(alert==null)return;
        Color color=Color.FromArgb(alert.ColorArgb);
        using var fill=new SolidBrush(Color.FromArgb(alert.Pulse&&now.Second%2==0?180:220,color));g.FillEllipse(fill,bounds);
        using var font=new Font("Segoe UI",26,FontStyle.Bold,GraphicsUnit.Pixel);using var note=new Font("Segoe UI",13,FontStyle.Regular,GraphicsUnit.Pixel);
        using var format=new StringFormat{Alignment=StringAlignment.Center,LineAlignment=StringAlignment.Center};
        g.DrawString(alert.Name,font,Brushes.White,new RectangleF(60,130,292,130),format);g.DrawString("Systeemmelding · controleer je hardware",note,Brushes.White,new RectangleF(60,265,292,60),format);
    }
}

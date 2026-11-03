static class OfficialTemplates
{
    static readonly Lazy<IReadOnlyList<DesignTemplate>> premium=new(()=>NeonTemplates.All.Concat(ExpansionTemplates.All).ToArray());
    public static IReadOnlyList<DesignTemplate> Premium=>premium.Value;
    public static bool IsPremium(DesignTemplate item)=>Premium.Any(t=>ReferenceEquals(t,item));
    public static bool IsOriginal(DesignTemplate item)=>PremiumTemplates.All.Any(t=>ReferenceEquals(t,item));
    public static bool Contains(DesignTemplate item)=>IsPremium(item)||IsOriginal(item);
    public static int Number(DesignTemplate item)=>IsPremium(item)&&int.TryParse(item.Id.AsSpan(28),out int n)?n:0;
}

using System.Drawing;
using System.Text.Json.Serialization;

enum TouchAction { NextPage, PreviousPage, GoToPage, ToggleBrightness }
sealed class TouchZone
{
    public string Name { get; set; }="Volgende";
    public int X { get; set; }=280;
    public int Y { get; set; }=160;
    public int Width { get; set; }=100;
    public int Height { get; set; }=90;
    public TouchAction Action { get; set; }=TouchAction.NextPage;
    public int Page { get; set; }
    public bool Visible { get; set; }=true;
}
sealed class DisplayPage
{
    public string Name { get; set; }="Pagina";
    public int BackgroundArgb { get; set; }=Color.Black.ToArgb();
    public string? BackgroundPng { get; set; }
    public BackgroundFit Fit { get; set; }
    public int Darken { get; set; }=25;
    public List<ScreenWidget> Widgets { get; set; }=[];
    public List<TouchZone> TouchZones { get; set; }=[];
    public PageAnimation Animation { get; set; }
    public float AnimationSpeed { get; set; }=1;
    public bool AnimationsEnabled { get; set; }=true;
    public static DisplayPage From(ScreenDesign design)=>new(){Name=string.IsNullOrWhiteSpace(design.FirstPageName)?design.Name:design.FirstPageName,BackgroundArgb=design.BackgroundArgb,BackgroundPng=design.BackgroundPng,Fit=design.Fit,Darken=design.Darken,Widgets=design.Widgets,TouchZones=design.TouchZones,Animation=design.Animation,AnimationSpeed=design.AnimationSpeed,AnimationsEnabled=design.AnimationsEnabled};
    public ScreenDesign Design()=>new(){Name=Name,BackgroundArgb=BackgroundArgb,BackgroundPng=BackgroundPng,Fit=Fit,Darken=Darken,Widgets=Widgets,TouchZones=TouchZones,Animation=Animation,AnimationSpeed=AnimationSpeed,AnimationsEnabled=AnimationsEnabled};
}
static class DisplayPages
{
    public static int Count(ScreenDesign project)=>1+project.Pages.Count;
    public static ScreenDesign At(ScreenDesign project,int index)=>index<=0||project.Pages.Count==0?DisplayPage.From(project).Design():project.Pages[Math.Clamp(index-1,0,project.Pages.Count-1)].Design();
    public static void Replace(ScreenDesign project,int index,ScreenDesign page)
    {
        if(index>0){project.Pages[index-1]=DisplayPage.From(page);return;}
        project.FirstPageName=page.Name;project.BackgroundArgb=page.BackgroundArgb;project.BackgroundPng=page.BackgroundPng;project.Fit=page.Fit;project.Darken=page.Darken;project.Widgets=page.Widgets;project.TouchZones=page.TouchZones;project.Animation=page.Animation;project.AnimationSpeed=page.AnimationSpeed;project.AnimationsEnabled=page.AnimationsEnabled;
    }
    public static void Reorder(ScreenDesign project,int from,int to)
    {
        if(from<0||to<0||from>=Count(project)||to>=Count(project)||from==to)return;
        var pages=new List<DisplayPage>{DisplayPage.From(project)};pages.AddRange(project.Pages);
        var original=pages.ToArray();
        var moved=pages[from];pages.RemoveAt(from);pages.Insert(to,moved);
        foreach(var zone in pages.SelectMany(p=>p.TouchZones).Where(z=>z.Action==TouchAction.GoToPage&&z.Page>=0&&z.Page<original.Length))zone.Page=pages.IndexOf(original[zone.Page]);
        Replace(project,0,pages[0].Design());project.Pages=pages.Skip(1).ToList();
    }
    public static void Remove(ScreenDesign project,int index)
    {
        if(Count(project)<=1)return;
        if(index==0){Replace(project,0,project.Pages[0].Design());project.Pages.RemoveAt(0);}else project.Pages.RemoveAt(index-1);
        foreach(var zone in new[]{DisplayPage.From(project)}.Concat(project.Pages).SelectMany(p=>p.TouchZones).Where(z=>z.Action==TouchAction.GoToPage))zone.Page=Math.Clamp(zone.Page>index?zone.Page-1:zone.Page,0,Count(project)-1);
    }
}
sealed class PageController
{
    public int Index { get; private set; }
    DateTime changed;
    public void Go(int index,int count,DateTime now){Index=((index%Math.Max(1,count))+Math.Max(1,count))%Math.Max(1,count);changed=now;}
    public void Tick(ScreenDesign project,DateTime now)
    {
        if(Index>=DisplayPages.Count(project))Go(0,DisplayPages.Count(project),now);
        if(changed==default)changed=now;
        if(project.RotatePages&&now-changed>=TimeSpan.FromSeconds(project.RotationSeconds))Go(Index+1,DisplayPages.Count(project),now);
    }
}

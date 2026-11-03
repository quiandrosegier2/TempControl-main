using System.Drawing;

// Owns at most one animation cycle. The PictureBox borrows these images.
sealed class PreviewImageCache(int capacity=40) : IDisposable
{
    readonly Dictionary<string,Image> images=new(StringComparer.OrdinalIgnoreCase);
    readonly Queue<string> order=new();
    public int Count=>images.Count;
    public Image Get(string path)
    {
        if(images.TryGetValue(path,out var cached))return cached;
        using var file=Image.FromFile(path);var image=new Bitmap(file);
        while(images.Count>=Math.Max(1,capacity)){string oldest=order.Dequeue();images[oldest].Dispose();images.Remove(oldest);}
        images.Add(path,image);order.Enqueue(path);return image;
    }
    public void Clear(){foreach(var image in images.Values)image.Dispose();images.Clear();order.Clear();}
    public void Dispose()=>Clear();
}

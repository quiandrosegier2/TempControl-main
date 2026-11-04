using System.Diagnostics;
using System.Runtime.InteropServices;

// Meet de FPS van het venster op de voorgrond (je game) met PresentMon (Intel, open source).
// PresentMon schrijft per frame een CSV-regel; wij tellen de regels per seconde.
sealed record GamingReading(float? Fps,float? AverageFps,float? FrameTimeMs,string? Process);
sealed class FpsMonitor : IDisposable
{
    const string SessionName = "TempMonitor";
    const int MaxLoggedErrors = 20;

    readonly Process? _process;
    readonly object _lock = new();
    readonly Dictionary<(int Pid, string SwapChain), int> _frames = new();
    readonly Stopwatch _window = Stopwatch.StartNew();
    int _pidCol = -1, _swapChainCol = -1, _timeCol=-1;
    readonly Dictionary<(int Pid,string SwapChain),(double Sum,int Count)> _frameTimes=new();
    readonly Queue<(DateTime Time,int Frames,double Seconds)> _recent=new();
    int _lastForeground;
    public GamingReading Latest {get;private set;}=new(null,null,null,null);
    int _loggedErrors;

    public FpsMonitor()
    {
        string exe = Path.Combine(AppContext.BaseDirectory, "PresentMon.exe");
        if (!File.Exists(exe))
        {
            Log.Write("PresentMon.exe niet gevonden: FPS wordt niet gemeten.");
            return;
        }

        var psi = new ProcessStartInfo(exe,
            $"--output_stdout --no_console_stats --stop_existing_session --session_name {SessionName} " +
            "--no_track_display --no_track_gpu --no_track_input")
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        try
        {
            _process = Process.Start(psi)!;
            _process.OutputDataReceived += (_, e) => OnCsvLine(e.Data);
            _process.ErrorDataReceived += (_, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data) && Interlocked.Increment(ref _loggedErrors) <= MaxLoggedErrors)
                    Log.Write($"PresentMon: {e.Data}");
            };
            _process.EnableRaisingEvents = true;
            _process.Exited += (_, _) => Log.Write("PresentMon is gestopt: FPS wordt niet meer gemeten.");
            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();
            Log.Write("PresentMon gestart voor FPS-meting.");
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            Log.Write($"PresentMon kon niet starten: {ex.Message}");
            _process = null;
        }
    }

    // FPS van het voorgrondvenster sinds de vorige aanroep, of null als dat venster geen frames tekent.
    public float? TakeFps()
    {
        int foreground = ForegroundProcessId();
        lock (_lock)
        {
            double seconds = _window.Elapsed.TotalSeconds;
            _window.Restart();

            int best = 0;string chain="";
            foreach (var (key, count) in _frames)
                if (key.Pid == foreground && count > best){best=count;chain=key.SwapChain;}
            float? fps=best>0&&seconds>0?(float)(best/seconds):null;
            float? frameTime=_frameTimes.TryGetValue((foreground,chain),out var times)&&times.Count>0?(float)(times.Sum/times.Count):null;
            if(foreground!=_lastForeground){_recent.Clear();_lastForeground=foreground;}
            if(fps.HasValue){_recent.Enqueue((DateTime.UtcNow,best,seconds));while(_recent.Count>0&&DateTime.UtcNow-_recent.Peek().Time>TimeSpan.FromSeconds(60))_recent.Dequeue();}else _recent.Clear();
            string? process=null;if(fps.HasValue)try{using var active=Process.GetProcessById(foreground);process=active.ProcessName;}catch(ArgumentException){}catch(InvalidOperationException){}catch(System.ComponentModel.Win32Exception){}
            Latest=new(fps,_recent.Count>0?(float)(_recent.Sum(v=>(double)v.Frames)/_recent.Sum(v=>v.Seconds)):null,frameTime,process);
            _frames.Clear();_frameTimes.Clear();return fps;
        }
    }

    void OnCsvLine(string? line)
    {
        if (string.IsNullOrEmpty(line)) return;
        var cols = CsvFields(line);

        lock (_lock)
        {
            if (_pidCol < 0)
            {
                _pidCol = Array.IndexOf(cols, "ProcessID");
                _swapChainCol = Array.IndexOf(cols, "SwapChainAddress");
                _timeCol=Array.IndexOf(cols,"MsBetweenPresents");if(_timeCol<0)_timeCol=Array.IndexOf(cols,"CPUFrameTime");if(_timeCol<0)_timeCol=Array.IndexOf(cols,"MsBetweenAppStart");
                return;
            }
            if (cols.Length <= Math.Max(_pidCol, _swapChainCol) || !int.TryParse(cols[_pidCol], out int pid))
                return;

            var key = (pid, _swapChainCol >= 0 ? cols[_swapChainCol] : "");
            _frames[key] = _frames.GetValueOrDefault(key) + 1;
            if(_timeCol>=0&&_timeCol<cols.Length&&double.TryParse(cols[_timeCol],System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out double ms)&&double.IsFinite(ms)&&ms>0){var old=_frameTimes.GetValueOrDefault(key);_frameTimes[key]=(old.Sum+ms,old.Count+1); }
        }
    }

    internal static string[] CsvFields(string line)
    {
        var fields=new List<string>();var value=new System.Text.StringBuilder();bool quoted=false;
        for(int i=0;i<line.Length;i++){char c=line[i];if(c=='"'){if(quoted&&i+1<line.Length&&line[i+1]=='"'){value.Append('"');i++;}else quoted=!quoted;}else if(c==','&&!quoted){fields.Add(value.ToString().Trim().TrimStart('\uFEFF'));value.Clear();}else value.Append(c);}
        fields.Add(value.ToString().Trim());return fields.ToArray();
    }

    static int ForegroundProcessId()
    {
        GetWindowThreadProcessId(GetForegroundWindow(), out uint pid);
        return (int)pid;
    }

    public void Dispose()
    {
        try { _process?.Kill(entireProcessTree: true); } catch (InvalidOperationException) { }
        _process?.Dispose();
    }

    [DllImport("user32.dll")]
    static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);
}

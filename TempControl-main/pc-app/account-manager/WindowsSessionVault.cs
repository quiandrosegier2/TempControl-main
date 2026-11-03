using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace QscreenAccounts;
interface ISessionVault {string? Load();void Save(string value);void Clear();}
sealed class WindowsSessionVault : ISessionVault
{
    readonly string path;
    public WindowsSessionVault(string? path=null){this.path=path??Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"QPRINTS","Qscreen","Accounts","session.dpapi");}
    [StructLayout(LayoutKind.Sequential)]struct Blob{public int Length;public IntPtr Data;}
    [DllImport("crypt32.dll",CharSet=CharSet.Unicode,SetLastError=true)]static extern bool CryptProtectData(ref Blob input,string? description,IntPtr entropy,IntPtr reserved,IntPtr prompt,uint flags,out Blob output);
    [DllImport("crypt32.dll",SetLastError=true)]static extern bool CryptUnprotectData(ref Blob input,IntPtr description,IntPtr entropy,IntPtr reserved,IntPtr prompt,uint flags,out Blob output);
    [DllImport("kernel32.dll")]static extern IntPtr LocalFree(IntPtr value);
    static byte[] Transform(byte[] bytes,bool protect)
    {
        var input=new Blob{Length=bytes.Length,Data=Marshal.AllocHGlobal(bytes.Length)};Blob output=default;
        try{Marshal.Copy(bytes,0,input.Data,bytes.Length);bool ok=protect?CryptProtectData(ref input,"Qscreen account session",IntPtr.Zero,IntPtr.Zero,IntPtr.Zero,1,out output):CryptUnprotectData(ref input,IntPtr.Zero,IntPtr.Zero,IntPtr.Zero,IntPtr.Zero,1,out output);if(!ok)throw new CryptographicException(new Win32Exception(Marshal.GetLastWin32Error()).Message);var result=new byte[output.Length];Marshal.Copy(output.Data,result,0,result.Length);return result;}
        finally{for(int i=0;i<input.Length;i++)Marshal.WriteByte(input.Data,i,0);Marshal.FreeHGlobal(input.Data);if(output.Data!=IntPtr.Zero){for(int i=0;i<output.Length;i++)Marshal.WriteByte(output.Data,i,0);LocalFree(output.Data);}}
    }
    public string? Load(){if(!File.Exists(path))return null;try{if(new FileInfo(path).Length>131072)throw new InvalidDataException();var decoded=Transform(File.ReadAllBytes(path),false);try{return Encoding.UTF8.GetString(decoded);}finally{CryptographicOperations.ZeroMemory(decoded);}}catch(Exception ex)when(ex is IOException or CryptographicException){Clear();return null;}}
    public void Save(string value){Directory.CreateDirectory(Path.GetDirectoryName(path)!);var bytes=Encoding.UTF8.GetBytes(value);string temp=path+"."+Guid.NewGuid().ToString("N")+".tmp";try{File.WriteAllBytes(temp,Transform(bytes,true));File.Move(temp,path,true);}finally{CryptographicOperations.ZeroMemory(bytes);if(File.Exists(temp))File.Delete(temp);}}
    public void Clear(){if(File.Exists(path))File.Delete(path);}
}

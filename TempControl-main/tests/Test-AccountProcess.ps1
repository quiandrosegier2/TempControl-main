$ErrorActionPreference='Stop'
$project=Split-Path $PSScriptRoot
$publish=Join-Path $project 'pc-app\installer\publish'
Add-Type @'
using System;
using System.Runtime.InteropServices;
using System.Text;
public static class QscreenManifestReader {
 [DllImport("kernel32.dll",CharSet=CharSet.Unicode,SetLastError=true)] static extern IntPtr LoadLibraryEx(string path,IntPtr file,uint flags);
 [DllImport("kernel32.dll",SetLastError=true)] static extern IntPtr FindResource(IntPtr module,IntPtr name,IntPtr type);
 [DllImport("kernel32.dll")] static extern IntPtr LoadResource(IntPtr module,IntPtr resource);
 [DllImport("kernel32.dll")] static extern IntPtr LockResource(IntPtr resource);
 [DllImport("kernel32.dll")] static extern uint SizeofResource(IntPtr module,IntPtr resource);
 [DllImport("kernel32.dll")] static extern bool FreeLibrary(IntPtr module);
 public static string Read(string path){var module=LoadLibraryEx(path,IntPtr.Zero,0x22);if(module==IntPtr.Zero)throw new Exception("Cannot inspect binary manifest");try{var resource=FindResource(module,new IntPtr(1),new IntPtr(24));if(resource==IntPtr.Zero)throw new Exception("Manifest missing");var bytes=new byte[SizeofResource(module,resource)];Marshal.Copy(LockResource(LoadResource(module,resource)),bytes,0,bytes.Length);return Encoding.UTF8.GetString(bytes).TrimStart('\uFEFF');}finally{FreeLibrary(module);}}
}
'@
$main=[xml][QscreenManifestReader]::Read((Join-Path $publish 'Qscreen.exe'))
$account=[xml][QscreenManifestReader]::Read((Join-Path $publish 'accounts\Qscreen.Accounts.exe'))
$query="//*[local-name()='requestedExecutionLevel']"
if($main.SelectSingleNode($query).GetAttribute('level') -ne 'requireAdministrator'){throw 'Main monitor privilege manifest changed unexpectedly'}
if($account.SelectSingleNode($query).GetAttribute('level') -ne 'asInvoker'){throw 'Account process must use asInvoker'}
$entry=Get-Content (Join-Path $project 'pc-app\account-manager\Program.cs') -Raw
if($entry -notmatch 'WindowsBuiltInRole.Administrator'){throw 'Missing independent elevated-account-process guard'}
'PASS: compiled monitor requires sensor privileges; compiled account process uses asInvoker and has its own elevated-execution guard.'

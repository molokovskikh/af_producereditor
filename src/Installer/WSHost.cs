using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Installer
{
	public class WSHost
	{
		public static void DeleteScript(string tempFile, string publisherPath)
		{
			File.WriteAllText(tempFile,
			                  String.Format(@"
shell = WScript.CreateObject(""WScript.Shell"");
fileSystem = WScript.CreateObject(""Scripting.FileSystemObject"");
i = 0;
" + 
			                                String.Format(@"dir = ""{0}"";", publisherPath.Replace(Path.DirectorySeparatorChar.ToString(), @"\\")) 
			                                + 
			                                String.Format(@"self = ""{0}"";", tempFile.Replace(Path.DirectorySeparatorChar.ToString(), @"\\"))) 
			                  + @"
fileSystem.DeleteFile(self);
while(i < 10)
{
	WScript.Sleep(100);
	try
	{
		fileSystem.DeleteFolder(dir, true);
	}
	catch(exception)
	{}
	if (!fileSystem.FolderExists(dir))
		break;
	i++;
}",
			                  Encoding.GetEncoding(1251));
			var processInfo = new ProcessStartInfo("cscript.exe")
			                  	{
			                  		Arguments = String.Format("\"{0}\" //B //Nologo //T:100", tempFile),
			                  		CreateNoWindow = true,
			                  		UseShellExecute = false,
			                  	};
			Process.Start(processInfo);
		}
	}
}
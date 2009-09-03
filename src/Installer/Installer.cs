using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Xml;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.Win32;

namespace Installer
{
	public class Installer
	{
		private readonly string _version;
		private readonly string _publisher;
		private readonly string _application;
		private readonly string _updateUri;

		private readonly string _applicationPath;
		private readonly string _applicationFiles;
		private readonly string _mainExecutable;

		private const string _uninstalRegistryKey = @"Software\Microsoft\Windows\CurrentVersion\Uninstall";
		private readonly RegistryKey _registryRoot;

		private readonly EventWaitHandle _done = new EventWaitHandle(false, EventResetMode.ManualReset);
		

		public Installer()
		{
			_registryRoot = Registry.CurrentUser;

			var appSettings = ConfigurationManager.AppSettings;

			if (appSettings["Version"] == null)
			{
				var conf = ConfigurationManager.OpenExeConfiguration(Assembly.GetExecutingAssembly().Location);
				var version = conf.AppSettings.Settings["Version"];
				if (version == null || String.IsNullOrEmpty(version.Value))
					throw new Exception("В конфигурационном файле инсталятора нет настройки версии");
				_version = version.Value;
				_application = conf.AppSettings.Settings["Application"].Value;
				_publisher = conf.AppSettings.Settings["Publisher"].Value;
				_updateUri = conf.AppSettings.Settings["UpdateUri"].Value;
			}
			else
			{
				_version = appSettings["Version"];
				_application = appSettings["Application"];
				_publisher = appSettings["Publisher"];
				_updateUri = appSettings["UpdateUri"];
			}

			if (String.IsNullOrEmpty(_updateUri))
				throw new Exception("Установка обновления не возможна");

			var instaletionRoot = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
			var publisherPath = Path.Combine(instaletionRoot, _publisher);
			_applicationPath = Path.Combine(Path.Combine(publisherPath, _application), "Application");
			_applicationFiles = Path.Combine(_applicationPath, _version);
			_mainExecutable = Path.Combine(_applicationPath, _application + ".exe");
		}

		public void Install()
		{
			if (IsApplicationInstalled())
			{
				Start();
				return;
			}

			CopyFiles();
			MoveMainExecutable();
			CreateRegistory();
			CreateShortcuts();

			Process.Start(_mainExecutable);
		}

		public void Uninstall()
		{
			if (!IsApplicationInstalled())
				return;

			DeleteFiles();
			DeleteShortcuts();
			DeleteRegistry();
			DeleteSelf();
		}

		public void Upgrade(int pid)
		{
			CopyFiles();
			UpdateRegistry();

			Console.Out.WriteLine("Done");

			WaitForExit(pid);
			MoveMainExecutable();
			Start();
			UninstallPrevVersion();
		}

		private void CreateShortcuts()
		{		
			CreateShortcut(Environment.GetFolderPath(Environment.SpecialFolder.Desktop));
			var programs = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), _publisher);
			Directory.CreateDirectory(programs);
			CreateShortcut(programs);
		}

		private void CreateShortcut(string path)
		{
			var shortcutFile = Path.Combine(path, _application + ".lnk");
			using (var shortcut = new ShellLink())
			{
				shortcut.Target = _mainExecutable;
				shortcut.Description = _application;
				shortcut.WorkingDirectory = Path.GetDirectoryName(Path.GetDirectoryName(_mainExecutable));
				shortcut.Save(shortcutFile);
			}
		}

		private void CreateRegistory()
		{
			using (var uninstall = _registryRoot.OpenSubKey(_uninstalRegistryKey, true))
			{
				var uninstallLocal = uninstall;
				if (uninstall == null)
					uninstallLocal = _registryRoot.CreateSubKey(_uninstalRegistryKey, RegistryKeyPermissionCheck.ReadWriteSubTree);
				using (var app = uninstallLocal.CreateSubKey(_application))
				{
					app.SetValue("DisplayIcon", String.Format("{0},0", _mainExecutable));
					app.SetValue("DisplayName", _application);
					app.SetValue("DisplayVersion", _version);
					app.SetValue("InstallDate", DateTime.Now.ToString("yyyyMMdd"));
					app.SetValue("InstallLocation", _applicationPath);
					app.SetValue("NoModify", 1, RegistryValueKind.DWord);
					app.SetValue("NoRepair", 1, RegistryValueKind.DWord);
					app.SetValue("Publisher", _publisher);
					app.SetValue("UninstallString", Path.Combine(_applicationFiles, String.Format(@"{0}.Installer.exe /uninstall", _application)));
					app.SetValue("Version", _version);
				}
			}
		}

		private void CopyFiles()
		{
			Directory.CreateDirectory(_applicationFiles);

			var zip = new FastZip();
			var root = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			zip.ExtractZip(Path.Combine(root, String.Format("{0}.zip", _application)), _applicationFiles, null);
			var installer = _applicationFiles;

			//инсталятор кладем вместе со всеми исполняемыми файлами что бы для начала жить проще
			//Path.Combine(_applicationFiles, "Installer");
			//Directory.CreateDirectory(installer);
			File.Copy(Assembly.GetExecutingAssembly().Location,
			          Path.Combine(installer, Path.GetFileName(Assembly.GetExecutingAssembly().Location)), 
			          true);
			var config = Assembly.GetExecutingAssembly().Location + ".config";
			if (File.Exists(config))
				File.Copy(config, Path.Combine(installer, Path.GetFileName(config)), true);
		}

		private void MoveMainExecutable()
		{
			Move(Path.Combine(_applicationFiles, _application + ".exe"), _mainExecutable);
			Move(Path.Combine(_applicationFiles, _application + ".exe.config"), _mainExecutable + ".config");
		}

		private void Move(string source, string destination)
		{
			if (File.Exists(destination))
				File.Delete(destination);
			File.Move(source, destination);
		}


		private bool IsApplicationInstalled()
		{
			using(var uninstall = _registryRoot.OpenSubKey(_uninstalRegistryKey))
			{
				if (uninstall == null)
					return false;
				using (var application = uninstall.OpenSubKey(_application))
					return application != null;
			}
		}

		private void DeleteSelf()
		{
			var tempFile = Path.GetTempFileName();
			tempFile = Path.ChangeExtension(tempFile, "js");
			var instaletionRoot = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
			var publisherPath = Path.Combine(instaletionRoot, _publisher);
			WSHost.DeleteScript(tempFile, publisherPath);
		}

		private void DeleteFiles()
		{
/*
			var files = Directory.GetFiles(_applicationFiles);
			foreach (var file in files)
				File.Delete(file);
			var dirs = Directory.GetDirectories(_applicationFiles);
			foreach (var dir in dirs)
			{
				if (Path.GetFileName(dir.ToLower()) == "installer")
					continue;
				Directory.Delete(dir, true);
			}
*/
		}

		private void DeleteRegistry()
		{
			using(var uninstall = _registryRoot.OpenSubKey(_uninstalRegistryKey, true))
				uninstall.DeleteSubKey(_application);
		}

		private void DeleteShortcuts()
		{
			var desktopShortcut = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), _application + ".lnk");
			if (File.Exists(desktopShortcut))
				File.Delete(desktopShortcut);
			var programs = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), _publisher);
			DeleteDirectory(programs);
		}

		private void UpdateRegistry()
		{
			using(var uninstall = _registryRoot.OpenSubKey(_uninstalRegistryKey, true))
			using(var app = uninstall.OpenSubKey(_application, true))
			{
				app.SetValue("DisplayVersion", _version);
				app.SetValue("Version", _version);
				app.SetValue("InstallDate", DateTime.Now.ToString("yyyyMMdd"));
				app.SetValue("UninstallString", Path.Combine(_applicationFiles, String.Format(@"{0}.Installer.exe /uninstall", _application)));
			}
		}

		public bool Update()
		{
			var files = new List<string>();
			using (var client = new WebClient())
			{
				var buffer = client.DownloadData(_updateUri);
				using(var stream = new MemoryStream(buffer))
				{
					var doc = new XmlDocument();
					doc.Load(stream);
					var versionNode = doc.SelectSingleNode("update/version");
					if (versionNode.InnerText == _version)
						return false;
					var filesNodes = doc.SelectNodes("update/files/file");
					foreach (XmlNode fileNode in filesNodes)
					{
						var file = fileNode.InnerText;
						files.Add(file);
						client.DownloadFile(file, Path.Combine(Path.GetTempPath(), Path.GetFileName(file)));
					}
				}
			}

			var executable = files.First(f => Path.GetExtension(f).ToLower() == ".exe");
			var startInfo = new ProcessStartInfo(Path.Combine(Path.GetTempPath(), Path.GetFileName(executable)), 
			                                     String.Format("/upgrade {0}", Process.GetCurrentProcess().Id))
			                	{
			                		CreateNoWindow = true,
			                		UseShellExecute = false,
			                		RedirectStandardOutput = true,
			                		RedirectStandardError = true,
			                	};
			var process = Process.Start(startInfo);
			string data = null;
			process.OutputDataReceived += (sender, args) =>
			                              {
			                              	data = args.Data;
			                              	_done.Set();
			                              };
			process.BeginOutputReadLine();
			process.Exited += (sender, args) => _done.Set();
			_done.WaitOne();
			if (!String.IsNullOrEmpty(data) && data.ToLower() == "done")
				return true;

			if (!process.HasExited)
				process.Kill();

			var error = process.StandardError.ReadToEnd();
			if (!String.IsNullOrEmpty(error))
				throw new Exception(error);

			throw new Exception(String.Format(@"Не удалось установить обновление, обратитесь в АК ""Инфорум"""));
		}

		private void UninstallPrevVersion()
		{
			foreach (var dir in Directory.GetDirectories(_applicationPath).Where(d => Path.GetFileName(d) != _version))
				DeleteDirectory(dir);
		}

		private static void DeleteDirectory(string dir)
		{
			foreach(var file in Directory.GetFiles(dir))
				File.Delete(file);

			foreach (var subDir in Directory.GetDirectories(dir))
				DeleteDirectory(subDir);

			Directory.Delete(dir);
		}

		private void WaitForExit(int pid)
		{
			var process = Process.GetProcesses().Where(p => p.Id == pid).SingleOrDefault();
			if (process == null)
				return;
			process.WaitForExit();
		}

		private void Start()
		{
			Process.Start(_mainExecutable);
		}
	}
}
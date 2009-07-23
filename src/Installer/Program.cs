using System;
using log4net;
using log4net.Config;

namespace Installer
{
	class Program
	{
		private static readonly ILog _log = LogManager.GetLogger(typeof (Program));

		static int Main(string[] args)
		{
			XmlConfigurator.Configure();
			try
			{
				var installer = new global::Installer.Installer();
				if (args.Length > 0 && args[0] == "/uninstall")
					installer.Uninstall();
				else if (args.Length > 0 && args[0] == "/upgrade")
					installer.Upgrade(Convert.ToInt32(args[1]));
				else
					installer.Install();
				return 0;
			}
			catch (Exception e)
			{
				Console.Error.WriteLine(e);
				_log.Error("Ошибка установщика", e);
				return 1;
			}
		}
	}
}
using System;
using System.Windows.Forms;
using log4net;
using log4net.Config;
using ProducerEditor.Views;

namespace ProducerEditor
{
	public static class Program
	{
		[STAThread]
		public static void Main()
		{
#if !DEBUG
			try {
#endif
				XmlConfigurator.Configure();
#if !DEBUG
				var installer = new Installer.Installer();
				if (installer.Update())
					return;
#endif

				Application.EnableVisualStyles();
				Application.SetCompatibleTextRenderingDefault(false);
				Initializer.Initialize();
#if !DEBUG
				Application.ThreadException += (sender, e) => HandleException(e.Exception);
#endif

				Application.Run(new Shell());
#if !DEBUG
			}
			catch (Exception e) {
				HandleException(e);
			}
#endif
		}

		private static void HandleException(Exception exception)
		{
			var logger = LogManager.GetLogger(typeof(Program));
			logger.Error("Ошибка в Редакторе производителей", exception);
			MessageBox.Show("В приложении возникла ошибка. Попробуйте перезапустить приложение и повторить операцию.",
				"Ошибка приложения",
				MessageBoxButtons.OK,
				MessageBoxIcon.Error);
		}
	}
}
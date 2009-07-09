using System;
using System.Windows.Forms;
using log4net;
using log4net.Config;

namespace ProducerEditor
{
	public static class Program
	{
		[STAThread]
		public static void Main()
		{

			try
			{
				XmlConfigurator.Configure();
				Application.EnableVisualStyles();
				Application.SetCompatibleTextRenderingDefault(false);
				Initialezer.Initialize();
				Application.Run(new MainForm());
				Application.ThreadException += (sender, e) => HandleException(e.Exception);
			}
			catch (Exception e)
			{
				HandleException(e);
			}
		}

		private static void HandleException(Exception exception)
		{
			var logger = LogManager.GetLogger(typeof(Program));
			logger.Error("Ошибка в Редакторе производителей", exception);
			MessageBox.Show("В приложении возникла ошибка. Попробуйте перезапустить приложение и повторить операцию.",
							"Ошибка приложения",
							MessageBoxButtons.OK,
							MessageBoxIcon.Error);
			Application.Exit();
		}
	}
}
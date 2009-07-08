using System;
using System.Windows.Forms;

namespace ProducerEditor
{
	public static class Program
	{
		[STAThread]
		public static void Main()
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Initialezer.Initialize();
			Application.Run(new MainForm());
		}
	}
}
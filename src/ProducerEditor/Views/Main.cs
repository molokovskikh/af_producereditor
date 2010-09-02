using System;
using System.Drawing;
using System.Linq.Expressions;
using System.Windows.Forms;
using ProducerEditor.Infrastructure;
using ProducerEditor.Models;
using Subway.Helpers;
using View = ProducerEditor.Infrastructure.View;

namespace ProducerEditor.Views
{
	public class Main : View
	{
		public Main()
		{
			Text = "Редактор каталога производителей";
			MinimumSize = new Size(800, 600);
			KeyPreview = true;

			this.InputMap()
				.KeyDown(Keys.F5, () => OpenView(typeof(ShowProducers)))
				.KeyDown(Keys.F6, Controller(s => s.ShowAssortment(Settings.Default.BookmarkAssortimentId)))
				.KeyDown(Keys.F7, Controller(s => s.ShowExcludes(0, false)))
				.KeyDown(Keys.F8, Controller(s => s.ShowSynonymReport(DateTime.Now.AddDays(-1).Date, DateTime.Now.Date)))
				.KeyDown(Keys.F9, Controller(s => s.ShowSuspiciousSynonyms()));

			var navigation = new ToolStrip()
				.Button("Producers", "Производители  (F5)", () => OpenView(typeof(ShowProducers)))
				.Button("Ассортимент (F6)", Controller(s => s.ShowAssortment(Settings.Default.BookmarkAssortimentId)))
				.Button("Исключения (F7)", Controller(s => s.ShowExcludes(0, false)))
				.Button("Отчет о сопоставлениях (F8)", Controller(s => s.ShowSynonymReport(DateTime.Now.AddDays(-1).Date, DateTime.Now.Date)))
				.Button("Подозрительные сопоставления (F9)", Controller(c => c.ShowSuspiciousSynonyms()))
				.ActAsNavigator();

			Controls.Add(navigation);

			((ToolStripButton) navigation.Items["Producers"]).Checked = true;
			OpenView(typeof(ShowProducers));
		}

		protected override Action Controller<T>(Expression<Func<ProducerService, T>> func)
		{
			return () => WithService(s => {
				var viewName = MvcHelper.GetViewName(func);
				var viewType = MvcHelper.GetViewType(viewName);
				var result = func.Compile()(s);
				OpenView(viewType, result);
			});
		}

		private void OpenView(Type viewType, params object[] args)
		{
			var form = (Form) Activator.CreateInstance(viewType, args);

			form.ControlBox = false;
			form.FormBorderStyle = FormBorderStyle.None;
			form.MaximizeBox = false;
			form.MinimizeBox = false;
			form.TopLevel = false;
			form.ShowInTaskbar = false;
			form.Dock = DockStyle.Fill;
			Controls.Add(form);
			form.Show();
			form.BringToFront();
		}
	}
}
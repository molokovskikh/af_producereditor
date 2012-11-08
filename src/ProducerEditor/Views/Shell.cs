using System;
using System.Drawing;
using System.Linq.Expressions;
using System.Windows.Forms;
using ProducerEditor.Contract;
using ProducerEditor.Infrastructure;
using Subway.Helpers;
using View = ProducerEditor.Infrastructure.View;

namespace ProducerEditor.Views
{
	public class Shell : View
	{
		private TabControl tabs;

		public Shell()
		{
			Text = "Редактор каталога производителей";
			MinimumSize = new Size(800, 600);
			KeyPreview = true;

			this.InputMap()
				.KeyDown(Keys.F5, () => OpenView(typeof(ShowProducers)))
				.KeyDown(Keys.F6, Controller(s => s.ShowAssortment(Settings.Default.BookmarkAssortimentId)))
				.KeyDown(Keys.F7, Controller(s => s.ShowExcludes()))
				.KeyDown(Keys.F8, Controller(s => s.ShowSynonymReport(DateTime.Now.AddDays(-1).Date, DateTime.Now.Date)))
				.KeyDown(Keys.F9, Controller(s => s.ShowSuspiciousSynonyms()));

			tabs = new TabControl {
				Dock = DockStyle.Fill
			};

			var navigation = new ToolStrip()
				.Button("Producers", "Производители  (F5)", () => OpenView(typeof(ShowProducers)))
				.Button("Ассортимент (F6)", Controller(s => s.ShowAssortment(Settings.Default.BookmarkAssortimentId)))
				.Button("Исключения (F7)", Controller(s => s.ShowExcludes()))
				.Button("Отчет о сопоставлениях (F8)", Controller(s => s.ShowSynonymReport(DateTime.Now.AddDays(-1).Date, DateTime.Now.Date)))
				.Button("Подозрительные сопоставления (F9)", Controller(c => c.ShowSuspiciousSynonyms()))
				.ActAsNavigator()
				.Separator()
				.Button("Закрыть вкладку", CloseTab);

			Controls.Add(tabs);
			Controls.Add(navigation);

			((ToolStripButton)navigation.Items["Producers"]).Checked = true;
			OpenView(typeof(ShowProducers));
		}

		private void CloseTab()
		{
			if (tabs.SelectedTab != null)
				tabs.TabPages.Remove(tabs.SelectedTab);
		}

		protected override Action Controller<T>(Expression<Func<IProducerService, T>> func)
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
			var form = (Form)Activator.CreateInstance(viewType, args);

			form.ControlBox = false;
			form.FormBorderStyle = FormBorderStyle.None;
			form.MaximizeBox = false;
			form.MinimizeBox = false;
			form.TopLevel = false;
			form.ShowInTaskbar = false;
			form.Dock = DockStyle.Fill;
			var tabPage = new TabPage(form.Text);
			tabPage.Controls.Add(form);
			tabs.TabPages.Add(tabPage);
			tabs.SelectedTab = tabPage;
			form.Show();
			form.BringToFront();
		}
	}
}
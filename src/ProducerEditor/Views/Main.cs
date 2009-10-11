using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Linq.Expressions;
using System.ServiceModel;
using System.Windows.Forms;
using ProducerEditor.Infrastructure;
using ProducerEditor.Models;
using Subway.Dom;
using Subway.Dom.Styles;
using Subway.Helpers;
using Subway.VirtualTable;
using Common.Tools;

namespace ProducerEditor.Views
{
	public class WidthHolder
	{
		public static List<int> ProducerWidths = Enumerable.Repeat(100, 4).ToList();
		public static List<int> OffersWidths = Enumerable.Repeat(100, 4).ToList();
		public static List<int> ReportWidths = Enumerable.Repeat(100, 6).ToList();
		public static List<int> ProductsAndProducersWidths = Enumerable.Repeat(100, 5).ToList();
		public static List<int> OffersBySynonymView = Enumerable.Repeat(100, 2).ToList();
		public static List<int> SyspiciosSynonyms = Enumerable.Repeat(100, 6).ToList();
		public static List<int> AssortimentWidths = Enumerable.Repeat(100, 2).ToList();

		public static void Update(VirtualTable table, Column column, List<int> widths)
		{
			var element = column;
			do
			{
				widths[table.Columns.IndexOf(element)] = element.ReadonlyStyle.Get(StyleElementType.Width);
				var node = table.Columns.Find(element).Next;
				if (node != null)
					element = node.Value;
				else
					element = null;
			}
			while(element != null);
		}
	}

	public class View : Form
	{
		protected virtual Action Controller<T>(Expression<Func<ProducerService, T>> func)
		{
			return () => WithService(s => {
				var viewName = MvcHelper.GetViewName(func);
				var viewType = MvcHelper.GetViewType(viewName);
				var result = func.Compile()(s);
				MvcHelper.ShowDialog(viewType, result);
			});
		}

		protected Action Controller(Action<ProducerService> action)
		{
			return () => WithService(action);
		}

		protected void Action(Action<ProducerService> action)
		{
			WithService(action);
		}

		protected void WithService(Action<ProducerService> action)
		{
			var binding = new BasicHttpBinding
			{
				MaxBufferSize = int.MaxValue,
				MaxReceivedMessageSize = int.MaxValue,
				SendTimeout = TimeSpan.FromMinutes(10),
				ReaderQuotas = {MaxArrayLength = int.MaxValue},
			};
			var endpoint = new EndpointAddress(Settings.Default.EndpointAddress + "ProducerService.svc");
			var factory = new ChannelFactory<ProducerService>(binding, endpoint);
			action(factory.CreateChannel());
		}
	}

	public class Main : View
	{
		public Main()
		{
			Text = "Редактор каталога производителей";
			MinimumSize = new Size(800, 600);
			KeyPreview = true;

			this.InputMap()
				.KeyDown(Keys.F9, Controller(s => s.ShowSynonymReport(DateTime.Now.AddDays(-1).Date, DateTime.Now.Date)))
				.KeyDown(Keys.F10, Controller(s => s.ShowSuspiciousSynonyms()));

			var navigation = new ToolStrip()
				.Button("Producers", "Производители", () => OpenView(typeof(ShowProducers)))
				.Button("Ассортимент", Controller(s => s.ShowAssortment(Settings.Default.BookmarkAssortimentId)))
				.Button("Отчет о сопоставлениях (F9)", Controller(s => s.ShowSynonymReport(DateTime.Now.AddDays(-1).Date, DateTime.Now.Date)))
				.Button("Подозрительные сопоставления (F10)", Controller(c => c.ShowSuspiciousSynonyms()))
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
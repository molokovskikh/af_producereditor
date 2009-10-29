using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ProducerEditor.Models;
using Subway.Dom;
using Subway.Dom.Styles;
using Subway.Helpers;
using Subway.VirtualTable;
using Subway.VirtualTable.Behaviors;
using Subway.VirtualTable.Behaviors.Selection;
using Subway.VirtualTable.Behaviors.Specialized;

namespace ProducerEditor.Views
{
	public class ShowSuspiciousSynonyms : View
	{
		private VirtualTable report;

		public ShowSuspiciousSynonyms(IList<SynonymReportItem> items)
		{
			Text = "Подозрительные сопоставления";
			MinimumSize = new Size(640, 480);
			var widths = WidthHolder.SyspiciosSynonyms;
			var tools = new ToolStrip()
				.Button("Удалить (Delete)", Delete)
				.Button("Не подозрительный (Пробел)", NotSuspicious);

			report = new VirtualTable(new TemplateManager<List<SynonymReportItem>, SynonymReportItem>(
				() => { 
					var row = new Row();

					var header = new Header("Пользователь").Sortable("User");
					header.InlineStyle.Set(StyleElementType.Width, widths[0]);
					row.Append(header);

					header = new Header("Прайс").Sortable("Price");
					header.InlineStyle.Set(StyleElementType.Width, widths[1]);
					row.Append(header);

					header = new Header("Регион").Sortable("Region");
					header.InlineStyle.Set(StyleElementType.Width, widths[2]);
					row.Append(header);

					header = new Header("Синоним").Sortable("Synonym");
					header.InlineStyle.Set(StyleElementType.Width, widths[3]);
					row.Append(header);

					header = new Header("Производитель").Sortable("Producer");
					header.InlineStyle.Set(StyleElementType.Width, widths[4]);
					row.Append(header);

					header = new Header("Продукты").Sortable("Products");
					header.InlineStyle.Set(StyleElementType.Width, widths[4]);
					row.Append(header);

					return row;
				},
				i => Row.Cells(i.User, i.Price, i.Region, i.Synonym, i.Producer, i.Products)
			));
			report.CellSpacing = 1;
			report.RegisterBehavior(new ToolTipBehavior(),
				new ColumnResizeBehavior(),
				new RowSelectionBehavior(),
				new SortInList());
			report.Host
				.InputMap()
				.KeyDown(Keys.Delete, NotSuspicious)
				.KeyDown(Keys.Space, Delete);

			report.TemplateManager.Source = items.ToList();
			report.Behavior<ColumnResizeBehavior>().ColumnResized += column => WidthHolder.Update(report, column, widths);
			report.TemplateManager.ResetColumns();

			Controls.Add(report.Host);
			Controls.Add(tools);
			KeyPreview = true;
			this.InputMap().KeyDown(Keys.Escape, Close);

			Shown += (s, a) => report.Host.Focus();
		}

		private void NotSuspicious()
		{
			Controller(s => {
				var item = CurrentItem();
				if (item == null)
					return;
				s.DeleteProducerSynonym(item.Id);
				((IList<SynonymReportItem>)report.TemplateManager.Source).Remove(item);
				report.RebuildViewPort();
			})();
		}

		private void Delete()
		{
			Controller(s => {
				var item = CurrentItem();
				if (item == null)
					return;
				s.DeleteSuspicious(item.Id);
				((IList<SynonymReportItem>)report.TemplateManager.Source).Remove(item);
				report.RebuildViewPort();
			})();
		}

		private SynonymReportItem CurrentItem()
		{
			return report.Selected<SynonymReportItem>();
		}
	}
}

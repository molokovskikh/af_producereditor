using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ProducerEditor.Infrastructure;
using ProducerEditor.Models;
using Subway.Dom;
using Subway.Dom.Styles;
using Subway.Helpers;
using Subway.VirtualTable;
using Subway.VirtualTable.Behaviors;
using Subway.VirtualTable.Behaviors.Selection;
using Subway.VirtualTable.Behaviors.Specialized;
using View = ProducerEditor.Infrastructure.View;

namespace ProducerEditor.Views
{
	public class ShowSynonymReport : View
	{
		private VirtualTable report;

		public ShowSynonymReport(IList<SynonymReportItem> items)
		{
			Text = "Отчет о сопоставлениях";
			report = new VirtualTable(new TemplateManager<List<SynonymReportItem>, SynonymReportItem>(
				() => { 
					var row = new Row();

					var widths = WidthHolder.ReportWidths;

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
				i => {
					var row = Row.Cells(i.User, i.Price, i.Region, i.Synonym, i.Producer, i.Products);
					if (i.IsSuspicious == 1)
						row.AddClass("Suspicious");
					return row;
				}
			));
			report.CellSpacing = 1;
			report.RegisterBehavior(new ToolTipBehavior(),
				new ColumnResizeBehavior(),
				new SortInList(),
				new RowSelectionBehavior());
			report.TemplateManager.Source = items.ToList();
			report.Behavior<ColumnResizeBehavior>().ColumnResized += column => WidthHolder.Update(report, column, WidthHolder.ReportWidths);
			report.TemplateManager.ResetColumns();
			report.Host
				.InputMap()
				.KeyDown(Keys.Space, Suspicios)
				.KeyDown(Keys.Delete, Delete);

			Controls.Add(report.Host);

			var toolBar = new ToolStrip();
			Controls.Add(toolBar);

			var begin = DateTime.Now.AddDays(-1).Date;
			var end = DateTime.Now.Date;

			var beginPeriodCalendar = new DateTimePicker
			{
				Value = begin,
				Width = 130,
			};

			var endPeriodCalendar = new DateTimePicker
			{
				Value = end,
				Width = 130,
			};

			toolBar
				.Label("C")
				.Host(beginPeriodCalendar)
				.Label("По")
				.Host(endPeriodCalendar)
				.Button("Показать", () => {
					Action(s => {
						report.TemplateManager.Source = s.ShowSynonymReport(beginPeriodCalendar.Value, endPeriodCalendar.Value).ToList();
					});
				})
				.Separator()
				.Button("Suspicious", "Подозрительный (Пробел)", Suspicios)
				.Button("Удалить (Delete)", Delete);

			MinimumSize = new Size(640, 480);
			KeyPreview = true;
			this.InputMap().KeyDown(Keys.Escape, Close);
			report.Behavior<IRowSelectionBehavior>().SelectedRowChanged += (oldIndex, newIndex) => {
				var item = report.Translate<SynonymReportItem>(report.ViewPort.GetRow(newIndex));
				if (item.IsSuspicious == 0)
					toolBar.Items["Suspicious"].Text = "Подозрительный (Пробел)";
				else
					toolBar.Items["Suspicious"].Text = "Не подозрительный (Пробел)";
			};
		}

		private void Delete()
		{
			Action(s => {
				var item = CurrentItem();
				if (item == null)
					return;
				s.DeleteProducerSynonym(item.Id);
				((IList<SynonymReportItem>)report.TemplateManager.Source).Remove(item);
				report.RebuildViewPort();
			});
		}

		private void Suspicios()
		{
			Action(s => {
				var item = CurrentItem();
				if (item == null)
					return;

				if (item.IsSuspicious == 1)
				{
					s.DeleteSuspicious(item.Id);
					item.IsSuspicious = 0;
				}
				else
				{
					s.Suspicious(item.Id);
					item.IsSuspicious = 1;
				}
				report.RebuildViewPort();
			});
		}

		private SynonymReportItem CurrentItem()
		{
			return report.Selected<SynonymReportItem>();
		}
	}
}

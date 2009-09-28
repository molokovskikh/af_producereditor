using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using ProducerEditor.Models;
using Subway.Dom;
using Subway.Dom.Styles;
using Subway.Helpers;
using Subway.VirtualTable;
using Subway.VirtualTable.Behaviors;
using Subway.VirtualTable.Behaviors.Specialized;

namespace ProducerEditor.Views
{
	public class SynonymReport : Form
	{
		public SynonymReport(List<SynonymReportItem> items, DateTime begin, DateTime end)
		{
			Text = "Отчет о сопоставлениях";
			var report = new VirtualTable(new TemplateManager<List<SynonymReportItem>, SynonymReportItem>(
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
				i => Row.Cells(i.User, i.Price, i.Region, i.Synonym, i.Producer, i.Products)
			));
			report.CellSpacing = 1;
			report.RegisterBehavior(new ToolTipBehavior(),
			                        new ColumnResizeBehavior(),
			                        new SortInList());
			report.TemplateManager.Source = items;
			report.Behavior<ColumnResizeBehavior>().ColumnResized += column => WidthHolder.Update(report, column, WidthHolder.ReportWidths);
			report.TemplateManager.ResetColumns();

			Controls.Add(report.Host);

			var toolBar = new ToolStrip();
			Controls.Add(toolBar);

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
				.Button("Обновить", () => report.TemplateManager.Source = SynonymReportItem.Load(beginPeriodCalendar.Value, endPeriodCalendar.Value));

			MinimumSize = new Size(640, 480);
			KeyPreview = true;
			this.InputMap().KeyDown(Keys.Escape, Close);
		}
	}
}

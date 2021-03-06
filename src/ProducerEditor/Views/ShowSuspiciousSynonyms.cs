﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ProducerEditor.Contract;
using ProducerEditor.Infrastructure;
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
				.Button("Не подозрительный (Пробел)", NotSuspicious)
				.Button("Отправить уведомление поставщику", SendNotificationToSupplier)
				.Button("Обновить (F11)", Reload);

			report = new VirtualTable(new TemplateManager<SynonymReportItem>(
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
				i => Row.Cells(i.User, i.Price, i.Region, i.Synonym, i.Producer, i.Products)));
			report.CellSpacing = 1;
			report.RegisterBehavior(new ToolTipBehavior(),
				new ColumnResizeBehavior(),
				new RowSelectionBehavior(),
				new SortInList());
			report.Host
				.InputMap()
				.KeyDown(Keys.Delete, NotSuspicious)
				.KeyDown(Keys.Space, Delete)
				.KeyDown(Keys.F11, Reload);

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
			Action(s => {
				var item = CurrentItem();
				if (item == null)
					return;
				s.DeleteProducerSynonym(item.Id);
				((IList<SynonymReportItem>)report.TemplateManager.Source).Remove(item);
				report.RebuildViewPort();
			});
		}

		private void SendNotificationToSupplier()
		{
			var addressList = String.Empty;
			Action(s => {
				var item = CurrentItem();
				if (item == null)
					return;
				addressList = s.GetSupplierEmails(item.SupplierId);
			});
			if (!String.IsNullOrEmpty(addressList))
				Process.Start(String.Format("mailto:{0}?Subject={1}&Body={2}",
					addressList, "Неверная связка товар/производитель", ""));
		}

		private void Delete()
		{
			Action(s => {
				var item = CurrentItem();
				if (item == null)
					return;
				s.DeleteSuspicious(item.Id);
				((IList<SynonymReportItem>)report.TemplateManager.Source).Remove(item);
				report.RebuildViewPort();
			});
		}

		private SynonymReportItem CurrentItem()
		{
			return report.Selected<SynonymReportItem>();
		}

		private void Reload()
		{
			Action(s => {
				report.TemplateManager.Source = s.ShowSuspiciousSynonyms();
			});
		}
	}
}
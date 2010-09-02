using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ProducerEditor.Infrastructure;
using ProducerEditor.Models;
using ProducerEditor.Presenters;
using Subway.Dom;
using Subway.VirtualTable;
using Subway.VirtualTable.Behaviors;
using Subway.VirtualTable.Behaviors.Selection;
using Subway.VirtualTable.Behaviors.Specialized;
using View = ProducerEditor.Infrastructure.View;

namespace ProducerEditor.Views
{
	public class ShowExcludes : View
	{
		public ShowExcludes(Pager<Exclude> pager)
		{
			Text = "Исключения";
			MinimumSize = new Size(640, 480);
			((ShowExcludesPresenter) Presenter).page = pager;
			Wire();
		}

		protected override void Init()
		{
			var excludes = new VirtualTable(new TemplateManager<List<Exclude>, Exclude>(
				() => {
					var row = Row.Headers(
						new Header("Продукт").Sortable("Catalog"),
						new Header("Оригинальное наименование").Sortable("Catalog"),
						new Header("Синоним").Sortable("ProducerSynonym"),
						new Header("Поставщик").Sortable("Supplier"),
						new Header("Регион").Sortable("Region")
						);
					return row;
				},
				e => Row.Cells(e.Catalog, e.OriginalSynonym, e.ProducerSynonym, e.Supplier, e.Region)
			));

			excludes.Host.Name = "Excludes";
			excludes.CellSpacing = 1;
			excludes.RegisterBehavior(
				new RowSelectionBehavior(),
				new ToolTipBehavior(),
				new SortInList(),
				new ColumnResizeBehavior());
			excludes.Behavior<ColumnResizeBehavior>().ColumnResized += column => WidthHolder.Update(excludes, column, WidthHolder.ExcludeWidths);
			excludes.TemplateManager.ResetColumns();

			Controls.Add(excludes.Host);
			Controls.Add(new ToolStrip()
				.Button("AddToAssortment", "Добавить в ассортимент")
				.Button("DoNotShow", "Больше не показывать")
				.Button("DeleteSynonym", "Ошибочное сопоставление по наименованию"));

			Shown += (s, a) => excludes.Host.Focus();
		}
	}
}

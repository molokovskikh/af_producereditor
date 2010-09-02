using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ProducerEditor.Contract;
using ProducerEditor.Infrastructure;
using ProducerEditor.Models;
using ProducerEditor.Presenters;
using Subway.Dom;
using Subway.Dom.Styles;
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
			excludes.Host.Tag = PaginatorExtention.TableName;
			excludes.CellSpacing = 1;
			excludes.RegisterBehavior(
				new RowSelectionBehavior(),
				new ToolTipBehavior(),
				new SortInList(),
				new ColumnResizeBehavior());
			excludes.Behavior<ColumnResizeBehavior>().ColumnResized += column => WidthHolder.Update(excludes, column, WidthHolder.ExcludeWidths);
			excludes.TemplateManager.ResetColumns();

			var synonymsTable = new VirtualTable(new TemplateManager<List<ProducerSynonymDto>, ProducerSynonymDto>(
				() => {
					var row = Row.Headers();
					var header = new Header("Синоним").Sortable("Name");
					row.Append(header);

					header = new Header("Производитель").Sortable("Producer");
					row.Append(header);

					header = new Header("Поставщик").Sortable("Supplier");
					row.Append(header);

					header = new Header("Регион").Sortable("Region");
					row.Append(header);

					return row;
				},
				synonym => {
					var row = Row.Cells(synonym.Name,
						synonym.Producer,
						synonym.Supplier,
						synonym.Region);
					if (synonym.HaveOffers)
						row.AddClass("WithoutOffers");
					return row;
				}));
			synonymsTable.Host.Name = "Synonyms";
			synonymsTable.CellSpacing = 1;
			synonymsTable.RegisterBehavior(
				new ToolTipBehavior(),
				new SortInList(),
				new ColumnResizeBehavior(),
				new RowSelectionBehavior());
			synonymsTable.TemplateManager.ResetColumns();

			var assortment = new VirtualTable(new TemplateManager<List<ProducerOrEquivalentDto>, ProducerOrEquivalentDto>(
				() => Row.Headers(new Header("Производитель").Sortable("Producer")),
				synonym => Row.Cells(synonym.Name)));
			assortment.Host.Name = "Producers";
			assortment.CellSpacing = 1;
			assortment.RegisterBehavior(
				new ToolTipBehavior(),
				new SortInList(),
				new RowSelectionBehavior());
			assortment.TemplateManager.ResetColumns();

			var split = new SplitContainer {
				Height = 200,
				Orientation = Orientation.Vertical,
				Dock = DockStyle.Bottom
			};
			split.Panel1.Controls.Add(assortment.Host);
			split.Panel2.Controls.Add(synonymsTable.Host);

			Controls.Add(excludes.Host);
			Controls.Add(split);
			Controls.Add(new ToolStrip()
				.Button("AddToAssortment", "Добавить в ассортимент")
				.Button("DoNotShow", "Больше не показывать")
				.Button("DeleteSynonym", "Ошибочное сопоставление по наименованию"));

			Shown += (s, a) => excludes.Host.Focus();
		}
	}
}

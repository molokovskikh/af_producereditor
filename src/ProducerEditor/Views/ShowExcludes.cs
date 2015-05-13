using System;
using System.Drawing;
using System.Windows.Forms;
using ProducerEditor.Contract;
using ProducerEditor.Infrastructure;
using ProducerEditor.Presenters;
using Subway.Dom;
using Subway.Dom.Styles;
using Subway.Helpers;
using Subway.VirtualTable;
using Subway.VirtualTable.Behaviors.Specialized;
using View = ProducerEditor.Infrastructure.View;

namespace ProducerEditor.Views
{
	public class ShowExcludes : View
	{
		public ShowExcludes(Pager<ExcludeDto> pager)
		{
			Text = "Исключения";
			MinimumSize = new Size(640, 480);
			((ShowExcludesPresenter)Presenter).page = pager;
		}

		protected override void Init()
		{
			var excludes = new VirtualTable(new TemplateManager<ExcludeDto>(
				() => {
					var row = Row.Headers(
						new Header("Продукт").Sortable("Catalog"),
						new Header("Оригинальное наименование").Sortable("Catalog"),
						new Header("Синоним").Sortable("ProducerSynonym"),
						new Header("Поставщик").Sortable("Supplier"),
						new Header("Регион").Sortable("Region"),
						new Header("Оператор").Sortable("Operator"));
					return row;
				},
				e => Row.Cells(e.Catalog, e.OriginalSynonym, e.ProducerSynonym, e.Supplier, e.Region, e.Operator)));

			excludes.Host.Name = "Excludes";
			excludes.Host.Tag = PaginatorExtention.TableName;

			var synonymsTable = new VirtualTable(new TemplateManager<ProducerSynonymDto>(
				() => {
					return Row.Headers(
						new Header("Синоним").Sortable("Name"),
						new Header("Производитель").Sortable("Producer"),
						new Header("Поставщик").Sortable("Supplier"),
						new Header("Регион").Sortable("Region"));
				},
				synonym => {
					var row = Row.Cells(synonym.Name,
						synonym.Producer,
						synonym.Supplier,
						synonym.Region);
					if (synonym.HaveOffers)
						row.AddClass("WithoutOffers");
					if (synonym.SameAsCurrent)
						row.AddClass("SameAsCurrent");
					return row;
				}));
			synonymsTable.Host.Name = "ProducerSynonyms";

			var assortment = new VirtualTable(new TemplateManager<ProducerOrEquivalentDto>(
				() => Row.Headers(new Header("Производитель").Sortable("Producer")),
				synonym => Row.Cells(synonym.Name)));
			assortment.Host.Name = "ProducerOrEquivalents";

			var split = new SplitContainer {
				Height = 200,
				Orientation = Orientation.Vertical,
				Dock = DockStyle.Bottom
			};
			split.Panel1.Controls.Add(assortment.Host);
			split.Panel2.Controls.Add(synonymsTable.Host);

			Controls.Add(excludes.Host);
			Controls.Add(split);
			Controls.Add(new Legend("WithoutOffers", "SameAsCurrent"));
			Controls.Add(new ToolStrip()
				.Item(new ToolStripButton { CheckOnClick = true, Name = "ShowHidden", Text = "Показать скрытых" })
				.Item(new ToolStripButton { CheckOnClick = true, Name = "ShowPharmacie", Text = "Показать только фармацевтику" })
				.Separator()
				.Button("AddToAssortment", "Добавить в ассортимент")
				.Button("DoNotShow", "Больше не показывать")
				.Button("MistakenExclude", "Ошибочное исключение")
				.Button("DeleteSynonym", "Ошибочное сопоставление по наименованию")
				.Button("MistakenProducerSynonym", "Ошибочное сопоставление по производителю")
				.Button("AddEquivalent", "Создать эквивалент")
				.Button("Обновить (F11)", Reload));
			excludes.Host.InputMap()
				.KeyDown(Keys.F11, Reload);

			Shown += (s, a) => excludes.Host.Focus();
		}

		private void Reload()
		{
			((ShowExcludesPresenter)Presenter).Refresh();
		}
	}
}
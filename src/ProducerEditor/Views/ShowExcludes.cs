using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using ProducerEditor.Contract;
using ProducerEditor.Infrastructure;
using ProducerEditor.Models;
using ProducerEditor.Presenters;
using Subway.Dom;
using Subway.Dom.Styles;
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
			((ShowExcludesPresenter) Presenter).page = pager;
			Wire();
		}

		protected override void Init()
		{
			var excludes = new VirtualTable(new TemplateManager<List<ExcludeDto>, ExcludeDto>(
				() => {
					var row = Row.Headers(
						new Header("Продукт").Sortable("Catalog"),
						new Header("Оригинальное наименование").Sortable("Catalog"),
						new Header("Синоним").Sortable("ProducerSynonym"),
						new Header("Поставщик").Sortable("Supplier"),
						new Header("Регион").Sortable("Region"),
						new Header("Оператор").Sortable("Operator")
						);
					return row;
				},
				e => Row.Cells(e.Catalog, e.OriginalSynonym, e.ProducerSynonym, e.Supplier, e.Region, e.Operator)
			));

			excludes.Host.Name = "Excludes";
			excludes.Host.Tag = PaginatorExtention.TableName;

			var synonymsTable = new VirtualTable(new TemplateManager<List<ProducerSynonymDto>, ProducerSynonymDto>(
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

			var assortment = new VirtualTable(new TemplateManager<List<ProducerOrEquivalentDto>, ProducerOrEquivalentDto>(
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
				.Item(new ToolStripButton{CheckOnClick = true, Name = "ShowHidden", Text = "Показать скрытых"})
				.Item(new ToolStripButton{CheckOnClick = true, Name = "ShowPharmacie", Text = "Показать только фармацевтику"})
				.Separator()
				.Button("AddToAssortment", "Добавить в ассортимент")
				.Button("DoNotShow", "Больше не показывать")
				.Button("MistakenExclude", "Ошибочное исключение")
				.Button("DeleteSynonym", "Ошибочное сопоставление по наименованию")
				.Button("MistakenProducerSynonym", "Ошибочное сопоставление по производителю")
				.Button("AddEquivalent", "Создать эквивалент"));

			Shown += (s, a) => excludes.Host.Focus();
		}
	}

	public class Legend : UserControl
	{
		Dictionary<string, string> knownStyles = new Dictionary<string, string>{
			{"WithoutOffers", "Есть предложения"},
			{"SameAsCurrent", "Синоним аналог"}
		};

		public Legend(params string[] styles)
		{
			Dock = DockStyle.Bottom;
			AutoSize = true;
			var flowPanel = new FlowLayoutPanel();
			flowPanel.AutoSize = true;
			flowPanel.SuspendLayout();
			SuspendLayout();

			foreach (var item in styles)
			{
				var label = knownStyles[item];
				Color color = Color.White;
				if (item == "SameAsCurrent")
					color = Color.FromArgb(222, 201, 231);
				else if (item == "WithoutOffers")
					color = Color.FromArgb(231, 231, 200);
/*
 * не работает тк mix не зранит в стиле значение с которым надо смешивать
 * надо что то придумать, например хранить в стиле значение и смешивать правдо непонятно что будет применяться
 				var style = StylesHolder.Instance.GetStyle(item);
				var styleColor = style.Get(StyleElementType.BackgroundColor);
				var color = Color.FromArgb(styleColor.R, styleColor.G, styleColor.B);
*/
				flowPanel.Controls.Add(new Label {
					AutoSize = true,
					BackColor = color,
					Margin = new Padding(5, 5, 0, 5),
					Padding = new Padding(styles.Length),
					Text = label,
					TextAlign = ContentAlignment.MiddleCenter
				});
			}

			flowPanel.Dock = DockStyle.Fill;

			Controls.Add(flowPanel);
			Name = "Legend";
			flowPanel.ResumeLayout(false);
			ResumeLayout(false);
		}
	}
}

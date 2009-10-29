using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ProducerEditor.Models;
using Subway.Dom;
using Subway.VirtualTable;
using Subway.VirtualTable.Behaviors;
using Subway.VirtualTable.Behaviors.Selection;

namespace ProducerEditor.Views
{
	public class ShowExcludes : View
	{
		private VirtualTable excludeTable;

		public ShowExcludes(Pager<Exclude> pager)
		{
			Text = "Исключения";
			MinimumSize = new Size(640, 480);

			var tools = new ToolStrip()
				.Button("Добавить в ассортимент", AddToAssortiment)
				.Button("Больше не показывать", DoNotShow);

			var navigation = new ToolStrip()
				.Button("Prev", "Передыдущая страница")
				.Label("PageLabel", "")
				.Button("Next", "Следующая страница");

			excludeTable = new VirtualTable(new TemplateManager<List<Exclude>, Exclude>(
				() => Row.Headers("Продукт", "Производитель", "Синоним", "Поставщик", "Регион"),
				e => Row.Cells(e.Catalog, e.Producer, e.ProducerSynonym, e.Supplier, e.Region)));

			excludeTable.CellSpacing = 1;
			excludeTable.RegisterBehavior(new RowSelectionBehavior(),
				new ToolTipBehavior(),
				new ColumnResizeBehavior());
			excludeTable.Behavior<ColumnResizeBehavior>().ColumnResized += column => WidthHolder.Update(excludeTable, column, WidthHolder.ExcludeWidths);
			excludeTable.TemplateManager.ResetColumns();


			excludeTable.TemplateManager.Source = pager.Content.ToList();

			Controls.Add(excludeTable.Host);
			Controls.Add(navigation);
			Controls.Add(tools);

			navigation.ActAsPaginator(pager, page => {
				var result = Request(s => s.ShowExcludes(page));
				excludeTable.TemplateManager.Source = result.Content.ToList();
				return result;
			});

			Shown += (s, a) => excludeTable.Host.Focus();
		}

		public void AddToAssortiment()
		{
			var exclude = excludeTable.Selected<Exclude>();

			if (exclude == null)
				return;

			Action(s => s.AddToAssotrment(exclude.Id));
			((List<Exclude>)excludeTable.TemplateManager.Source).Remove(exclude);
			excludeTable.RebuildViewPort();
		}

		public void DoNotShow()
		{
			var exclude = excludeTable.Selected<Exclude>();

			if (exclude == null)
				return;

			Action(s => s.DoNotShow(exclude.Id));
			((List<Exclude>)excludeTable.TemplateManager.Source).Remove(exclude);
			excludeTable.RebuildViewPort();
		}
	}
}

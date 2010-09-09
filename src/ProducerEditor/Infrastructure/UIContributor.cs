using System;
using System.Collections.Generic;
using System.Linq;
using Subway.Dom;
using Subway.Dom.Styles;
using Subway.VirtualTable;
using Subway.VirtualTable.Behaviors;
using Subway.VirtualTable.Behaviors.Selection;
using Subway.VirtualTable.Behaviors.Specialized;

namespace ProducerEditor.Infrastructure
{
	public class UIContributor
	{
		public void Contribute(View view)
		{
			var tables = view.Children().OfType<TableHost>();
			foreach (var holder in tables)
			{
				var table = holder.Table;
				table.CellSpacing = 1;
				table.RegisterBehavior(
					new ToolTipBehavior(),
					new SortInList(),
					new RowSelectionBehavior());

				if (!String.IsNullOrEmpty(holder.Name))
				{
					var name = view.GetType().Name + "." + holder.Name;

					if (!WidthHolder.Widths.ContainsKey(name))
						WidthHolder.Widths.Add(name, new List<int>());

					var widths = WidthHolder.Widths[name];

					var resizeBehavior = new ColumnResizeBehavior();
					resizeBehavior.ColumnResized += column => WidthHolder.Update(table, column, widths);
					table.RegisterBehavior(resizeBehavior);
					table.TemplateManager.RegisterDecorator(new SetColumnSizeDecorator(widths));
				}
			}
		}
	}

	public class SetColumnSizeDecorator : IDecorator
	{
		private List<int> _widths;

		public SetColumnSizeDecorator(List<int> widths)
		{
			_widths = widths;
		}

		public void Decorate(Row row, int rowIndex)
		{
			if (rowIndex == ViewPort.HeaderRowIndex)
			{
				var index = 0;
				foreach (var child in row.Children/*.Where(c => !c.ReadonlyStyle.Get(StyleElementType.IsFixed))*/)
				{
					if (_widths.Count <= index)
						_widths.Add(100);

					child.InlineStyle.Set(StyleElementType.Width, _widths[index]);
					index++;
				}
			}
		}

		public void Reset()
		{}
	}
}
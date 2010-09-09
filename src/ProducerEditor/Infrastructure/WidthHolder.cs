using System.Collections.Generic;
using System.Linq;
using Common.Tools;
using Subway.Dom;
using Subway.Dom.Styles;
using Subway.VirtualTable;

namespace ProducerEditor.Infrastructure
{
	public class WidthHolder
	{
		public static List<int> ProducerWidths = Enumerable.Repeat(100, 4).ToList();
		public static List<int> OffersWidths = Enumerable.Repeat(100, 4).ToList();
		public static List<int> ReportWidths = Enumerable.Repeat(100, 6).ToList();
		public static List<int> ProductsAndProducersWidths = Enumerable.Repeat(100, 5).ToList();
		public static List<int> SyspiciosSynonyms = Enumerable.Repeat(100, 6).ToList();
		public static List<int> AssortimentWidths = Enumerable.Repeat(100, 3).ToList();

		public static Dictionary<string, List<int>> Widths = new Dictionary<string, List<int>>();

		public static void Update(VirtualTable table, Column column, List<int> widths)
		{
			var element = column;
			do
			{
				widths[table.Columns.IndexOf(element)] = element.ReadonlyStyle.Get(StyleElementType.Width);
				var node = table.Columns.Find(element).Next;
				if (node != null)
					element = node.Value;
				else
					element = null;
			}
			while(element != null);
		}
	}
}
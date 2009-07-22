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
	public class OffersView : Form
	{
		private static List<int> _widths = new List<int>{
			100, 100, 100, 100
		};


		public OffersView(List<OfferView> offers)
		{
			MinimumSize = new Size(640, 480);
			KeyPreview = true;
			Text = "Предложения";
			var offersTable = new VirtualTable(new TemplateManager<List<OfferView>, OfferView>(
												() => { 
													var row = Row.Headers(); 

													var header = new Header("Поставщик").Sortable("Supplier");
													header.InlineStyle.Set(StyleElementType.Width, _widths[0]);
													row.Append(header);

			                                   	    header = new Header("Сегмент").Sortable("Segment");
													header.InlineStyle.Set(StyleElementType.Width, _widths[1]);
													row.Append(header);

													header = new Header("Наименование").Sortable("ProductSynonym");
													header.InlineStyle.Set(StyleElementType.Width, _widths[2]);
													row.Append(header);

													header = new Header("Производитель").Sortable("ProducerSynonym");
													header.InlineStyle.Set(StyleElementType.Width, _widths[3]);
													row.Append(header);
													return row;
												},
			                                   	offer => Row.Cells(offer.Supplier,
			                                   	                   offer.SegmentAsString(),
			                                   	                   offer.ProductSynonym,
			                                   	                   offer.ProducerSynonym)));
			offersTable.CellSpacing = 1;
			offersTable.RegisterBehavior(new ToolTipBehavior(),
										new ColumnResizeBehavior(),
			                             new SortInList());
			offersTable.TemplateManager.Source = offers;
			offersTable.Behavior<ColumnResizeBehavior>().ColumnResized += column => {
				var element = column;
				do
				{
					_widths[offersTable.Columns.IndexOf(element)] = element.ReadonlyStyle.Get(StyleElementType.Width);
					var node = offersTable.Columns.Find(element).Next;
					if (node != null)
						element = (Column) node.Value;
					else
						element = null;
				}
				while(element != null);
			};
			offersTable.TemplateManager.ResetColumns();
			Controls.Add(offersTable.Host);
			this.InputMap().KeyDown(Keys.Escape, Close);
		}
	}
}
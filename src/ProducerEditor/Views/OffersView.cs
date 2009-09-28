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
		public OffersView(List<OfferView> offers)
		{
			MinimumSize = new Size(640, 480);
			KeyPreview = true;
			Text = "Предложения";
			var offersTable = new VirtualTable(new TemplateManager<List<OfferView>, OfferView>(
												() => { 
													var row = Row.Headers(); 

													var header = new Header("Поставщик").Sortable("Supplier");
													header.InlineStyle.Set(StyleElementType.Width, WidthHolder.OffersWidths[0]);
													row.Append(header);

			                                   	    header = new Header("Сегмент").Sortable("Segment");
													header.InlineStyle.Set(StyleElementType.Width, WidthHolder.OffersWidths[1]);
													row.Append(header);

													header = new Header("Наименование").Sortable("ProductSynonym");
													header.InlineStyle.Set(StyleElementType.Width, WidthHolder.OffersWidths[2]);
													row.Append(header);

													header = new Header("Производитель").Sortable("ProducerSynonym");
													header.InlineStyle.Set(StyleElementType.Width, WidthHolder.OffersWidths[3]);
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
			offersTable.Behavior<ColumnResizeBehavior>().ColumnResized += column => WidthHolder.Update(offersTable, column, WidthHolder.OffersWidths);
			offersTable.TemplateManager.ResetColumns();
			Controls.Add(offersTable.Host);
			this.InputMap().KeyDown(Keys.Escape, Close);
		}
	}
}
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
	public class OffersBySynonym : Form
	{
		public OffersBySynonym(List<Offer> offers)
		{
			MinimumSize = new Size(640, 480);
			KeyPreview = true;
			Text = "Предложения";
			var offersTable = new VirtualTable(new TemplateManager<List<Offer>, Offer>(
				() => { 
					var row = Row.Headers(); 

					var header = new Header("Наименование").Sortable("ProductSynonym");
					header.InlineStyle.Set(StyleElementType.Width, WidthHolder.OffersWidths[2]);
					row.Append(header);

					header = new Header("Производитель").Sortable("ProducerSynonym");
					header.InlineStyle.Set(StyleElementType.Width, WidthHolder.OffersWidths[3]);
					row.Append(header);
					return row;
				},
				offer => Row.Cells(offer.Product,
					offer.Producer)
			));

			offersTable.CellSpacing = 1;
			offersTable.RegisterBehavior(new ToolTipBehavior(),
			                             new ColumnResizeBehavior(),
			                             new SortInList());
			offersTable.TemplateManager.Source = offers;
			offersTable.Behavior<ColumnResizeBehavior>().ColumnResized += column => WidthHolder.Update(offersTable, column, WidthHolder.OffersBySynonymView);
			offersTable.TemplateManager.ResetColumns();
			Controls.Add(offersTable.Host);
			this.InputMap().KeyDown(Keys.Escape, Close);
		}
	}
}

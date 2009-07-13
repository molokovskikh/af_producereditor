using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using ProducerEditor.Models;
using Subway.Dom;
using Subway.VirtualTable;
using Subway.VirtualTable.Behaviors;
using Subway.VirtualTable.Behaviors.Specialized;

namespace ProducerEditor.Views
{
	public class ProductsAndProducersView : Form
	{
		public ProductsAndProducersView(Producer producer, List<ProductAndProducer> productAndProducers)
		{
			MinimumSize = new Size(640, 480);
			Text = "Продукты";
			var offersTable = new VirtualTable(new TemplateManager<List<ProductAndProducer>, ProductAndProducer>(
			                                   	() => Row.Headers(new Header("Продукт").Sortable("Product"),
			                                   	                  new Header("Производитель").Sortable("Producer")),
			                                   	offer => Row.Cells(offer.Product,
			                                   	                   offer.Producer)));
			offersTable.RegisterBehavior(new ToolTipBehavior(),
			                             new SortInList());
			offersTable.TemplateManager.Source = productAndProducers;
			Controls.Add(offersTable.Host);
		}
	}
}

using System;
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
		public ProductsAndProducersView(Producer producer, List<ProductAndProducer> productAndProducers, int offersCount, int ordersCount)
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
			var flowPanel = new FlowLayoutPanel
			                	{
									Padding = new Padding(0, 5, 0, 5),
			                		AutoSize = true,
									Dock = DockStyle.Top
			                	};
			flowPanel.Controls.Add(new Label{ AutoSize = true, Text = String.Format("Количество предложений {0}", offersCount)});
			flowPanel.Controls.Add(new Label { AutoSize = true, Text = String.Format("Количество закзов {0}", ordersCount) });
			Controls.Add(offersTable.Host);
			Controls.Add(flowPanel);
		}
	}
}

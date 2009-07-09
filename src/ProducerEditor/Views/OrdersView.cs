using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ProducerEditor.Models;
using Subway.Dom;
using Subway.VirtualTable;
using Subway.VirtualTable.Behaviors;

namespace ProducerEditor.Views
{
	public class OrdersView : Form
	{
		public OrdersView(List<OrderView> orders, Producer producer)
		{
			MinimumSize = new Size(640, 480);
			Text = String.Format(@"Последнии 20 заказов по производителю ""{0}""", producer.Name);
			var offersTable = new VirtualTable(new TemplateManager<List<OrderView>, OrderView>(
												() => {
													return Row.Headers("Дата заказа", "Поставщик", "Аптека",  "Наименование", "Производитель");
												},
												order => {
													return Row.Cells(order.WriteTime, order.Supplier, order.Drugstore, order.ProductSynonym, order.ProducerSynonym);
												}));
			offersTable.TemplateManager.Source = orders;
			offersTable.RegisterBehavior(new ToolTipBehavior());
			Controls.Add(offersTable.Host);
		}
	}
}
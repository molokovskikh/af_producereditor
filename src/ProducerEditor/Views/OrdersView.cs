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
	public class OrdersView : Form
	{
		public OrdersView(List<OrderView> orders, Producer producer)
		{
			MinimumSize = new Size(640, 480);
			Text = String.Format(@"Последнии 20 заказов по производителю ""{0}""", producer.Name);
			var offersTable = new VirtualTable(new TemplateManager<List<OrderView>, OrderView>(
			                                   	() => Row.Headers(new Header("Дата заказа").Sortable("WriteTime"),
																  new Header("Поставщик").Sortable("Supplier"),
																  new Header("Аптека").Sortable("Drugstore"),
																  new Header("Наименование").Sortable("ProductSynonym"),
																  new Header("Производитель").Sortable("ProducerSynonym")),
												order => Row.Cells(order.WriteTime,
												                   order.Supplier,
												                   order.Drugstore,
												                   order.ProductSynonym,
												                   order.ProducerSynonym)));
			offersTable.TemplateManager.Source = orders;
			offersTable.RegisterBehavior(new ToolTipBehavior(),
			                             new SortInList());
			Controls.Add(offersTable.Host);
		}
	}
}
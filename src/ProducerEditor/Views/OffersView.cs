using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using ProducerEditor.Models;
using Subway.Dom;
using Subway.VirtualTable;
using Subway.VirtualTable.Behaviors;

namespace ProducerEditor.Views
{
	public class OffersView : Form
	{
		public OffersView(List<OfferView> offers, Producer producer)
		{
			MinimumSize = new Size(640, 480);
			Text = String.Format(@"Предложения производителя ""{0}""", producer.Name);
			var offersTable = new VirtualTable(new TemplateManager<List<OfferView>, OfferView>(
			                                   	() => {
			                                   		return Row.Headers("Поставщик", "Сегмент", "Наименование", "Производитель");
			                                   	},
												offer => {
													return Row.Cells(offer.Supplier, offer.SegmentAsString(), offer.ProductSynonym, offer.ProducerSynonym);
												}));
			offersTable.RegisterBehavior(new ToolTipBehavior());
			offersTable.TemplateManager.Source = offers;
			Controls.Add(offersTable.Host);
		}
	}
}
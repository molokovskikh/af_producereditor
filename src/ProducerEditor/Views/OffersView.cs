﻿using System;
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
	public class OffersView : Form
	{
		public OffersView(List<OfferView> offers, Producer producer)
		{
			MinimumSize = new Size(640, 480);
			Text = String.Format(@"Предложения производителя ""{0}""", producer.Name);
			var offersTable = new VirtualTable(new TemplateManager<List<OfferView>, OfferView>(
												() => Row.Headers(new Header("Поставщик").Sortable("Supplier"),
			                                   	                  new Header("Сегмент").Sortable("Segment"),
																  new Header("Наименование").Sortable("ProductSynonym"),
																  new Header("Производитель").Sortable("ProducerSynonym")),
			                                   	offer => Row.Cells(offer.Supplier,
			                                   	                   offer.SegmentAsString(),
			                                   	                   offer.ProductSynonym,
			                                   	                   offer.ProducerSynonym)));
			offersTable.RegisterBehavior(new ToolTipBehavior(),
			                             new SortInList());
			offersTable.TemplateManager.Source = offers;
			Controls.Add(offersTable.Host);
		}
	}
}
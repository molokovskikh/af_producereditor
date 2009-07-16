﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Common.Tools;
using ProducerEditor.Models;
using Subway.Dom;
using Subway.Dom.Input;
using Subway.Helpers;
using Subway.VirtualTable;
using Subway.VirtualTable.Behaviors;
using Subway.VirtualTable.Behaviors.Selection;
using Subway.VirtualTable.Behaviors.Specialized;

namespace ProducerEditor.Views
{
	public class ProductsAndProducersView : Form
	{
		private Controller _controller;
		private Producer _producer;
		private List<ProductAndProducer> _productAndProducers;
		private VirtualTable productsAndProducers;

		public ProductsAndProducersView(Controller controller, Producer producer, List<ProductAndProducer> productAndProducers)
		{
			_controller = controller;
			_producer = producer;
			_productAndProducers = productAndProducers;

			MinimumSize = new Size(640, 480);
			Text = "Продукты";
			KeyPreview = true;
			KeyDown += (sender, args) => {
			           	if (args.KeyCode == Keys.Escape)
			           		Close();
			           };

			productsAndProducers = new VirtualTable(new TemplateManager<List<ProductAndProducer>, ProductAndProducer>(
												() => Row.Headers(new Header().AddClass("CheckBoxColumn"),
																  new Header("Продукт").Sortable("Product"),
			                                   	                  new Header("Производитель").Sortable("Producer"),
																  new Header("Количество предложений").Sortable("OffersCount"),
																  new Header("Количество заказов").Sortable("OrdersCount")),
												offer =>
												{
													var row = Row.Cells(offer.Product,
													          offer.Producer,
													          offer.OffersCount,
													          offer.OrdersCount);
													if (offer.ExistsInRls == 0)
														row.AddClass("NotExistsInRls");
													if (offer.ProducerId != producer.Id)
														row.Prepend(new CheckBoxInput(offer.Selected));
													else
														row.Prepend(new Cell());
													return row;
												}));
			productsAndProducers.CellSpacing = 1;
			productsAndProducers.RegisterBehavior(new ToolTipBehavior(),
			                             new SortInList(),
			                             new RowSelectionBehavior(),
			                             new InputSupport(input => {
			                                              	var row = (Row) input.Parent.Parent;
			                                              	var productAndProducer = productsAndProducers.Translate<ProductAndProducer>(row);
			                                              	((IList<ProductAndProducer>) productsAndProducers.TemplateManager.Source)
			                                              		.Where(p => p.ProducerId == productAndProducer.ProducerId)
			                                              		.Each(p => p.Selected = ((CheckBoxInput) input).Checked);
															productsAndProducers.RebuildViewPort();
			                                              }));
			productsAndProducers.TemplateManager.Source = productAndProducers;
			productsAndProducers.Host.InputMap()
				.KeyDown(Keys.F3, Join)
				.KeyDown(Keys.F4, ShowOffersForProducerId)
				.KeyDown(Keys.F5, ShowOffersForCatalogId);

			var toolStrip = new ToolStrip();
			toolStrip
				.Button(String.Format("Объединить c {0} (F3)", producer.Name), Join)
				.Button("Предложения для производителя (F4)", ShowOffersForProducerId)
				.Button("Предложения для продукта (F5)", ShowOffersForCatalogId);

			Controls.Add(productsAndProducers.Host);
			Controls.Add(toolStrip);
		}

		public void ShowOffersForCatalogId()
		{
			var productAndProducer = productsAndProducers.Selected<ProductAndProducer>();
			if (productAndProducer != null)
				_controller.OffersForCatalogId(productAndProducer.CatalogId);
		}

		public void ShowOffersForProducerId()
		{
			var productAndProducer = productsAndProducers.Selected<ProductAndProducer>();
			if (productAndProducer != null)
				_controller.OfferForProducerId(productAndProducer.ProducerId);
		}

		private void Join()
		{
			_controller.DoJoin(_productAndProducers
			                   	.Where(p => p.Selected)
			                   	.Select(p => new Producer {Id = p.ProducerId, Name = p.Producer})
			                   	.GroupBy(p => p.Id)
			                   	.Select(g => g.First()).ToArray(), _producer);

			productsAndProducers.TemplateManager.Source = _controller.FindRelativeProductsAndProducers(_producer);
		}
	}
}

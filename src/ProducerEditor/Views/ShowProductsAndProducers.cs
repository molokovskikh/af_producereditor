using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Common.Tools;
using ProducerEditor.Infrastructure;
using ProducerEditor.Models;
using Subway.Dom;
using Subway.Dom.Input;
using Subway.Dom.Styles;
using Subway.Helpers;
using Subway.Table;
using Subway.VirtualTable;
using Subway.VirtualTable.Behaviors;
using Subway.VirtualTable.Behaviors.Selection;
using Subway.VirtualTable.Behaviors.Specialized;
using View = ProducerEditor.Infrastructure.View;

namespace ProducerEditor.Views
{
	public class ShowProductsAndProducers : View
	{
		private ProducerDto producer;
		private List<ProductAndProducer> productAndProducers;
		private VirtualTable productsAndProducers;
		private List<ProducerDto> producers;

		public ShowProductsAndProducers(ProducerDto producer, List<ProducerDto> producers, List<ProductAndProducer> productAndProducers)
		{
			this.producer = producer;
			this.productAndProducers = productAndProducers;
			this.producers = producers;

			MinimumSize = new Size(640, 480);
			Size = new Size(640, 480);
			Text = "Продукты";
			KeyPreview = true;
			KeyDown += (sender, args) => {
				if (args.KeyCode == Keys.Escape)
					Close();
			};

			productsAndProducers = new VirtualTable(new TemplateManager<List<ProductAndProducer>, ProductAndProducer>(
				() => {
					var row = Row.Headers(new Header().AddClass("CheckBoxColumn"));

					var header = new Header("Продукт").Sortable("Product");
					header.InlineStyle.Set(StyleElementType.Width, WidthHolder.ProductsAndProducersWidths[0]);
					row.Append(header);

					header = new Header("Производитель").Sortable("Producer");
					header.InlineStyle.Set(StyleElementType.Width, WidthHolder.ProductsAndProducersWidths[1]);
					row.Append(header);

					header = new Header("Количество предложений").Sortable("OffersCount");
					header.InlineStyle.Set(StyleElementType.Width, WidthHolder.ProductsAndProducersWidths[2]);
					row.Append(header);

					header = new Header("Количество заказов").Sortable("OrdersCount");
					header.InlineStyle.Set(StyleElementType.Width, WidthHolder.ProductsAndProducersWidths[3]);
					row.Append(header);
					return row;
				},
				offer => {
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
			productsAndProducers.RegisterBehavior(
				new ToolTipBehavior(),
				new SortInList(),
				new RowSelectionBehavior(),
				new ColumnResizeBehavior(),
				new InputSupport(input => {
					var row = (Row) input.Parent.Parent;
					var productAndProducer = productsAndProducers.Translate<ProductAndProducer>(row);
					((IList<ProductAndProducer>) productsAndProducers.TemplateManager.Source)
						.Where(p => p.ProducerId == productAndProducer.ProducerId)
						.Each(p => p.Selected = ((CheckBoxInput) input).Checked);
					productsAndProducers.RebuildViewPort();
				}));
			productsAndProducers.TemplateManager.Source = productAndProducers;
			productsAndProducers.Behavior<ColumnResizeBehavior>().ColumnResized += column => WidthHolder.Update(productsAndProducers, column, WidthHolder.ProductsAndProducersWidths);
			productsAndProducers.Host.InputMap()
				.KeyDown(Keys.F3, Join)
				.KeyDown(Keys.F4, ShowOffersForProducerId)
				.KeyDown(Keys.F5, ShowOffersForCatalogId);

			var toolStrip = new ToolStrip();
			toolStrip
				.Button(String.Format("Объединить c {0} (F3)", producer.Name), Join)
				.Button("Предложения для производителя (F4)", ShowOffersForProducerId)
				.Button("Предложения для продукта (F5)", ShowOffersForCatalogId);

			productsAndProducers.TemplateManager.ResetColumns();
			Controls.Add(productsAndProducers.Host);
			Controls.Add(toolStrip);
		}

		public void ShowOffersForCatalogId()
		{
			var productAndProducer = productsAndProducers.Selected<ProductAndProducer>();
			if (productAndProducer != null)
				Controller(s => s.ShowOffers(new OffersQuery("CatalogId", productAndProducer.CatalogId)))();
		}

		public void ShowOffersForProducerId()
		{
			var productAndProducer = productsAndProducers.Selected<ProductAndProducer>();
			if (productAndProducer != null)
				Controller(s => s.ShowOffers(new OffersQuery("ProducerId", productAndProducer.ProducerId)))();
		}

		private void Join()
		{
			var joinedProducers = productAndProducers
				.Where(p => p.Selected)
				.Select(p => new ProducerDto {Id = p.ProducerId, Name = p.Producer})
				.GroupBy(p => p.Id)
				.Select(g => g.First()).ToArray();
			Action(s => s.DoJoin(joinedProducers.Select(source => source.Id).ToArray(), producer.Id));
			foreach (var source in joinedProducers)
				producers.Remove(source);

			Action(s => {
				productsAndProducers.TemplateManager.Source = s.ShowProductsAndProducers(producer.Id);
			});
		}
	}
}

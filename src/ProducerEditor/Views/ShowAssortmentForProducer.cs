using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ProducerEditor.Infrastructure;
using ProducerEditor.Models;
using Subway.Dom;
using Subway.Dom.Base;
using Subway.Dom.Input;
using Subway.Helpers;
using Subway.Table;
using Subway.VirtualTable;
using Subway.VirtualTable.Behaviors;
using Subway.VirtualTable.Behaviors.Selection;
using View = ProducerEditor.Infrastructure.View;

namespace ProducerEditor.Views
{
	public class ShowAssortmentForProducer : View
	{
		private VirtualTable assortmentTable;

		public ShowAssortmentForProducer(uint producerId, Pager<Assortment> assortments)
		{
			Text = "Ассортимент";
			MinimumSize = new Size(640, 480);

			var tools = new ToolStrip()
				.Button("Удалить (Delete)", Delete);

			var navigationToolStrip = new ToolStrip()
				.Button("Prev", "Предыдущая страница")
				.Label("PageLabel", "")
				.Button("Next", "Следующая страница");

			assortmentTable = new VirtualTable(new TemplateManager<List<Assortment>, Assortment>(
				() => Row.Headers(new Header("Проверен").AddClass("CheckBoxColumn1"), "Продукт", "Производитель"),
				a => {
					var row = Row.Cells(new CheckBoxInput(a.Checked).Attr("Name", "Checked"), a.Product, a.Producer);
					if (a.Id == Settings.Default.BookmarkAssortimentId)
						((IDomElementWithChildren)row.Children.ElementAt(1)).Prepend(new TextBlock {Class = "BookmarkGlyph"});
					return row;
				}));
			assortmentTable.CellSpacing = 1;
			assortmentTable.RegisterBehavior(
				new RowSelectionBehavior(),
				new ToolTipBehavior(),
				new ColumnResizeBehavior(),
				new InputController()
			);
			assortmentTable.Behavior<ColumnResizeBehavior>().ColumnResized += column => WidthHolder.Update(assortmentTable, column, WidthHolder.AssortimentWidths);
			assortmentTable.TemplateManager.ResetColumns();
			assortmentTable.Host
				.InputMap()
				.KeyDown(Keys.Delete, Delete);

			assortmentTable.Host.InputMap()
				.KeyDown(Keys.Escape, Close);

			Controls.Add(assortmentTable.Host);
			Controls.Add(navigationToolStrip);
			Controls.Add(tools);

			navigationToolStrip.ActAsPaginator(
				assortments,
				page => {
					Pager<Assortment> pager = null;
					Action(s => {
						pager = s.ShowAssortmentForProducer(producerId, page);
					});
					assortmentTable.TemplateManager.Source = pager.Content.ToList();
					return pager;
				});

			assortmentTable.TemplateManager.Source = assortments.Content.ToList();

			Shown += (s, a) => assortmentTable.Host.Focus();
		}

		private void Delete()
		{
			var assortment = assortmentTable.Selected<Assortment>();
			if (assortment == null)
				return;

			Action(s => s.DeleteAssortment(assortment.Id));
			((List<Assortment>)assortmentTable.TemplateManager.Source).Remove(assortment);
			assortmentTable.RebuildViewPort();
		}

	}
}

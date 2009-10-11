using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ProducerEditor.Models;
using Subway.Dom;
using Subway.Dom.Base;
using Subway.Helpers;
using Subway.VirtualTable;
using Subway.VirtualTable.Behaviors;
using Subway.VirtualTable.Behaviors.Selection;
using Common.Tools;

namespace ProducerEditor.Views
{
	public class ShowAssortment : View
	{
		private ToolStrip tools;
		private ToolStrip bookmarksToolStrip;
		private VirtualTable assortimentTable;

		private Pager<IList<Assortment>> assortiments;
		
		public ShowAssortment(Pager<IList<Assortment>> assortiments)
		{
			Text = "Ассортимент";
			MinimumSize = new Size(640, 480);

			tools = new ToolStrip()
				.Edit("SearchText")
				.Button("Поиск", Search);

			bookmarksToolStrip = new ToolStrip()
				.Button("К закаладке", MoveToBookmark)
				.Button("Установить закладку", SetBookmark)
				.Separator()
				.Button("Prev", "Предыдущая страница", Prev)
				.Label("PageLabel", "")
				.Button("Next", "Следующая страница", Next);

			assortimentTable = new VirtualTable(new TemplateManager<List<Assortment>, Assortment>(
				() => Row.Headers("Продукт", "Производитель"),
				a => {
					var row = Row.Cells(a.Product, a.Producer);
					if (a.Id == Settings.Default.BookmarkAssortimentId)
						((IDomElementWithChildren)row.Children.First()).Prepend(new TextBlock {Class = "BookmarkGlyph"});
					return row;
				}));
			assortimentTable.CellSpacing = 1;
			assortimentTable.RegisterBehavior(new RowSelectionBehavior(),
				new ToolTipBehavior(),
				new ColumnResizeBehavior());
			assortimentTable.Behavior<ColumnResizeBehavior>().ColumnResized += column => WidthHolder.Update(assortimentTable, column, WidthHolder.AssortimentWidths);
			assortimentTable.TemplateManager.ResetColumns();

			UpdateAssortment(assortiments);

			var searchText = ((ToolStripTextBox)tools.Items["SearchText"]);
			searchText.KeyDown += (sender,args) => {
				if (args.KeyCode == Keys.Enter)
					Search();
			};

			assortimentTable.Host.InputMap()
				.KeyDown(Keys.Enter, Search)
				.KeyDown(Keys.Escape, () => searchText.Text = "")
				.KeyPress((o, a) => searchText.Text += a.KeyChar);

			Controls.Add(assortimentTable.Host);
			Controls.Add(bookmarksToolStrip);
			Controls.Add(tools);
		}

		private void Search()
		{
			var searchText = tools.Items["SearchText"].Text;
			if (String.IsNullOrEmpty(searchText))
				return;

			Action(s => {
				UpdateAssortment(s.SearchAssortment(searchText));
			});
		}

		private void Prev()
		{
			if (assortiments.Page == 1)
				return;

			Action(s => {
				UpdateAssortment(s.GetAssortmentPage(assortiments.Page - 1));
			});
		}

		private void Next()
		{
			if (assortiments.Page == assortiments.TotalPages - 1)
				return;

			Action(s => {
				UpdateAssortment(s.GetAssortmentPage(assortiments.Page + 1));
			});
		}

		private void SetBookmark()
		{
			Settings.Default.BookmarkAssortimentId = assortimentTable.Selected<Assortment>().Id;
			Settings.Default.Save();
			assortimentTable.RebuildViewPort();
		}

		private void MoveToBookmark()
		{
			if (Settings.Default.BookmarkAssortimentId == 0)
				return;

			Action(s => {
				UpdateAssortment(s.ShowAssortment(Settings.Default.BookmarkAssortimentId));
				assortimentTable.Behavior<IRowSelectionBehavior>().MoveSelectionAt(assortiments.Content.IndexOf(a => a.Id == Settings.Default.BookmarkAssortimentId));
			});
		}

		private void UpdateAssortment(Pager<IList<Assortment>> pager)
		{
			assortiments = pager;
			assortimentTable.TemplateManager.Source = assortiments.Content.ToList();
			bookmarksToolStrip.Items["PageLabel"].Text = String.Format("Страница {0} из {1}", assortiments.Page, assortiments.TotalPages);
			bookmarksToolStrip.Items["Next"].Enabled = assortiments.Page < assortiments.TotalPages - 1;
			bookmarksToolStrip.Items["Prev"].Enabled = assortiments.Page > 1;
		}
	}
}

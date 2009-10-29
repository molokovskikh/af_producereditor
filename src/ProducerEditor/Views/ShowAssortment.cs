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

		private Pager<Assortment> assortiments;
		
		public ShowAssortment(Pager<Assortment> assortiments)
		{
			Text = "Ассортимент";
			MinimumSize = new Size(640, 480);

			tools = new ToolStrip()
				.Edit("SearchText")
				.Button("Поиск", Search)
				.Separator()
				.Button("Удалить (Delete)", Delete);

			bookmarksToolStrip = new ToolStrip()
				.Button("К закаладке", MoveToBookmark)
				.Button("Установить закладку", SetBookmark)
				.Separator()
				.Button("Prev", "Предыдущая страница")
				.Label("PageLabel", "")
				.Button("Next", "Следующая страница");

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
			assortimentTable.Host
				.InputMap()
				.KeyDown(Keys.Delete, Delete);

			UpdateAssortment(assortiments);

			var searchText = ((ToolStripTextBox)tools.Items["SearchText"]);
			searchText.KeyDown += (sender,args) => {
				if (args.KeyCode == Keys.Enter)
					Search();
			};

			assortimentTable.Host.InputMap()
				.KeyDown(Keys.Enter, Search)
				.KeyDown(Keys.Escape, () => searchText.Text = "")
				.KeyPress((o, a) => {
					if (!Char.IsLetterOrDigit(a.KeyChar))
						return;
					searchText.Text += a.KeyChar;
				});

			Controls.Add(assortimentTable.Host);
			Controls.Add(bookmarksToolStrip);
			Controls.Add(tools);

			bookmarksToolStrip.ActAsPaginator(assortiments,
				page => {
					Pager<Assortment> pager = null;
					Action(s => {
						pager = s.GetAssortmentPage(page);
						UpdateAssortment(pager);
					});
					return pager;
				});

			MoveToBookmark();
			Shown += (s, a) => assortimentTable.Host.Focus();
		}

		private void Delete()
		{
			var assortment = assortimentTable.Selected<Assortment>();
			if (assortment == null)
				return;

			Action(s => s.DeleteAssortment(assortment.Id));
			((List<Assortment>)assortimentTable.TemplateManager.Source).Remove(assortment);
			assortimentTable.RebuildViewPort();
		}

		private void Search()
		{
			var searchText = tools.Items["SearchText"].Text;
			if (String.IsNullOrEmpty(searchText))
				return;

			Action(s => {
				var pager = s.SearchAssortment(searchText);
				if (pager == null)
				{
					MessageBox.Show("По вашему запросу ничего не найдено");
					return;
				}
				UpdateAssortment(pager);
				bookmarksToolStrip.UpdatePaginator(pager);
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

		private void UpdateAssortment(Pager<Assortment> pager)
		{
			assortiments = pager;
			assortimentTable.TemplateManager.Source = assortiments.Content.ToList();
		}
	}
}

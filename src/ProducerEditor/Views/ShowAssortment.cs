using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ProducerEditor.Models;
using Subway.Dom;
using Subway.Dom.Base;
using Subway.Dom.Input;
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
		private VirtualTable assortmentTable;

		private Pager<Assortment> assortiments;
		
		public ShowAssortment(Pager<Assortment> assortments)
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

			assortmentTable = new VirtualTable(new TemplateManager<List<Assortment>, Assortment>(
				() => Row.Headers(new Header("Проверен").AddClass("CheckBoxColumn1"), "Продукт", "Производитель"),
				a => {
					var row = Row.Cells(new CheckBoxInput(a.Checked), a.Product, a.Producer);
					if (a.Id == Settings.Default.BookmarkAssortimentId)
						((IDomElementWithChildren)row.Children.ElementAt(1)).Prepend(new TextBlock {Class = "BookmarkGlyph"});
					return row;
				}));
			assortmentTable.CellSpacing = 1;
			assortmentTable.RegisterBehavior(new RowSelectionBehavior(),
				new ToolTipBehavior(),
				new ColumnResizeBehavior(),
				new InputSupport(input => {
					var row = (Row)input.Parent.Parent;
					var assortment = assortmentTable.Translate<Assortment>(row);
					assortment.Checked = ((CheckBoxInput) input).Checked;
					Action(s => s.SetAssortmentChecked(assortment.Id, assortment.Checked));
				}));
			assortmentTable.Behavior<ColumnResizeBehavior>().ColumnResized += column => WidthHolder.Update(assortmentTable, column, WidthHolder.AssortimentWidths);
			assortmentTable.TemplateManager.ResetColumns();
			assortmentTable.Host
				.InputMap()
				.KeyDown(Keys.Delete, Delete);

			UpdateAssortment(assortments);

			var searchText = ((ToolStripTextBox)tools.Items["SearchText"]);
			searchText.KeyDown += (sender,args) => {
				if (args.KeyCode == Keys.Enter)
					Search();
			};

			assortmentTable.Host.InputMap()
				.KeyDown(Keys.Enter, Search)
				.KeyDown(Keys.Escape, () => searchText.Text = "")
				.KeyPress((o, a) => {
					if (!Char.IsLetterOrDigit(a.KeyChar))
						return;
					searchText.Text += a.KeyChar;
				});

			Controls.Add(assortmentTable.Host);
			Controls.Add(bookmarksToolStrip);
			Controls.Add(tools);

			bookmarksToolStrip.ActAsPaginator(assortments,
				page => {
					Pager<Assortment> pager = null;
					Action(s => {
						pager = s.GetAssortmentPage(page);
						UpdateAssortment(pager);
					});
					return pager;
				});

			MoveToBookmark();
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
			Settings.Default.BookmarkAssortimentId = assortmentTable.Selected<Assortment>().Id;
			Settings.Default.Save();
			assortmentTable.RebuildViewPort();
		}

		private void MoveToBookmark()
		{
			if (Settings.Default.BookmarkAssortimentId == 0)
				return;

			Action(s => {
				UpdateAssortment(s.ShowAssortment(Settings.Default.BookmarkAssortimentId));
				assortmentTable.Behavior<IRowSelectionBehavior>().MoveSelectionAt(assortiments.Content.IndexOf(a => a.Id == Settings.Default.BookmarkAssortimentId));
			});
		}

		private void UpdateAssortment(Pager<Assortment> pager)
		{
			assortiments = pager;
			assortmentTable.TemplateManager.Source = assortiments.Content.ToList();
		}
	}
}

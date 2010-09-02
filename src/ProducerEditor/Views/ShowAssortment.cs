using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ProducerEditor.Contract;
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
using Common.Tools;
using Subway.Dom.Styles;
using Subway.VirtualTable.Behaviors.Specialized;
using View = ProducerEditor.Infrastructure.View;

namespace ProducerEditor.Views
{
	public class ShowAssortment : View
	{
		private ToolStrip tools;
		private ToolStrip navigationToolStrip;
		private VirtualTable assortmentTable;
		private readonly VirtualTable synonymsTable;
		private readonly VirtualTable equivalentTable;

		private string _searchText;
		
		public ShowAssortment(Pager<AssortmentDto> assortments)
		{
			Text = "Ассортимент";
			MinimumSize = new Size(640, 480);

			tools = new ToolStrip()
				.Edit("SearchText")
				.Button("Поиск", Search)
				.Separator()
				.Button("Удалить (Delete)", Delete);

			navigationToolStrip = new ToolStrip()
				.Button("К закаладке", MoveToBookmark)
				.Button("Установить закладку", SetBookmark)
				.Separator()
				.Button("Prev", "Предыдущая страница")
				.Label("PageLabel", "")
				.Button("Next", "Следующая страница");

			assortmentTable = new VirtualTable(new TemplateManager<List<AssortmentDto>, AssortmentDto>(
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

			UpdateAssortment(assortments);

			var searchText = ((ToolStripTextBox)tools.Items["SearchText"]);
			searchText.KeyDown += (sender,args) => {
				if (args.KeyCode == Keys.Enter)
					Search();
			};

			assortmentTable.Host.InputMap()
				.KeyDown(Keys.Enter, Search)
				.KeyDown(Keys.Escape, () => {
					searchText.Text = "";
					if (!String.IsNullOrEmpty(_searchText))
					{
						_searchText = "";
						var pager = Request(s => s.GetAssortmentPage(0));
						UpdateAssortment(pager);
						navigationToolStrip.UpdatePaginator(pager);
					}
				})
				.KeyPress((o, a) => {
					if (!Char.IsLetterOrDigit(a.KeyChar))
						return;
					searchText.Text += a.KeyChar;
				});
			
			var behavior = assortmentTable.Behavior<IRowSelectionBehavior>();
			behavior.SelectedRowChanged += (oldRow, newRow) => SelectedAssortmentChanged(behavior.Selected<AssortmentDto>());

			synonymsTable = new VirtualTable(new TemplateManager<List<ProducerSynonymDto>, ProducerSynonymDto>(
				() => {
					var row = Row.Headers();
					var header = new Header("Синоним").Sortable("Name");
					header.InlineStyle.Set(StyleElementType.Width, WidthHolder.ProducerWidths[0]);
					row.Append(header);

					header = new Header("Поставщик").Sortable("Supplier");
					header.InlineStyle.Set(StyleElementType.Width, WidthHolder.ProducerWidths[1]);
					row.Append(header);

					header = new Header("Регион").Sortable("Region");
					header.InlineStyle.Set(StyleElementType.Width, WidthHolder.ProducerWidths[2]);
					row.Append(header);

					return row;
				},
				synonym => {
					var row = Row.Cells(synonym.Name,
						synonym.Supplier,
						synonym.Region);
					if (synonym.HaveOffers)
						row.AddClass("WithoutOffers");
					return row;
				}));
			synonymsTable.CellSpacing = 1;
			synonymsTable.RegisterBehavior(new ToolTipBehavior(),
				new SortInList(),
				new ColumnResizeBehavior(),
				new RowSelectionBehavior());

			synonymsTable.Host.InputMap();
			synonymsTable.Behavior<ColumnResizeBehavior>().ColumnResized += column => WidthHolder.Update(synonymsTable, column, WidthHolder.ProducerWidths);

			equivalentTable = new VirtualTable(new TemplateManager<List<string>, string>(
				() => Row.Headers("Эквивалент"), e => Row.Cells(e)));

			var producersToSynonymsSplit = new SplitContainer {
				Dock = DockStyle.Fill,
				Orientation = Orientation.Horizontal
			};
			var producersToEquivalentsSplit = new SplitContainer {
				Dock = DockStyle.Fill,
			};

			producersToEquivalentsSplit.Panel1.Controls.Add(assortmentTable.Host);
			producersToEquivalentsSplit.Panel2.Controls.Add(equivalentTable.Host);

			producersToSynonymsSplit.Panel1.Controls.Add(producersToEquivalentsSplit);
			producersToSynonymsSplit.Panel2.Controls.Add(synonymsTable.Host);

			Controls.Add(producersToSynonymsSplit);
			Controls.Add(navigationToolStrip);
			Controls.Add(tools);

			producersToSynonymsSplit.SplitterDistance = (int)(0.5 * Height);
			producersToEquivalentsSplit.SplitterDistance = (int)(0.7 * producersToEquivalentsSplit.Width);

			assortmentTable.Host.Tag = PaginatorExtention.TableName;

			navigationToolStrip.ActAsPaginator(
				assortments,
				page => {
					Pager<AssortmentDto> pager = null;
					if (String.IsNullOrEmpty(_searchText))
						Action(s => {
							pager = s.GetAssortmentPage(page);
						});
					else
						Action(s => {
							pager = s.SearchAssortment(_searchText, page);
						});
					UpdateAssortment(pager);
					return pager;
				});

			MoveToBookmark();
			Shown += (s, a) => assortmentTable.Host.Focus();

			var selected = assortmentTable.Selected<AssortmentDto>();
			SelectedAssortmentChanged(selected);
		}

		private void SelectedAssortmentChanged(AssortmentDto assortment)
		{
			Action(s => {
				synonymsTable.TemplateManager.Source = s.GetSynonyms(assortment.ProducerId).ToList();
				equivalentTable.TemplateManager.Source = s.GetEquivalents(assortment.ProducerId).ToList();
			});
		}

		private void Delete()
		{
			var assortment = assortmentTable.Selected<AssortmentDto>();
			if (assortment == null)
				return;

			Action(s => s.DeleteAssortment(assortment.Id));
			((List<AssortmentDto>)assortmentTable.TemplateManager.Source).Remove(assortment);
			assortmentTable.RebuildViewPort();
		}

		private void Search()
		{
			var searchText = tools.Items["SearchText"].Text;
			if (String.IsNullOrEmpty(searchText))
				return;

			_searchText = searchText;

			Action(s => {
				var pager = s.SearchAssortment(searchText, 0);
				if (pager == null)
				{
					MessageBox.Show("По вашему запросу ничего не найдено");
					return;
				}
				UpdateAssortment(pager);
				navigationToolStrip.UpdatePaginator(pager);
			});
		}

		private void SetBookmark()
		{
			var selectedItem = assortmentTable.Selected<AssortmentDto>();
			if (selectedItem == null)
			{
				MessageBox.Show("Выделите позицию в ассортименте");
				return;
			}
			Settings.Default.BookmarkAssortimentId = selectedItem.Id;
			Settings.Default.Save();
			assortmentTable.RebuildViewPort();
		}

		private void MoveToBookmark()
		{
			if (Settings.Default.BookmarkAssortimentId == 0)
				return;

			Action(s => {
				var assortments = s.ShowAssortment(Settings.Default.BookmarkAssortimentId);
				UpdateAssortment(assortments);
				assortmentTable.Behavior<IRowSelectionBehavior>().MoveSelectionAt(assortments.Content.IndexOf(a => a.Id == Settings.Default.BookmarkAssortimentId));
			});
		}

		private void UpdateAssortment(Pager<AssortmentDto> pager)
		{
			assortmentTable.TemplateManager.Source = pager.Content.ToList();
		}
	}
}

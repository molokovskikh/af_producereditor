using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Common.Tools;
using log4net;
using ProducerEditor.Infrastructure;
using ProducerEditor.Models;
using Subway.Dom;
using Subway.Helpers;
using Subway.VirtualTable;
using Subway.VirtualTable.Behaviors;
using Subway.VirtualTable.Behaviors.Selection;
using Subway.VirtualTable.Behaviors.Specialized;

namespace ProducerEditor.Views
{
	public class ShowExcludes : View
	{
		private VirtualTable excludeTable;
		private ToolStrip tools;
		private ToolStrip navigation;
		private uint _currentPage;
		private string _searchText;

		public ShowExcludes(Pager<Exclude> pager)
		{
			Text = "Исключения";
			MinimumSize = new Size(640, 480);

			tools = new ToolStrip()
				.Edit("SearchText")
				.Button("Поиск", Search)
				.Separator()
				.Button("Добавить в ассортимент", AddToAssortiment)
				.Button("Больше не показывать", DoNotShow)
				.Button("Ошибочное сопоставление по наименованию", DeleteSynonym);
			var searchText = ((ToolStripTextBox)tools.Items["SearchText"]);
			searchText.KeyDown += (sender, args) => {
				if (args.KeyCode == Keys.Enter)
					Search();
			};

			navigation = new ToolStrip()
				.Button("Prev", "Передыдущая страница")
				.Label("PageLabel", "")
				.Button("Next", "Следующая страница");

			excludeTable = new VirtualTable(new TemplateManager<List<Exclude>, Exclude>(
				() => {
					var row = Row.Headers(
						new Header("Продукт").Sortable("Catalog"),
						new Header("Оригинальное наименование").Sortable("Catalog"),
						new Header("Синоним").Sortable("ProducerSynonym"),
						new Header("Поставщик").Sortable("Supplier"),
						new Header("Регион").Sortable("Region")
					);
					return row;
				},
				e => Row.Cells(e.Catalog, e.OriginalSynonym, e.ProducerSynonym, e.Supplier, e.Region)
			));

			excludeTable.CellSpacing = 1;
			excludeTable.RegisterBehavior(
				new RowSelectionBehavior(),
				new ToolTipBehavior(),
				new SortInList(),
				new ColumnResizeBehavior());
			excludeTable.Behavior<ColumnResizeBehavior>().ColumnResized += column => WidthHolder.Update(excludeTable, column, WidthHolder.ExcludeWidths);
			excludeTable.TemplateManager.ResetColumns();

			excludeTable.TemplateManager.Source = pager.Content.ToList();

			Controls.Add(excludeTable.Host);
			Controls.Add(navigation);
			Controls.Add(tools);

			_currentPage = 0;
			navigation.ActAsPaginator(pager, page => {
				Pager<Exclude> paginator = null;
				paginator = RequestExcludes(page, false);
				excludeTable.TemplateManager.Source = paginator.Content.ToList();
				_currentPage = page;
				return paginator;
			});

			Shown += (s, a) => excludeTable.Host.Focus();
		}

		// Если флажок isRefresh равен true, тогда данные выбираются из мастера.
		// Это нужно, потому что возникали ситуации, когда из мастера запись удалили, а она снова выбралась из слейва
		// (репликация не успела)
		private Pager<Exclude> RequestExcludes(uint page, bool isRefresh)
		{
			if (String.IsNullOrEmpty(_searchText))
				return Request(s => s.ShowExcludes(page, isRefresh));
			return Request(s => s.SearchExcludes(_searchText, page, isRefresh));
		}

		public void DeleteSynonym()
		{
			var exclude = excludeTable.Selected<Exclude>();
			var selectedIndex = excludeTable.Behavior<IRowSelectionBehavior>().SelectedRowIndex;
			if (exclude == null)
				return;
			if (String.IsNullOrEmpty(exclude.OriginalSynonym) && exclude.OriginalSynonymId == 0)
			{
				MessageBox.Show("Для выбранного исключения не задано оригинальное наименование", "Предупреждение",
					MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}
			Action(s => s.DeleteSynonym(exclude.OriginalSynonymId));
			RefreshTable();
			excludeTable.Behavior<IRowSelectionBehavior>().MoveSelectionAt(selectedIndex);
		}

		private void RefreshTable()
		{
			var paginator = RequestExcludes(_currentPage, true);
			excludeTable.TemplateManager.Source = paginator.Content.ToList();
		}

		private void Search()
		{
			var searchText = tools.Items["SearchText"].Text;
			if (searchText == null || (String.IsNullOrEmpty(searchText) && String.IsNullOrEmpty(_searchText)))
				return;

			_searchText = searchText;

			Action(s => {
				var pager = s.SearchExcludes(searchText, 0, false);
				if (pager == null)
				{
					MessageBox.Show("По вашему запросу ничего не найдено");
					return;
				}
				UpdateExcludes(pager);
				navigation.UpdatePaginator(pager);
			});
		}

		private void UpdateExcludes(Pager<Exclude> pager)
		{
			excludeTable.TemplateManager.Source = pager.Content.ToList();
		}

		public void AddToAssortiment()
		{
			IList<uint> deletedExcludesIds = null;

			var exclude = excludeTable.Selected<Exclude>();
			var selectedIndex = excludeTable.Behavior<IRowSelectionBehavior>().SelectedRowIndex;
			if (exclude == null)
				return;

			var view = new AddToAssortmentView(exclude, ShowProducers.producers);
			if (view.ShowDialog() == DialogResult.OK)
			{
				//Action(s => deletedExcludesIds = s.AddToAssotrment(exclude.Id));
				RefreshTable();
				excludeTable.Behavior<IRowSelectionBehavior>().MoveSelectionAt(selectedIndex);
			}
		}

		public void DoNotShow()
		{
			var exclude = excludeTable.Selected<Exclude>();

			if (exclude == null)
				return;

			Action(s => s.DoNotShow(exclude.Id));
			((List<Exclude>)excludeTable.TemplateManager.Source).Remove(exclude);
			excludeTable.RebuildViewPort();
		}
	}
}

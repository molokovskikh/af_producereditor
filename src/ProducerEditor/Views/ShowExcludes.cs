using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Common.Tools;
using log4net;
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
				.Button("Ошибочное сопоставление (Delete)", DeleteProducerSynonym)
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
				() =>
					{
						var row = Row.Headers();
						var header = new Header("Продукт").Sortable("Catalog");
						row.Append(header);
						header = new Header("Оригинальное наименование").Sortable("Catalog");
						row.Append(header);
						header = new Header("Производитель").Sortable("Producer");
						row.Append(header);
						header = new Header("Синоним").Sortable("ProducerSynonym");
						row.Append(header);
						header = new Header("Поставщик").Sortable("Supplier");
						row.Append(header);
						header = new Header("Регион").Sortable("Region");
						row.Append(header);
						return row;
					},
				e => Row.Cells(e.Catalog, e.OriginalSynonym, e.Producer, e.ProducerSynonym, e.Supplier, e.Region)
				));

			excludeTable.CellSpacing = 1;
			excludeTable.RegisterBehavior(new RowSelectionBehavior(),
				new ToolTipBehavior(),
				new SortInList(),
				new ColumnResizeBehavior());
			excludeTable.Behavior<ColumnResizeBehavior>().ColumnResized += column => WidthHolder.Update(excludeTable, column, WidthHolder.ExcludeWidths);
			excludeTable.TemplateManager.ResetColumns();

			excludeTable.Host.InputMap()
				.KeyDown(Keys.Delete, DeleteProducerSynonym);

			excludeTable.TemplateManager.Source = pager.Content.ToList();

			Controls.Add(excludeTable.Host);
			Controls.Add(navigation);
			Controls.Add(tools);

			_currentPage = 0;
			navigation.ActAsPaginator(pager, page => {
				Pager<Exclude> paginator = null;
				paginator = RequestExcludes(page);
				excludeTable.TemplateManager.Source = paginator.Content.ToList();
				_currentPage = page;
				return paginator;
			});

			Shown += (s, a) => excludeTable.Host.Focus();
		}

		private Pager<Exclude> RequestExcludes(uint page)
		{
			if (String.IsNullOrEmpty(_searchText))
				return Request(s => s.ShowExcludes(page));
			return Request(s => s.SearchExcludes(_searchText, page));
		}

		public void DeleteProducerSynonym()
		{
			var exclude = excludeTable.Selected<Exclude>();
			if (exclude == null)
				return;

			Action(s => s.DeleteProducerSynonym(exclude.ProducerSynonymId));
			var excludes = ((List<Exclude>)excludeTable.TemplateManager.Source);
			excludes.Where(e => e.ProducerSynonymId == exclude.ProducerSynonymId)
				.ToList()
				.Each(e => excludes.Remove(e));
			excludeTable.RebuildViewPort();
		}

		public void DeleteSynonym()
		{
			var exclude = excludeTable.Selected<Exclude>();
			if (exclude == null)
				return;
			if (String.IsNullOrEmpty(exclude.OriginalSynonym) && exclude.OriginalSynonymId == 0)
			{
				MessageBox.Show("Для выбранного исключения не задано оригинальное наименование", "Предупреждение",
					MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;	
			}
			Action(s => s.DeleteSynonym(exclude.OriginalSynonymId));
			var excludes = ((List<Exclude>)excludeTable.TemplateManager.Source);
			excludes.Where(e => e.OriginalSynonymId == exclude.OriginalSynonymId)
				.ToList()
				.Each(e => excludes.Remove(e));
			excludeTable.RebuildViewPort();
		}

		private void Search()
		{
			var searchText = tools.Items["SearchText"].Text;
			if (searchText == null || (String.IsNullOrEmpty(searchText) && String.IsNullOrEmpty(_searchText)))
				return;

			_searchText = searchText;

			Action(s => {
				var pager = s.SearchExcludes(searchText, 0);
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
			var exclude = excludeTable.Selected<Exclude>();
			var selectedIndex = excludeTable.Behavior<IRowSelectionBehavior>().SelectedRowIndex;
			if (exclude == null)
				return;

			Action(s => s.AddToAssotrment(exclude.Id));

			var paginator = RequestExcludes(_currentPage);
			if (paginator.Content.ToList().Contains(exclude))
			{
				var logger = LogManager.GetLogger(typeof(ShowExcludes));
				logger.Error("Предупреждение в Редакторе производителей", new Exception(@"
После добавления исключения в ассортимент и удаления этой и других записей(с таким же CatalogId и ProducerId) из таблицы исключений, эти записи выбраны снова.
Slave не обновлен."));
			}
			UpdateExcludes(paginator);
			excludeTable.Behavior<IRowSelectionBehavior>().MoveSelectionAt(selectedIndex);
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

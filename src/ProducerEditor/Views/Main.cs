﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Linq.Expressions;
using System.ServiceModel;
using System.Windows.Forms;
using ProducerEditor.Infrastructure;
using ProducerEditor.Models;
using Subway.Dom;
using Subway.Dom.Base;
using Subway.Dom.Input;
using Subway.Dom.Styles;
using Subway.Helpers;
using Subway.VirtualTable;
using Subway.VirtualTable.Behaviors;
using Subway.VirtualTable.Behaviors.Selection;
using Subway.VirtualTable.Behaviors.Specialized;
using Common.Tools;

namespace ProducerEditor.Views
{
	public class WidthHolder
	{
		public static List<int> ProducerWidths = Enumerable.Repeat(100, 4).ToList();
		public static List<int> OffersWidths = Enumerable.Repeat(100, 4).ToList();
		public static List<int> ReportWidths = Enumerable.Repeat(100, 6).ToList();
		public static List<int> ProductsAndProducersWidths = Enumerable.Repeat(100, 5).ToList();
		public static List<int> OffersBySynonymView = Enumerable.Repeat(100, 2).ToList();
		public static List<int> SyspiciosSynonyms = Enumerable.Repeat(100, 6).ToList();
		public static List<int> AssortimentWidths = Enumerable.Repeat(100, 2).ToList();

		public static void Update(VirtualTable table, Column column, List<int> widths)
		{
			var element = column;
			do
			{
				widths[table.Columns.IndexOf(element)] = element.ReadonlyStyle.Get(StyleElementType.Width);
				var node = table.Columns.Find(element).Next;
				if (node != null)
					element = node.Value;
				else
					element = null;
			}
			while(element != null);
		}
	}

	public class View : Form
	{
		protected Action Controller<T>(Expression<Func<ProducerService, T>> func)
		{
			return () => WithService(s => {
				var viewName = MvcHelper.GetViewName(func);
				var viewType = MvcHelper.GetViewType(viewName);
				var result = func.Compile()(s);
				MvcHelper.ShowDialog(viewType, result);
			});
		}

		protected Action Controller(Action<ProducerService> action)
		{
			return () => WithService(action);
		}

		protected void Action(Action<ProducerService> action)
		{
			WithService(action);
		}

		private void WithService(Action<ProducerService> action)
		{
			var binding = new BasicHttpBinding
			{
				MaxBufferSize = int.MaxValue,
				MaxReceivedMessageSize = int.MaxValue,
				SendTimeout = TimeSpan.FromMinutes(10),
				ReaderQuotas = {MaxArrayLength = int.MaxValue},
			};
			var endpoint = new EndpointAddress(Settings.Default.EndpointAddress + "ProducerService.svc");
			var factory = new ChannelFactory<ProducerService>(binding, endpoint);
			action(factory.CreateChannel());
		}
	}

	public class Main : View
	{
		private readonly MainController controller = new MainController();
		private readonly VirtualTable producerTable;
		private readonly VirtualTable synonymsTable;
		private readonly ToolStrip toolStrip;

		private uint BookmarkProducerId = Settings.Default.BookmarkProducerId;
		private VirtualTable equivalentTable;

		public Main()
		{
			Text = "Редактор каталога производителей";
			MinimumSize = new Size(800, 600);
			KeyPreview = true;

			this.InputMap()
				.KeyDown(Keys.F9, () => controller.ShowSynonymReport())
				.KeyDown(Keys.F10, Controller(s => s.ShowSuspiciousSynonyms()));

			var navigation = new ToolStrip()
				.Button("Производители", () => { })
				.Button("Ассортимент", Controller(s => s.ShowAssortment(Settings.Default.BookmarkAssortimentId)))
				.Button("Отчет о сопоставлениях (F9)", () => controller.ShowSynonymReport())
				.Button("Подозрительные сопоставления (F10)", Controller(c => c.ShowSuspiciousSynonyms()));

			toolStrip = new ToolStrip()
				.Edit("SearchText")
				.Button("Поиск", SearchProducer)
				.Separator()
				.Button("Переименовать (F2)", ShowRenameView)
				.Button("Объединить (F3)", ShowJoinView)
				.Button("Удалить (Delete)", Delete)
				.Separator()
				.Button("Продукты (Enter)", ShowProducers);

			var bookmarksToolStrip = new ToolStrip()
				.Button("К закаладке", MoveToBookmark)
				.Button("Установить закладку", SetBookmark);

			var searchText = ((ToolStripTextBox) toolStrip.Items["SearchText"]);
			searchText.KeyDown += (sender, args) => {
				if (args.KeyCode == Keys.Enter)
					SearchProducer();
			};

			producerTable = new VirtualTable(new TemplateManager<List<Producer>, Producer>(
				() => Row.Headers(new Header("Проверен").AddClass("CheckBoxColumn1"), "Производитель"), 
				producer => {
					var row = Row.Cells(new CheckBoxInput(producer.Checked), producer.Name);
					if (producer.HasOffers == 0)
						row.AddClass("WithoutOffers");
					if (producer.Id == BookmarkProducerId)
						((IDomElementWithChildren)row.Children.Last()).Prepend(new TextBlock {Class = "BookmarkGlyph"});
					return row;
				}));
			producerTable.CellSpacing = 1;
			producerTable.RegisterBehavior(new RowSelectionBehavior(),
				new ToolTipBehavior(),
				new InputSupport(input => {
					var row = (Row)input.Parent.Parent;
					var producer = producerTable.Translate<Producer>(row);
					producer.Checked = ((CheckBoxInput) input).Checked;
					controller.Update(producer);
				}));
			producerTable.Host.KeyDown += (sender, args) => {
				if (args.KeyCode == Keys.Enter && String.IsNullOrEmpty(searchText.Text))
					ShowProducers();
				else if (args.KeyCode == Keys.Enter)
					SearchProducer();
				else if (args.KeyCode == Keys.Escape && !String.IsNullOrEmpty(searchText.Text))
					searchText.Text = "";
				else if (args.KeyCode == Keys.Escape && String.IsNullOrEmpty(searchText.Text))
					ReseteFilter();
				else if (args.KeyCode == Keys.Delete)
					Delete();
				else if (args.KeyCode == Keys.Tab)
					synonymsTable.Host.Focus();
				else if (args.KeyCode == Keys.F2)
					ShowRenameView();
				else if (args.KeyCode == Keys.F3)
					ShowJoinView();
			};
			producerTable.Host.KeyPress += (sender, args) => {
				if (Char.IsLetterOrDigit(args.KeyChar))
					searchText.Text += args.KeyChar;
			};

			var behavior = producerTable.Behavior<IRowSelectionBehavior>();
			behavior.SelectedRowChanged += (oldRow, newRow) => SelectedProducerChanged(behavior.Selected<Producer>());

			synonymsTable = new VirtualTable(new TemplateManager<List<SynonymDto>, SynonymDto>(
				() =>{
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

					header = new Header("Сегмент").Sortable("Segment");
					header.InlineStyle.Set(StyleElementType.Width, WidthHolder.ProducerWidths[3]);
					row.Append(header);

					return row;
				},
				synonym => {
					var row = Row.Cells(synonym.Name,
										synonym.Supplier,
										synonym.Region,
										synonym.SegmentAsString());
						if (synonym.HaveOffers == 0)
							row.AddClass("WithoutOffers");
						return row;
				}));
			synonymsTable.CellSpacing = 1;
			synonymsTable.RegisterBehavior(new ToolTipBehavior(),
				new SortInList(),
				new ColumnResizeBehavior(),
				new RowSelectionBehavior());

			synonymsTable.Host
				.InputMap()
				.KeyDown(Keys.Enter, ShowProducers)
				.KeyDown(Keys.Delete, Delete)
				.KeyDown(Keys.Escape, () => producerTable.Host.Focus());
			synonymsTable.Behavior<ColumnResizeBehavior>().ColumnResized += column => WidthHolder.Update(synonymsTable, column, WidthHolder.ProducerWidths);

			equivalentTable = new VirtualTable(new TemplateManager<List<string>, string>(
				() => Row.Headers("Эквивалент"),
				e => Row.Cells(e)));

			var producersToSynonymsSplit = new SplitContainer
			{
				Dock = DockStyle.Fill,
				Orientation = Orientation.Horizontal
			};
			var producersToEquivalentsSplit = new SplitContainer
			{
				Dock = DockStyle.Fill,
			};
			InputLanguageHelper.SetToRussian();
			producersToEquivalentsSplit.Panel1.Controls.Add(producerTable.Host);
			producersToEquivalentsSplit.Panel2.Controls.Add(equivalentTable.Host);

			producersToSynonymsSplit.Panel1.Controls.Add(producersToEquivalentsSplit);
			producersToSynonymsSplit.Panel2.Controls.Add(synonymsTable.Host);
			Controls.Add(producersToSynonymsSplit);
			Controls.Add(bookmarksToolStrip);
			Controls.Add(toolStrip);
			Controls.Add(navigation);
			producersToSynonymsSplit.SplitterDistance = (int) (0.6 * Size.Height);
			producersToEquivalentsSplit.SplitterDistance = (int) (0.7 * producersToEquivalentsSplit.Width);
			Shown += (sender, args) => producerTable.Host.Focus();
			synonymsTable.TemplateManager.ResetColumns();
			UpdateProducers();
		}

		private void SetBookmark()
		{
			BookmarkProducerId = producerTable.Behavior<IRowSelectionBehavior>().Selected<Producer>().Id;
			Settings.Default.BookmarkProducerId = BookmarkProducerId;
			Settings.Default.Save();
			producerTable.RebuildViewPort();
		}

		private void MoveToBookmark()
		{
			ReseteFilter();
			producerTable.Behavior<IRowSelectionBehavior>().MoveSelectionAt(controller.Producers.IndexOf(p => p.Id == BookmarkProducerId));
		}

		private void Delete()
		{
			if (producerTable.Host.Focused)
			{
				var producer = producerTable.Selected<Producer>();
				if (producer == null)
					return;
				if (MessageBox.Show(String.Format("Удалить производителя \"{0}\"", producer.Name), "Удаление производителя", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.Cancel)
					return;

				Action(s => {
					s.DeleteProducer(producer.Id);
					((IList<Producer>)producerTable.TemplateManager.Source).Remove(producer);
					controller.Producers.Remove(producer);
				});

				SelectedProducerChanged(producerTable.Selected<Producer>());
				producerTable.RebuildViewPort();
			}
			else if (synonymsTable.Host.Focused)
			{
				var synonym = synonymsTable.Selected<SynonymDto>();
				if (synonym == null)
					return;

				Action(s => s.DeleteProducerSynonym(synonym.Id));

				((IList<SynonymDto>)synonymsTable.TemplateManager.Source).Remove(synonym);
				synonymsTable.RebuildViewPort();
			}
		}

		private void ShowProducers()
		{
			if (producerTable.Host.Focused)
			{
				var producer = producerTable.Selected<Producer>();
				controller.ShowProductsAndProducers(producer);
				producerTable.RebuildViewPort();
			}
			else
			{
				var synonym = synonymsTable.Selected<ProducerSynonym>();
				if (synonym == null)
					return;

				Controller(s => s.ShowOffersBySynonym(synonym.Id))();
			}
		}

		private void ReseteFilter()
		{
			var producers = controller.SearchProducer(null);
			producerTable.TemplateManager.Source = producers;
			producerTable.Host.Focus();
		}

		private void SearchProducer()
		{
			var text = toolStrip.Items["SearchText"];
			var producers = controller.SearchProducer(text.Text);
			text.Text = "";
			if (producers.Count > 0)
			{
				producerTable.TemplateManager.Source = producers;
				producerTable.Host.Focus();
			}
			else
			{
				MessageBox.Show("По вашему запросу ничеого не найдено", "Результаты поиска",
								MessageBoxButtons.OK,
								MessageBoxIcon.Warning);
			}
		}

		private void ShowRenameView()
		{
			var producer = producerTable.Selected<Producer>();
			if (producer == null)
				return;
			var rename = new RenameView(controller, producer);
			if (rename.ShowDialog() != DialogResult.Cancel)
			{
				producerTable.RebuildViewPort();
			}
		}

		private void ShowJoinView()
		{
			var producer = producerTable.Selected<Producer>();
			controller.Join(producer,
				() => {
					producerTable.RebuildViewPort();
					SelectedProducerChanged(producerTable.Selected<Producer>());
			});
		}

		private void SelectedProducerChanged(Producer producer)
		{
			synonymsTable.TemplateManager.Source = controller.Synonyms(producer);
			Action(s => {
				equivalentTable.TemplateManager.Source = s.GetEquivalents(producer.Id).ToList();
			});
		}

		public void UpdateProducers()
		{
			var producers = controller.GetAllProducers();
			producerTable.TemplateManager.Source = producers;
		}
	}
}
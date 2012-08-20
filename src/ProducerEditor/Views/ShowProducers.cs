using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Common.Tools;
using ProducerEditor.Contract;
using ProducerEditor.Infrastructure;
using ProducerEditor.Models;
using ProducerEditor.Presenters;
using Subway.Dom;
using Subway.Dom.Base;
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
	public class ShowProducers : View
	{
		public static List<ProducerDto> producers;

		private VirtualTable producerTable;
		private VirtualTable synonymsTable;
		private ToolStrip toolStrip;

		private uint BookmarkProducerId = Settings.Default.BookmarkProducerId;
		private VirtualTable equivalentTable;
		private ToolStripTextBox searchText;

		public ShowProducers()
		{
			Text = "Производители";
			searchText = ((ToolStripTextBox) toolStrip.Items["SearchText"]);
		}

		protected override void Init()
		{
			toolStrip = new ToolStrip()
				.Button("Переименовать (F2)", ShowRenameView)
				.Button("Объединить (F3)", ShowJoinView)
				.Button("Удалить (Delete)", Delete)
				.Separator()
				.Button("Продукты (Enter)", ShowProductsAndProducersOrOffers)
				.Button("Показать в ассортименте", ShowAssortmentForProducer)
				.Separator()
				.Button("Создать эквивалент", ShowCreateEquivalentForProducer);
			toolStrip.Tag = "Searchable";

			var bookmarksToolStrip = new ToolStrip()
				.Button("К закаладке", MoveToBookmark)
				.Button("Установить закладку", SetBookmark);

			producerTable = new VirtualTable(new TemplateManager<List<ProducerDto>, ProducerDto>(
				() => Row.Headers(new Header("Проверен").AddClass("CheckBoxColumn1"), "Производитель"), 
				producer => {
					var row = Row.Cells(new CheckBoxInput(producer.Checked).Attr("Name", "Checked"), producer.Name);
					if (producer.HasOffers)
						row.AddClass("WithoutOffers");
					if (producer.Id == BookmarkProducerId)
						((IDomElementWithChildren)row.Children.Last()).Prepend(new TextBlock {Class = "BookmarkGlyph"});
					return row;
				}));
			producerTable.RegisterBehavior(
				new InputController()
			);
			producerTable.Host.Name = "Producers";
			producerTable.Host.KeyDown += (sender, args) => {
				if (args.KeyCode == Keys.Enter && String.IsNullOrEmpty(searchText.Text))
					ShowProductsAndProducersOrOffers();
				else if (args.KeyCode == Keys.Enter)
					((ShowProducersPresenter)Presenter).Search(searchText.Text);
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

			synonymsTable = new VirtualTable(new TemplateManager<List<ProducerSynonymDto>, ProducerSynonymDto>(
				() =>{
					var row = Row.Headers();
					var header = new Header("Синоним").Sortable("Name");
					row.Append(header);

					header = new Header("Поставщик").Sortable("Supplier");
					row.Append(header);

					header = new Header("Регион").Sortable("Region");
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

			synonymsTable.Host.Name = "ProducerSynonyms";
			synonymsTable.Host
				.InputMap()
				.KeyDown(Keys.Enter, ShowProductsAndProducersOrOffers)
				.KeyDown(Keys.Delete, Delete)
				.KeyDown(Keys.Escape, () => producerTable.Host.Focus());

			equivalentTable = new VirtualTable(new TemplateManager<List<string>, string>(
				() => Row.Headers("Эквивалент"),
				e => Row.Cells(e)));
			equivalentTable.Host.Name = "Equivalents";

			var producersToSynonymsSplit = new SplitContainer {
				Dock = DockStyle.Fill,
				Orientation = Orientation.Horizontal
			};
			var producersToEquivalentsSplit = new SplitContainer {
				Dock = DockStyle.Fill,
			};
			producersToEquivalentsSplit.Panel1.Controls.Add(producerTable.Host);
			producersToEquivalentsSplit.Panel2.Controls.Add(equivalentTable.Host);

			producersToSynonymsSplit.Panel1.Controls.Add(producersToEquivalentsSplit);
			producersToSynonymsSplit.Panel2.Controls.Add(synonymsTable.Host);
			Controls.Add(producersToSynonymsSplit);
			Controls.Add(new Legend("WithoutOffers"));
			Controls.Add(bookmarksToolStrip);
			Controls.Add(toolStrip);
			producersToSynonymsSplit.SplitterDistance = (int) (0.5 * Height);
			producersToEquivalentsSplit.SplitterDistance = (int) (0.7 * producersToEquivalentsSplit.Width);
			Shown += (sender, args) => producerTable.Host.Focus();
			synonymsTable.TemplateManager.ResetColumns();
			UpdateProducers();
		}

		private void SetBookmark()
		{
			var producer = producerTable.Selected<ProducerDto>();
			if (producer == null)
				return;

			BookmarkProducerId = producer.Id;
			Settings.Default.BookmarkProducerId = BookmarkProducerId;
			Settings.Default.Save();
			producerTable.RebuildViewPort();
		}

		private void MoveToBookmark()
		{
			ReseteFilter();
			producerTable.Behavior<IRowSelectionBehavior>().MoveSelectionAt(producers.IndexOf(p => p.Id == BookmarkProducerId));
		}

		private void ShowAssortmentForProducer()
		{
			var producer = producerTable.Selected<ProducerDto>();
			if (producer == null)
				return;


			MvcHelper.ShowDialog(
				typeof (ShowAssortmentForProducer), 
				producer.Id,
				Request(s => s.ShowAssortmentForProducer(producer.Id, 0)));
		}

		private void Delete()
		{
			if (producerTable.Host.Focused)
			{
				var producer = producerTable.Selected<ProducerDto>();
				if (producer == null)
					return;
				if (MessageBox.Show(String.Format("Удалить производителя \"{0}\"", producer.Name), "Удаление производителя", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.Cancel)
					return;

				Action(s => {
					s.DeleteProducer(producer.Id);
					((IList<ProducerDto>)producerTable.TemplateManager.Source).Remove(producer);
					producers.Remove(producer);
				});
				SelectedProducerChanged(producerTable.Selected<ProducerDto>());
				producerTable.RebuildViewPort();
			}
			else if (synonymsTable.Host.Focused)
			{
				var synonym = synonymsTable.Selected<ProducerSynonymDto>();
				if (synonym == null)
					return;

				Action(s => s.DeleteProducerSynonym(synonym.Id));

				((IList<ProducerSynonymDto>)synonymsTable.TemplateManager.Source).Remove(synonym);
				synonymsTable.RebuildViewPort();
			}
		}

		private void ShowProductsAndProducersOrOffers()
		{
			if (producerTable.Host.Focused)
			{
				var producer = producerTable.Selected<ProducerDto>();
				var productAndProducers = Request(s => s.ShowProductsAndProducers(producer.Id));
				new ShowProductsAndProducers(producer, producers, productAndProducers).ShowDialog();
				producerTable.RebuildViewPort();
			}
			else
			{
				var synonym = synonymsTable.Selected<ProducerSynonymDto>();
				if (synonym == null)
					return;

				Controller(s => s.ShowOffers(new OffersQuery("ProducerSynonymId", synonym.Id)))();
			}
		}

		private void ReseteFilter()
		{
			producerTable.TemplateManager.Source = producers;
			producerTable.Host.Focus();
		}

		private void ShowRenameView()
		{
			var producer = producerTable.Selected<ProducerDto>();
			if (producer == null)
				return;
			var rename = new RenameView(producer, producers, p => Action(s => s.UpdateProducer(p)));
			if (rename.ShowDialog() != DialogResult.Cancel)
			{
				producerTable.RebuildViewPort();
			}
		}

		private void ShowJoinView()
		{
			var producer = producerTable.Selected<ProducerDto>();
			if (producer == null)
				return;
			var view = new JoinView(producer, producers);
			if (view.ShowDialog() != DialogResult.Cancel)
			{
				producerTable.RebuildViewPort();
				SelectedProducerChanged(producerTable.Selected<ProducerDto>());
			}
		}

		private void SelectedProducerChanged(ProducerDto producer)
		{
			((ShowProducersPresenter)Presenter).CurrentChanged(producer);
		}

		public void UpdateProducers()
		{
			Action(s => {
				producers = s.GetProducers("").ToList();
			});
			producerTable.TemplateManager.Source = producers;
		}

		private void ShowCreateEquivalentForProducer()
		{
			var producer = producerTable.Selected<ProducerDto>();
			if (producer == null)
				return;
			IList<String> equivalents = null;
			Action(s => { equivalents = s.GetEquivalents(producer.Id).ToList(); });
			var createEquivalent = new CreateEquivalentView(producer, equivalents, 
				(text, producerId) => Action(s => s.CreateEquivalentForProducer(producerId, text)));
			if (createEquivalent.ShowDialog() != DialogResult.Cancel)
			{
				SelectedProducerChanged(producerTable.Selected<ProducerDto>());
			}
		}
	}
}
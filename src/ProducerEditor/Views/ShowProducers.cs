using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Common.Tools;
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

namespace ProducerEditor.Views
{
	public class ShowProducers : View
	{
		private readonly MainController controller = new MainController();
		private readonly VirtualTable producerTable;
		private readonly VirtualTable synonymsTable;
		private readonly ToolStrip toolStrip;

		private uint BookmarkProducerId = Settings.Default.BookmarkProducerId;
		private VirtualTable equivalentTable;

		public ShowProducers()
		{
			toolStrip = new ToolStrip()
				.Edit("SearchText")
				.Button("�����", SearchProducer)
				.Separator()
				.Button("������������� (F2)", ShowRenameView)
				.Button("���������� (F3)", ShowJoinView)
				.Button("������� (Delete)", Delete)
				.Separator()
				.Button("�������� (Enter)", ShowProductsAndProducersOrOffers)
				.Button("�������� � ������������", ShowAssortmentForProducer)
				.Separator()
				.Button("������� ����������", ShowCreateEquivalentForProducer);

			var bookmarksToolStrip = new ToolStrip()
				.Button("� ���������", MoveToBookmark)
				.Button("���������� ��������", SetBookmark);

			var searchText = ((ToolStripTextBox) toolStrip.Items["SearchText"]);
			searchText.KeyDown += (sender, args) => {
				if (args.KeyCode == Keys.Enter)
					SearchProducer();
			};

			producerTable = new VirtualTable(new TemplateManager<List<Producer>, Producer>(
				() => Row.Headers(new Header("��������").AddClass("CheckBoxColumn1"), "�������������"), 
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
					ShowProductsAndProducersOrOffers();
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

			synonymsTable = new VirtualTable(new TemplateManager<List<ProducerSynonym>, ProducerSynonym>(
				() =>{
					var row = Row.Headers();
					var header = new Header("�������").Sortable("Name");
					header.InlineStyle.Set(StyleElementType.Width, WidthHolder.ProducerWidths[0]);
					row.Append(header);

					header = new Header("���������").Sortable("Supplier");
					header.InlineStyle.Set(StyleElementType.Width, WidthHolder.ProducerWidths[1]);
					row.Append(header);

					header = new Header("������").Sortable("Region");
					header.InlineStyle.Set(StyleElementType.Width, WidthHolder.ProducerWidths[2]);
					row.Append(header);

					return row;
				},
				synonym => {
					var row = Row.Cells(synonym.Name,
						synonym.Supplier,
						synonym.Region);
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
				.KeyDown(Keys.Enter, ShowProductsAndProducersOrOffers)
				.KeyDown(Keys.Delete, Delete)
				.KeyDown(Keys.Escape, () => producerTable.Host.Focus());
			synonymsTable.Behavior<ColumnResizeBehavior>().ColumnResized += column => WidthHolder.Update(synonymsTable, column, WidthHolder.ProducerWidths);

			equivalentTable = new VirtualTable(new TemplateManager<List<string>, string>(
				() => Row.Headers("����������"),
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
			producersToEquivalentsSplit.Panel1.Controls.Add(producerTable.Host);
			producersToEquivalentsSplit.Panel2.Controls.Add(equivalentTable.Host);

			producersToSynonymsSplit.Panel1.Controls.Add(producersToEquivalentsSplit);
			producersToSynonymsSplit.Panel2.Controls.Add(synonymsTable.Host);
			Controls.Add(producersToSynonymsSplit);
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
			var producer = producerTable.Selected<Producer>();
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
			producerTable.Behavior<IRowSelectionBehavior>().MoveSelectionAt(controller.Producers.IndexOf(p => p.Id == BookmarkProducerId));
		}

		private void ShowAssortmentForProducer()
		{
			var producer = producerTable.Selected<Producer>();
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
				var producer = producerTable.Selected<Producer>();
				if (producer == null)
					return;
				if (MessageBox.Show(String.Format("������� ������������� \"{0}\"", producer.Name), "�������� �������������", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.Cancel)
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
				var synonym = synonymsTable.Selected<ProducerSynonym>();
				if (synonym == null)
					return;

				Action(s => s.DeleteProducerSynonym(synonym.Id));

				((IList<ProducerSynonym>)synonymsTable.TemplateManager.Source).Remove(synonym);
				synonymsTable.RebuildViewPort();
			}
		}

		private void ShowProductsAndProducersOrOffers()
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
				MessageBox.Show("�� ������ ������� ������� �� �������", "���������� ������",
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
			if (producer == null)
				return;
			Action(s => {
				synonymsTable.TemplateManager.Source = s.GetSynonyms(producer.Id).ToList();
				equivalentTable.TemplateManager.Source = s.GetEquivalents(producer.Id).ToList();
			});
		}

		public void UpdateProducers()
		{
			var producers = controller.GetAllProducers();
			producerTable.TemplateManager.Source = producers;
		}

		private void ShowCreateEquivalentForProducer()
		{
			var producer = producerTable.Selected<Producer>();
			if (producer == null)
				return;
			IList<String> equivalents = null;
			Action(s => { equivalents = s.GetEquivalents(producer.Id).ToList(); });
			var createEquivalent = new CreateEquivalentView(producer, equivalents, 
				(text, producerId) => Action(s => s.CreateEquivalentForProducer(producerId, text)));
			if (createEquivalent.ShowDialog() != DialogResult.Cancel)
			{
				SelectedProducerChanged(producerTable.Selected<Producer>());
			}
		}
	}
}
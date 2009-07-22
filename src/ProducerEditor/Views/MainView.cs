using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using ProducerEditor.Models;
using Subway.Dom;
using Subway.Dom.Styles;
using Subway.Helpers;
using Subway.VirtualTable;
using Subway.VirtualTable.Behaviors;
using Subway.VirtualTable.Behaviors.Selection;
using Subway.VirtualTable.Behaviors.Specialized;

namespace ProducerEditor.Views
{
	public class MainView : Form
	{
		private readonly Controller _controller = new Controller();
		private readonly VirtualTable producerTable;
		private readonly VirtualTable synonymsTable;
		private ToolStrip toolStrip;

		private static List<int> _widths = new List<int>{
			100, 100, 100, 100
		};

		public MainView()
		{
			Text = "Редактор каталога производителей";
			MinimumSize = new Size(640, 480);

			toolStrip = new ToolStrip()
				.Edit("SearchText")
				.Button("Поиск", SearchProducer)
				.Separator()
				.Button("Переименовать (F2)", ShowRenameView)
				.Button("Объединить (F3)", ShowJoinView)
				.Button("Удалить (Delete)", Delete)
				.Separator()
				.Button("Продукты (Enter)", ShowProducers);
			var searchText = ((ToolStripTextBox) toolStrip.Items["SearchText"]);
			searchText.KeyDown += (sender, args) => {
			                      	if (args.KeyCode == Keys.Enter)
			                      		SearchProducer();
			                      };

			var split = new SplitContainer
			            	{
			            		Dock = DockStyle.Fill,
			            		Orientation = Orientation.Horizontal
			            	};
			producerTable = new VirtualTable(new TemplateManager<List<Producer>, Producer>(
			                                 	() => Row.Headers("Производитель"), 
			                                 	producer => {
			                                 		var row = Row.Cells(producer.Name);
													if (producer.HasOffers == 0)
														row.AddClass("WithoutOffers");
			                                 		return row;
			                                 	}));
			producerTable.CellSpacing = 1;
			producerTable.RegisterBehavior(new RowSelectionBehavior(),
			                               new ToolTipBehavior());
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
			producerTable.Host.KeyPress += (sender, args) =>
											{
												if (Char.IsLetterOrDigit(args.KeyChar))
													searchText.Text += args.KeyChar;
											};

			var behavior = producerTable.Behavior<IRowSelectionBehavior>();
			behavior.SelectedRowChanged += (oldRow, newRow) => SelectedProducerChanged(behavior.Selected<Producer>());

			synonymsTable = new VirtualTable(new TemplateManager<List<SynonymView>, SynonymView>(
			                                 	() =>{
			                                 		var row = Row.Headers();
													var header = new Header("Синоним").Sortable("Name");
													header.InlineStyle.Set(StyleElementType.Width, _widths[0]);
													row.Append(header);

													header = new Header("Поставщик").Sortable("Supplier");
													header.InlineStyle.Set(StyleElementType.Width, _widths[1]);
													row.Append(header);

													header = new Header("Регион").Sortable("Region");
													header.InlineStyle.Set(StyleElementType.Width, _widths[2]);
													row.Append(header);

													header = new Header("Сегмент").Sortable("Segment");
													header.InlineStyle.Set(StyleElementType.Width, _widths[3]);
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
			synonymsTable.Host.KeyDown += (sender, args) => {
											if (args.KeyCode == Keys.Delete)
												Delete();
											if (args.KeyCode == Keys.Escape)
												producerTable.Host.Focus();
			                              };
			synonymsTable.Behavior<ColumnResizeBehavior>().ColumnResized += column => {
				var element = column;
				do
				{
					_widths[synonymsTable.Columns.IndexOf(element)] = element.ReadonlyStyle.Get(StyleElementType.Width);
					var node = synonymsTable.Columns.Find(element).Next;
					if (node != null)
						element = (Column) node.Value;
					else
						element = null;
				}
				while(element != null);
			};

			InputLanguageHelper.SetToRussian();
			split.Panel1.Controls.Add(producerTable.Host);
			split.Panel2.Controls.Add(synonymsTable.Host);
			Controls.Add(split);
			Controls.Add(toolStrip);
			split.SplitterDistance = (int) (Size.Height*0.6);
			Shown += (sender, args) => producerTable.Host.Focus();
			synonymsTable.TemplateManager.ResetColumns();
			UpdateProducers();
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

				_controller.Delete(producer);
				((IList<Producer>)producerTable.TemplateManager.Source).Remove(producer);
				_controller.Producers.Remove(producer);
				SelectedProducerChanged(producer);
				producerTable.RebuildViewPort();
			}
			else if (synonymsTable.Host.Focused)
			{
				var synonym = synonymsTable.Selected<SynonymView>();
				if (synonym == null)
					return;
				_controller.Delete(synonym);
				((IList<SynonymView>)synonymsTable.TemplateManager.Source).Remove(synonym);
				synonymsTable.RebuildViewPort();
			}
		}

		private void ShowProducers()
		{
			var producer = producerTable.Selected<Producer>();
			if (producer == null)
				return;
			new ProductsAndProducersView(_controller, producer, _controller.FindRelativeProductsAndProducers(producer)).ShowDialog();
			producerTable.RebuildViewPort();
		}

		private void ReseteFilter()
		{
			var producers = _controller.SearchProducer(null);
			producerTable.TemplateManager.Source = producers;
			producerTable.Host.Focus();
		}

		private void SearchProducer()
		{
			var text = toolStrip.Items["SearchText"];
			var producers = _controller.SearchProducer(text.Text);
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
			var rename = new RenameView(_controller, producer);
			if (rename.ShowDialog() != DialogResult.Cancel)
			{
				producerTable.RebuildViewPort();
			}
		}

		private void ShowJoinView()
		{
			var producer = producerTable.Selected<Producer>();
			_controller.Join(producer,
			                 () => {
			                 	producerTable.RebuildViewPort();
			                 	SelectedProducerChanged(producerTable.Selected<Producer>());
			                 });
		}

		private void SelectedProducerChanged(Producer producer)
		{
			synonymsTable.TemplateManager.Source = _controller.Synonyms(producer);
		}

		public void UpdateProducers()
		{
			var producers = _controller.GetAllProducers();
			producerTable.TemplateManager.Source = producers;
		}
	}
}
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using ProducerEditor.Models;
using Subway.Dom;
using Subway.VirtualTable;
using Subway.VirtualTable.Behaviors;
using Subway.VirtualTable.Behaviors.Selection;
using Subway.VirtualTable.Behaviors.Specialized;

namespace ProducerEditor.Views
{
	public class InputLanguageHelper
	{
		public static void SetToRussian()
		{
			TryToSetKeyboardLayout(CultureInfo.GetCultureInfo("ru-RU"));
		}

		public static void SetToEnglish()
		{
			TryToSetKeyboardLayout(CultureInfo.GetCultureInfo("en-US"));
		}

		private static void TryToSetKeyboardLayout(CultureInfo culture)
		{
			if (Application.CurrentInputLanguage.Culture.Equals(culture))
				return;

			InputLanguage russianInputLanguage = null;
			foreach (InputLanguage inputLanguage in InputLanguage.InstalledInputLanguages)
			{
				if (inputLanguage.Culture.Equals(culture))
				{
					russianInputLanguage = inputLanguage;
					break;
				}
			}

			if (russianInputLanguage != null)
				Application.CurrentInputLanguage = russianInputLanguage;
		}
	}

	public static class Extentions
	{
		public static ToolStrip Edit(this ToolStrip toolStrip, string name)
		{
			var edit = new ToolStripTextBox
			           	{
			           		Name = name
			           	};
			toolStrip.Items.Add(edit);
			return toolStrip;
		}

		public static ToolStrip Button(this ToolStrip toolStrip, string label, Action onclick)
		{
			var button = new ToolStripButton
			             	{
			             		Text = label
			             	};
			button.Click += (sender, args) => onclick();
			toolStrip.Items.Add(button);
			return toolStrip;
		}

		public static ToolStrip Separator(this ToolStrip toolStrip)
		{
			toolStrip.Items.Add(new ToolStripSeparator());
			return toolStrip;
		}
	}

	public class MainView : Form
	{
		private readonly Controller _controller = new Controller();
		private readonly VirtualTable producerTable;
		private readonly VirtualTable synonymsTable;

		public MainView()
		{
			Text = "Редактор каталога производителей";
			MinimumSize = new Size(640, 480);
			var toolStrip = new ToolStrip();
			
			toolStrip
				.Edit("SearchText")
				.Button("Поиск", () => SearchProducer(toolStrip))
				.Separator()
				.Button("Переименовать", ShowRenameView)
				.Button("Объединить", ShowJoinView)
				.Button("Удалить", Delete)
				.Separator()
				.Button("Продукты", ShowProducers);
			((ToolStripTextBox) toolStrip.Items["SearchText"]).KeyDown += (sender, args) => {
																			if (args.KeyCode == Keys.Enter)
																				SearchProducer(toolStrip);
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
			producerTable.RegisterBehavior(new RowSelectionBehavior(),
			                               new ToolTipBehavior());
			producerTable.Host.KeyDown += (sender, args) => {
											if (args.KeyCode == Keys.Enter)
												ShowProducers();
											else if (args.KeyCode == Keys.Delete)
												Delete();
			                              };
			var behavior = producerTable.Behavior<IRowSelectionBehavior>();
			behavior.SelectedRowChanged += (oldRow, newRow) => SelectedProducerChanged(behavior.Selected<Producer>());

			synonymsTable = new VirtualTable(new TemplateManager<List<SynonymView>, SynonymView>(
			                                 	() => Row.Headers(new Header("Синоним").Sortable("Name"),
																  new Header("Поставщик").Sortable("Supplier"),
																  new Header("Регион").Sortable("Region"),
			                                 	                  new Header("Сегмент").Sortable("Segment")),
			                                 	synonym => {
			                                 		var row = Row.Cells(synonym.Name,
			                                 		                    synonym.Supplier,
			                                 		                    synonym.Region,
			                                 		                    synonym.SegmentAsString());
			                                 			if (synonym.HaveOffers == 0)
															row.AddClass("WithoutOffers");
			                                 			return row;
			                                 		}));
			synonymsTable.RegisterBehavior(new ToolTipBehavior(),
										   new SortInList(),
										   new RowSelectionBehavior());
			synonymsTable.Host.KeyDown += (sender, args) => {
											if (args.KeyCode == Keys.Delete)
												Delete();
			                              };

			InputLanguageHelper.SetToRussian();
			split.Panel1.Controls.Add(producerTable.Host);
			split.Panel2.Controls.Add(synonymsTable.Host);
			Controls.Add(split);
			Controls.Add(toolStrip);
			split.SplitterDistance = (int) (Size.Height*0.6);
			Shown += (sender, args) => producerTable.Host.Focus();
			UpdateProducers();
		}

		private void Delete()
		{
			if (producerTable.Host.Focused)
			{
				var producer = producerTable.Selected<Producer>();
				if (producer == null)
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
			new ProductsAndProducersView(producer, _controller.FindRelativeProductsAndProducers(producer)).ShowDialog();
		}

		private void SearchProducer(ToolStrip toolStrip)
		{
			var producers = _controller.SearchProducer(toolStrip.Items["SearchText"].Text);
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
			var rename = new RenameForm(_controller, producer);
			if (rename.ShowDialog() != DialogResult.Cancel)
			{
				producerTable.RebuildViewPort();
			}
		}

		private void ShowJoinView()
		{
			var producer = producerTable.Selected<Producer>();
			if (producer == null)
				return;
			var rename = new JoinForm(_controller, producer);
			if (rename.ShowDialog() != DialogResult.Cancel)
			{
				producerTable.RebuildViewPort();
				SelectedProducerChanged(producerTable.Selected<Producer>());
			}
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

	public class JoinForm : Dialog
	{
		public JoinForm(Controller controller, Producer producer)
		{
			Text = "Объединение производителей";
			Height = 400;
			((Button) AcceptButton).Text = "Объединить";
			var producersTable = new VirtualTable(new TemplateManager<List<Producer>, Producer>(
			                                      	() => Row.Headers("Производитель"),
			                                      	p => Row.Cells(p.Name)
			                                      	));
			var toolStrip = new ToolStrip();
			var text = new ToolStripTextBox();
			text.KeyDown += (sender, args) =>
			                	{
/*									if (args.KeyCode == Keys.Enter)
									{
										DoSearch(controller, text, producersTable);
										args.Handled = true;
										args.SuppressKeyPress = true;
									}*/
			                	};
			toolStrip.Items.Add(text);
			var button = new ToolStripButton
			             	{
			             		Text = "Поиск"
			             	};
			button.Click += (sender, args) => DoSearch(producer, controller, text, producersTable);
			toolStrip.Items.Add(button);
			producersTable.CellSpacing = 1;
			producersTable.RegisterBehavior(new RowSelectionBehavior(),
			                                new ToolTipBehavior());
			table.RowCount = 2;
			table.RowStyles.Add(new RowStyle());
			table.Controls.Add(toolStrip, 0, 0);
			table.Controls.Add(producersTable.Host, 0, 1);
			Closing += (sender, args) =>
			           	{
			           		if (DialogResult == DialogResult.Cancel)
			           			return;

			           		var p = producersTable.Selected<Producer>();
			           		if (p == null)
			           		{
			           			MessageBox.Show("Не выбран производитель для объединения",
			           			                "Не выбран производитель",
			           			                MessageBoxButtons.OK,
			           			                MessageBoxIcon.Warning);
			           			args.Cancel = true;
			           			return;
			           		}

			           		controller.Join(producer, p);
			           	};
			Shown += (sender, args) => text.Focus();
		}

		private void DoSearch(Producer source, Controller controller, ToolStripTextBox text, VirtualTable producersTable)
		{
			var producers = controller.SearchProducer(text.Text).Where(p => p.Id != source.Id).ToList();
			if (producers.Count > 0)
			{
				producersTable.TemplateManager.Source = producers;
				producersTable.Host.Focus();
			}
			else
			{
				MessageBox.Show("По вашему запросу ничеого не найдено", "Результаты поиска",
				                MessageBoxButtons.OK,
				                MessageBoxIcon.Warning);
			}
		}
	}

	public class RenameForm : Dialog
	{
		public RenameForm(Controller controller, Producer producer)
		{
			var errorProvider = new ErrorProvider();
			var newName = new TextBox
			              	{
			              		Text = producer.Name,
			              		Width = 200,
			              	};
			table.Controls.Add(newName, 0, 0);
			Text = "Переименование производителя";
			Closing += (sender, args) =>
			           	{
			           		if (DialogResult == DialogResult.Cancel)
			           			return;
			           		if (String.IsNullOrEmpty(newName.Text.Trim()))
			           		{
			           			errorProvider.SetError(newName, "Название производителя не может быть пустым");
			           			errorProvider.SetIconAlignment(newName, ErrorIconAlignment.MiddleRight);
			           			args.Cancel = true;
			           			return;
			           		}
			           		producer.Name = newName.Text;
			           		controller.Update(producer);
			           	};
		}
	}

	public class Dialog : Form
	{
		protected TableLayoutPanel table;

		public Dialog()
		{
			AcceptButton = new Button
			               	{
			               		DialogResult = DialogResult.OK,
			               		Text = "Сохранить", 
			               		AutoSize = true,
			               	};
			CancelButton = new Button
			               	{
			               		DialogResult = DialogResult.Cancel,
			               		Text = "Отмена",
			               		AutoSize = true,
			               	};
			FormBorderStyle = FormBorderStyle.FixedSingle;
			MaximizeBox = false;
			MinimizeBox = false;
			ShowInTaskbar = false;
			StartPosition = FormStartPosition.CenterParent;
			var flow = new FlowLayoutPanel
			           	{
			           		AutoSize = true,
			           		Dock = DockStyle.Bottom,
			           		FlowDirection = FlowDirection.RightToLeft
			           	};
			flow.Controls.Add((Control) AcceptButton);
			flow.Controls.Add((Control) CancelButton);
			table = new TableLayoutPanel
			        	{
			        		//AutoSize = true,
			        		RowCount = 1,
			        		ColumnCount = 1,
			        		Dock = DockStyle.Fill
			        	};
			table.RowStyles.Add(new RowStyle());
			table.ColumnStyles.Add(new ColumnStyle());

			Controls.Add(table);
			Controls.Add(flow);
			AutoSize = true;
			Height = 80;
			//AutoSizeMode = AutoSizeMode.GrowAndShrink;
		}
	}
}
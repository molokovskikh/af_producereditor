using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using Subway.Dom;
using Subway.VirtualTable;
using Subway.VirtualTable.Behaviors;
using Subway.VirtualTable.Behaviors.Selection;

namespace ProducerEditor
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

	public class MainForm : Form
	{
		private Controller _controller = new Controller();
		private VirtualTable producerTable;
		private VirtualTable synonymsTable;

		public MainForm()
		{
			Text = "Редактор каталога производителей";
			MinimumSize = new Size(640, 480);
			var toolBar = new ToolStrip();

			var renameButton = new ToolStripButton
			            	{
			            		Text = "Переименовать"
			            	};
			renameButton.Click += (sender, args) =>
			                      	{
			                      		var producer = producerTable.Selected<Producer>();
										if (producer == null)
											return;
			                      		var rename = new RenameForm(_controller, producer);
										if (rename.ShowDialog() != DialogResult.Cancel)
										{
											producerTable.RebuildViewPort();
										}
			                      	};
			toolBar.Items.Add(renameButton);
			var joinButton = new ToolStripButton
			             	{
			             		Text = "Объединить"
			             	};
			joinButton.Click += (sender, args) =>
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
			                    	};
			toolBar.Items.Add(joinButton);
			var split = new SplitContainer
			            	{
			            		Dock = DockStyle.Fill,
								Orientation = Orientation.Horizontal
			            	};
			producerTable = new VirtualTable(new TemplateManager<List<Producer>, Producer>(
				() => Row.Headers("Производитель"), 
				producer => Row.Cells(producer.Name)));
			producerTable.CellSpacing = 1;
			producerTable.RegisterBehavior(new RowSelectionBehavior(),
			                               new AutoSizeBehavior(),
			                               new ToolTipBehavior());
			var behavior = producerTable.Behavior<IRowSelectionBehavior>();
			behavior.SelectedRowChanged += (oldRow, newRow) => SelectedProducerChanged(behavior.Selected<Producer>());

			synonymsTable = new VirtualTable(new TemplateManager<List<SynonymView>, SynonymView>(
				() => Row.Headers("Синоним", "Поставщик", "Регион", "Сегмент"),
				synonym =>
					{
						var row = Row.Cells(synonym.Synonym, synonym.Supplier, synonym.Region, synonym.SegmentAsString());
						if (synonym.HaveOffers == 0)
							row.AddClass("SynonymsWithoutOffers");
						return row;
					}
				));
			synonymsTable.CellSpacing = 1;
			synonymsTable.RegisterBehavior(new RowSelectionBehavior(),
			                               new AutoSizeBehavior(),
			                               new ToolTipBehavior());

			InputLanguageHelper.SetToRussian();
			split.Panel1.Controls.Add(producerTable.Host);
			split.Panel2.Controls.Add(synonymsTable.Host);
			Controls.Add(split);
			Controls.Add(toolBar);
			split.SplitterDistance = (int) (Size.Height*0.6);
			Shown += (sender, args) => producerTable.Host.Focus();
			UpdateProducers();
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
			                                new AutoSizeBehavior(),
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
								//BackColor = Color.Yellow,

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
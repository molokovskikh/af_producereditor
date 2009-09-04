using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using ProducerEditor.Models;
using Subway.Dom;
using Subway.Helpers;
using Subway.VirtualTable;
using Subway.VirtualTable.Behaviors;
using Subway.VirtualTable.Behaviors.Selection;

namespace ProducerEditor.Views
{
	public class JoinView : Dialog
	{
		public JoinView(Controller controller, Producer producer)
		{
			Text = "Объединение производителей";
			Width = 400;
			Height = 500;
			var accept = ((Button) AcceptButton);
			accept.Text = "Объединить";
			AcceptButton = null;
			var producersTable = new VirtualTable(new TemplateManager<List<Producer>, Producer>(
				() => Row.Headers("Производитель"),
				p => Row.Cells(p.Name)
			));
			
			producersTable.CellSpacing = 1;
			var toolStrip = new ToolStrip();
			var searchText = new ToolStripTextBox();

			toolStrip.Items.Add(searchText);
			var searchButton = new ToolStripButton {
				Text = "Поиск"
			};
			
			toolStrip.Items.Add(searchButton);
			producersTable.CellSpacing = 1;
			producersTable.RegisterBehavior(new RowSelectionBehavior(),
			                                new ToolTipBehavior());

			table.Controls.Add(new Label { 
				Padding = new Padding(0, 5, 0, 0),
				AutoSize = true,
				Text = String.Format("Объединить выбранного производителя с {0}.", producer.Name)
			}, 0, 0);
			table.Controls.Add(new Label { 
				Padding = new Padding(0, 0, 0, 5),
				AutoSize = true,
				Text = "Выбранный прозводитель будет удален."
			}, 0, 1);
			table.Controls.Add(toolStrip, 0, 2);
			table.Controls.Add(producersTable.Host, 0, 3);

			Shown += (sender, args) => searchText.Focus();
			searchButton.Click += (sender, args) => DoSearch(producer, controller, searchText, producersTable);
			searchText.InputMap().KeyDown(Keys.Enter, () => DoSearch(producer, controller, searchText, producersTable));

			accept.InputMap().Click(() => Join(producersTable, controller, producer));
			producersTable.Host.InputMap().KeyDown(Keys.Enter, () => Join(producersTable, controller, producer));
		}

		private void Join(VirtualTable producersTable, Controller controller, Producer producer)
		{
			var p = producersTable.Selected<Producer>();
			if (p == null)
			{
				MessageBox.Show("Не выбран производитель для объединения", "Не выбран производитель", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}

			controller.DoJoin(new[] { p }, producer);
			DialogResult = DialogResult.OK;
			Close();
		}

		private void DoSearch(Producer source, Controller controller, ToolStripTextBox text, VirtualTable producersTable)
		{
			var producers = controller.SearchProducer(text.Text).Where(p => source.Id != p.Id).ToList();
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
}
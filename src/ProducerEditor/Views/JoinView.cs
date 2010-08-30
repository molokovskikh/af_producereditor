using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using ProducerEditor.Infrastructure;
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
		private List<ProducerDto> _producers;

		public JoinView(ProducerDto producer, List<ProducerDto> producers)
		{
			_producers = producers;

			Text = "Объединение производителей";
			Width = 400;
			Height = 500;
			var accept = ((Button) AcceptButton);
			accept.Text = "Объединить";
			AcceptButton = null;

			var searcher = new ProducerSearcher(_producers);
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
			table.Controls.Add(searcher.ToolStrip, 0, 2);
			table.Controls.Add(searcher.Table.Host, 0, 3);

			Shown += (sender, args) => searcher.SearchText.Focus();

			accept.InputMap().Click(() => Join(searcher.Table.Selected<ProducerDto>(), producer));
			searcher.Table.Host.InputMap().KeyDown(Keys.Enter, () => Join(searcher.Table.Selected<ProducerDto>(), producer));
		}

		private void Join(ProducerDto p, ProducerDto producer)
		{
			if (p == null)
			{
				MessageBox.Show("Не выбран производитель для объединения", "Не выбран производитель", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}

			Action(s => s.DoJoin(new[] { p }.Select(source => source.Id).ToArray(), producer.Id));
			foreach (var source in new[] { p })
				_producers.Remove(source);

			DialogResult = DialogResult.OK;
			Close();
		}
	}

	public class ProducerSearcher
	{
		private List<ProducerDto> _producers;
		private VirtualTable _table;
		private ToolStripTextBox searchText;

		public ProducerSearcher(List<ProducerDto> producers)
		{
			_producers = producers;
			var producersTable = new VirtualTable(new TemplateManager<List<ProducerDto>, ProducerDto>(
				() => Row.Headers("Производитель"),
				p => Row.Cells(p.Name)
			));
			
			_table = producersTable;
			
			producersTable.CellSpacing = 1;

			ToolStrip = new ToolStrip();
			searchText = new ToolStripTextBox();

			ToolStrip.Items.Add(searchText);
			var searchButton = new ToolStripButton {
				Text = "Поиск"
			};

			ToolStrip.Items.Add(searchButton);
			producersTable.RegisterBehavior(
				new RowSelectionBehavior(),
				new ToolTipBehavior());

			searchButton.Click += (sender, args) => DoSearch(searchText, producersTable);
			searchText.InputMap().KeyDown(Keys.Enter, () => DoSearch(searchText, producersTable));
			producersTable.TemplateManager.Source = producers;
		}

		public VirtualTable Table
		{
			get { return _table; }
		}

		public ToolStripTextBox SearchText
		{
			get { return searchText; }
		}

		public ToolStrip ToolStrip { get; private set; }

		private void DoSearch(ToolStripTextBox text, VirtualTable producersTable)
		{
			var searchText = text.Text;
			List<ProducerDto> producers;
			if (!String.IsNullOrEmpty(searchText))
				producers = _producers.Where(p => p.Name.ToLower().Contains(searchText.ToLower())).ToList();
			else
				producers = _producers;

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
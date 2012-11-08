using System.Collections.Generic;
using System.Windows.Forms;
using ProducerEditor.Contract;
using ProducerEditor.Infrastructure;
using Subway.Helpers;
using Subway.VirtualTable.Behaviors.Selection;

namespace ProducerEditor.Views
{
	public class AddToAssortmentView : Dialog
	{
		public AddToAssortmentView(ExcludeDto exclude, List<ProducerDto> producers)
		{
			Text = "Добавление в ассортимент";
			Width = 400;
			Height = 500;
			var accept = ((Button) AcceptButton);
			accept.Text = "Добавить";
			AcceptButton = null;

			var createEquivalent = new CheckBox {
				AutoSize = true,
				Text = "Создать новый эквивалент?",
				Checked = true
			};
			table.Controls.Add(createEquivalent, 0, 0);
			var equivalent = new TextBox {
				Width = 200,
				Text = exclude.ProducerSynonym
			};
			table.Controls.Add(equivalent, 1, 0);
			table.Controls.Add(new Label {
				Text = "Выберете производителя",
				AutoSize = true,
			}, 0, 1);
			var searcher = new ProducerSearcher(producers);
			Shown += (s, a) => searcher.SearchText.Focus();

			table.Controls.Add(searcher.ToolStrip, 0, 2);
			table.Controls.Add(searcher.Table.Host, 0, 3);

			accept.InputMap().Click(() => Add(searcher.Table.Selected<ProducerDto>(), equivalent.Text, createEquivalent.Checked, exclude));
			searcher.Table.Host.InputMap().KeyDown(Keys.Enter, () => Add(searcher.Table.Selected<ProducerDto>(), equivalent.Text, createEquivalent.Checked, exclude));

		}

		public void Add(ProducerDto producer, string equivalent, bool createEquivalent, ExcludeDto exclude)
		{
			Action(s => {
				if (!createEquivalent)
					equivalent = null;
					if(!s.CheckProductIsMonobrend(exclude.Id, producer.Id))
						s.AddToAssotrment(exclude.Id, producer.Id, equivalent);
					else {
						MessageBox.Show("Товар помечен как \"Монобренд\", добавление к нему нового поставщика возможно только при снятии этой отметки.", "Невозможно добавить поставщика");
					}
			});
		}
	}
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using ProducerEditor.Infrastructure;
using ProducerEditor.Models;

namespace ProducerEditor.Views
{
	public class RenameView : Dialog
	{
		public RenameView(ProducerDto producer, List<ProducerDto> producers, Action<ProducerDto> rename)
		{
			var errorProvider = new ErrorProvider();
			var newName = new TextBox {
				Text = producer.Name,
				Width = 200,
			};
			table.Controls.Add(newName, 0, 0);
			Text = "Переименование производителя";
			Closing += (sender, args) => {
				if (DialogResult == DialogResult.Cancel)
					return;
				if (String.IsNullOrEmpty(newName.Text.Trim()))
				{
					errorProvider.SetError(newName, "Название производителя не может быть пустым");
					errorProvider.SetIconAlignment(newName, ErrorIconAlignment.MiddleRight);
					args.Cancel = true;
					return;
				}
				var existsProducer = producers.FirstOrDefault(p => p.Name.Equals(newName.Text, StringComparison.CurrentCultureIgnoreCase) && p.Id != producer.Id);
				if (existsProducer != null)
				{
					errorProvider.SetError(newName, "Такой производитель уже существует");
					errorProvider.SetIconAlignment(newName, ErrorIconAlignment.MiddleRight);
					args.Cancel = true;
					return;
				}
				producer.Name = newName.Text;
				rename(producer);
			};
		}
	}
}
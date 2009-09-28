using System;
using System.Linq;
using System.Windows.Forms;
using ProducerEditor.Models;

namespace ProducerEditor.Views
{
	public class RenameView : Dialog
	{
		public RenameView(MainController controller, Producer producer)
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
				var existsProducer = controller.Producers.Where(p => p.Name.ToLower() == newName.Text.Trim() && p.Id != producer.Id).FirstOrDefault();
				if (existsProducer != null)
				{
					errorProvider.SetError(newName, "Такой производитель уже существует");
					errorProvider.SetIconAlignment(newName, ErrorIconAlignment.MiddleRight);
					args.Cancel = true;
					return;
				}
				producer.Name = newName.Text;
				controller.Update(producer);
			};
		}
	}
}
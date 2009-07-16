using System;
using System.Windows.Forms;
using ProducerEditor.Models;

namespace ProducerEditor.Views
{
	public class RenameView : Dialog
	{
		public RenameView(Controller controller, Producer producer)
		{
			var errorProvider = new ErrorProvider();
			var newName = new TextBox
			              	{
			              		Text = producer.Name,
			              		Width = 200,
			              	};
			table.Controls.Add(newName, 0, 0);
			Text = "�������������� �������������";
			Closing += (sender, args) =>
			           {
			           	if (DialogResult == DialogResult.Cancel)
			           		return;
			           	if (String.IsNullOrEmpty(newName.Text.Trim()))
			           	{
			           		errorProvider.SetError(newName, "�������� ������������� �� ����� ���� ������");
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
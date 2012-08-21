using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using ProducerEditor.Contract;
using ProducerEditor.Infrastructure;

namespace ProducerEditor.Views
{
	public class RenameView : Dialog
	{
		private ErrorProvider errorProvider;
		private TextBox newName;

		public RenameView(string value)
		{
			errorProvider = new ErrorProvider();
			newName = new TextBox {
				Text = value,
				Width = 200,
			};
			table.Controls.Add(newName, 0, 0);

			Closing += (sender, args) => {
				if (DialogResult == DialogResult.Cancel)
					return;

				if (CheckValidation != null) {
					var error = CheckValidation();
					if (!String.IsNullOrEmpty(error))
						SetError(error, args);
				}
			};
		}

		public event Func<string> CheckValidation;

		public string Value
		{
			get { return newName.Text; }
			set { newName.Text = value; }
		}

		private void SetError(string error, CancelEventArgs args)
		{
			errorProvider.SetError(newName, error);
			errorProvider.SetIconAlignment(newName, ErrorIconAlignment.MiddleRight);
			args.Cancel = true;
		}
	}
}
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProducerEditor.Models;
using System.Windows.Forms;

namespace ProducerEditor.Views
{
	public class CreateEquivalentView : Dialog
	{		
		public CreateEquivalentView(Producer producer, IEnumerable<string> existsEquivalents, Action<string, uint> actionCreateEquivalent)
		{
			var errorProvider = new ErrorProvider();
			
			var equivalentText = new TextBox() {
                Text = String.Empty,
                Width = 200
            };
			equivalentText.Text = String.Empty;
			equivalentText.Width = 200;

			table.Controls.Add(equivalentText, 0, 0);
			Text = "Создание эквивалента";
			Closing += (sender, args) => {
                if (DialogResult == DialogResult.Cancel)
                	return;
				if (String.IsNullOrEmpty(equivalentText.Text.Trim()))
				{
					errorProvider.SetError(equivalentText, "Значение эквивалента не может быть пустой строкой");
					errorProvider.SetIconAlignment(equivalentText, ErrorIconAlignment.MiddleRight);
					args.Cancel = true;
					return;
				}
				if (existsEquivalents != null)
				{
					var exists =
						existsEquivalents.Where(name => name.ToLower() == equivalentText.Text.ToLower().Trim()).FirstOrDefault();
					if (exists != null)
					{
						errorProvider.SetError(equivalentText, "Такой эквивалент уже существует");
						errorProvider.SetIconAlignment(equivalentText, ErrorIconAlignment.MiddleRight);
						args.Cancel = true;
						return;
					}
				}
                actionCreateEquivalent(equivalentText.Text, producer.Id);
            };
		}
	}
}
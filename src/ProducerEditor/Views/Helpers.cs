using System;
using System.Globalization;
using System.Windows.Forms;

namespace ProducerEditor.Views
{
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
			flow.Controls.Add((Control)AcceptButton);
			flow.Controls.Add((Control)CancelButton);
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
}

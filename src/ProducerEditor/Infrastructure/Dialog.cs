using System.Windows.Forms;
using View = ProducerEditor.Infrastructure.View;

namespace ProducerEditor.Infrastructure
{
	public class Dialog : View
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
		}
	}
}

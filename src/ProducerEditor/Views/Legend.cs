using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace ProducerEditor.Views
{
	public class Legend : UserControl
	{
		Dictionary<string, string> knownStyles = new Dictionary<string, string>{
			{"WithoutOffers", "���� �����������"},
			{"SameAsCurrent", "������� ������"}
		};

		public Legend(params string[] styles)
		{
			Dock = DockStyle.Bottom;
			AutoSize = true;
			var flowPanel = new FlowLayoutPanel();
			flowPanel.AutoSize = true;
			flowPanel.SuspendLayout();
			SuspendLayout();

			foreach (var item in styles)
			{
				var label = knownStyles[item];
				Color color = Color.White;
				if (item == "SameAsCurrent")
					color = Color.FromArgb(222, 201, 231);
				else if (item == "WithoutOffers")
					color = Color.FromArgb(231, 231, 200);
/*
				�� �������� �� mix �� ������ � ����� �������� � ������� ���� ���������
				���� ��� �� ���������, �������� ������� � ����� �������� � ��������� ������ ��������� ��� ����� �����������
				var style = StylesHolder.Instance.GetStyle(item);
				var styleColor = style.Get(StyleElementType.BackgroundColor);
				var color = Color.FromArgb(styleColor.R, styleColor.G, styleColor.B);
*/
				flowPanel.Controls.Add(new Label {
					AutoSize = true,
					BackColor = color,
					Margin = new Padding(5, 5, 0, 5),
					Padding = new Padding(styles.Length),
					Text = label,
					TextAlign = ContentAlignment.MiddleCenter
				});
			}

			flowPanel.Dock = DockStyle.Fill;

			Controls.Add(flowPanel);
			Name = "Legend";
			flowPanel.ResumeLayout(false);
			ResumeLayout(false);
		}
	}
}
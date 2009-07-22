using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ProducerEditor.Models;
using Subway.Dom;
using Subway.VirtualTable;
using Subway.VirtualTable.Behaviors;
using Subway.VirtualTable.Behaviors.Selection;

namespace ProducerEditor.Views
{
	public class JoinView : Dialog
	{
		public JoinView(Controller controller, Producer producer)
		{
			Text = "����������� ��������������";
			Width = 400;
			Height = 500;
			((Button) AcceptButton).Text = "����������";
			var producersTable = new VirtualTable(new TemplateManager<List<Producer>, Producer>(
			                                      	() => Row.Headers("�������������"),
			                                      	p => Row.Cells(p.Name)
			                                      	));
			producersTable.CellSpacing = 1;
			var toolStrip = new ToolStrip();
			var text = new ToolStripTextBox();
			text.KeyDown += (sender, args) => {
				/*if (args.KeyCode == Keys.Enter)
				{
				DoSearch(controller, text, producersTable);
				args.Handled = true;
				args.SuppressKeyPress = true;
				}*/
			};
			toolStrip.Items.Add(text);
			var button = new ToolStripButton {
				Text = "�����"
			};
			button.Click += (sender, args) => DoSearch(producer, controller, text, producersTable);
			toolStrip.Items.Add(button);
			producersTable.CellSpacing = 1;
			producersTable.RegisterBehavior(new RowSelectionBehavior(),
			                                new ToolTipBehavior());


			table.Controls.Add(new Label { 
				Padding = new Padding(0, 5, 0, 5),
				AutoSize = true,
				Text = String.Format("���������� ���������� ������������� � {0}", producer.Name)
			}, 0, 0);
			table.Controls.Add(toolStrip, 0, 1);
			table.Controls.Add(producersTable.Host, 0, 2);
			Closing += (o, a) => {
				if (DialogResult == DialogResult.Cancel)
					return;

				var p = producersTable.Selected<Producer>();
				if (p == null)
				{
					MessageBox.Show("�� ������ ������������� ��� �����������", "�� ������ �������������", MessageBoxButtons.OK, MessageBoxIcon.Warning);
					a.Cancel = true;
					return;
				}

				controller.DoJoin(new[] { p }, producer);
			};
			Shown += (sender, args) => text.Focus();
		}

		private void DoSearch(Producer source, Controller controller, ToolStripTextBox text, VirtualTable producersTable)
		{
			var producers = controller.SearchProducer(text.Text).Where(p => source.Id != p.Id).ToList();
			if (producers.Count > 0)
			{
				producersTable.TemplateManager.Source = producers;
				producersTable.Host.Focus();
			}
			else
			{
				MessageBox.Show("�� ������ ������� ������� �� �������", "���������� ������",
				                MessageBoxButtons.OK,
				                MessageBoxIcon.Warning);
			}
		}
	}
}
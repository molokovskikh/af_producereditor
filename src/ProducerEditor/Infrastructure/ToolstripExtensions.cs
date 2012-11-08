using System;
using System.Windows.Forms;

namespace ProducerEditor.Infrastructure
{
	public static class ToolstripExtensions
	{
		public static ToolStrip Edit(this ToolStrip toolStrip, string name)
		{
			var edit = new ToolStripTextBox {
				Name = name
			};
			toolStrip.Items.Add(edit);
			return toolStrip;
		}

		public static ToolStrip Button(this ToolStrip toolStrip, string label, Action onclick)
		{
			var button = new ToolStripButton {
				Text = label
			};
			button.Click += (sender, args) => onclick();
			toolStrip.Items.Add(button);
			return toolStrip;
		}

		public static ToolStrip Button(this ToolStrip toolStrip, string name, string label, Action onclick)
		{
			var button = new ToolStripButton {
				Text = label,
				Name = name
			};
			button.Click += (sender, args) => onclick();
			toolStrip.Items.Add(button);
			return toolStrip;
		}

		public static ToolStrip Item(this ToolStrip toolStrip, ToolStripItem item)
		{
			toolStrip.Items.Add(item);
			return toolStrip;
		}

		public static ToolStrip Button(this ToolStrip toolStrip, string name, string label)
		{
			var button = new ToolStripButton {
				Text = label,
				Name = name
			};
			toolStrip.Items.Add(button);
			return toolStrip;
		}

		public static ToolStrip Host(this ToolStrip toolStrip, Control control)
		{
			var host = new ToolStripControlHost(control);
			toolStrip.Items.Add(host);
			return toolStrip;
		}

		public static ToolStrip Label(this ToolStrip toolStrip, string label)
		{
			toolStrip.Items.Add(new ToolStripLabel {
				Text = label
			});
			return toolStrip;
		}

		public static ToolStrip Label(this ToolStrip toolStrip, string name, string label)
		{
			toolStrip.Items.Add(new ToolStripLabel {
				Text = label,
				Name = name,
			});
			return toolStrip;
		}

		public static ToolStrip Separator(this ToolStrip toolStrip)
		{
			toolStrip.Items.Add(new ToolStripSeparator());
			return toolStrip;
		}
	}
}
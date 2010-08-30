using System;
using System.Linq;
using System.Windows.Forms;
using Common.Tools;

namespace ProducerEditor.Infrastructure
{
	public static class NavigatorExtention
	{
		public static ToolStrip ActAsNavigator(this ToolStrip toolStrip)
		{
			toolStrip.Items
				.OfType<ToolStripButton>()
				.Each(b => b.Click += MaintainNavigation);
			return toolStrip;
		}

		private static void MaintainNavigation(object sender, EventArgs e)
		{
			var button = (ToolStripButton) sender;
			if (!button.Checked)
				button.Checked = true;

			button.Owner
				.Items.OfType<ToolStripButton>()
				.Where(b => b != button && b.Checked)
				.Each(b => b.Checked = false);
		}
	}
}
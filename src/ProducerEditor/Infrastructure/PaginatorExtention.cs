using System;
using System.Linq;
using System.Windows.Forms;
using Common.Tools;
using ProducerEditor.Contract;
using Subway.Helpers;
using Subway.VirtualTable;

namespace ProducerEditor.Infrastructure
{
	public static class PaginatorExtention
	{
		public static string TableName = "TableHost";

		public static ToolStrip ActAsPaginator(this ToolStrip toolStrip, IPager pager, Func<uint, IPager> page)
		{
			Action<ToolStripButton> move = b => {
				var pageIndex = Convert.ToInt32(b.Tag);
				if (pageIndex < 0)
					return;

				pager = page((uint)pageIndex);
				toolStrip.UpdatePaginator(pager);
			};
			toolStrip.Items["Prev"].Click += (s, a) => move((ToolStripButton)s);
			toolStrip.Items["Next"].Click += (s, a) => move((ToolStripButton)s);

			if (pager != null)
				toolStrip.UpdatePaginator(pager);

			var form = toolStrip.Parent;
			if (form == null)
				throw new Exception("У paginatora нет родителя, всего скорее ты нужно добавлять поведение позже");

			var controls = form.Controls.Cast<Control>().Flat(control => control.Controls.Cast<Control>());
			var tables = controls.Where(control => control is TableHost);
			Control table;
			if (tables.Count() > 1)
				table = tables.Where(t => t.Tag != null).First(control => String.Compare(control.Tag.ToString(), TableName) == 0);
			else
				table = tables.First();
			table.InputMap()
				.KeyDown(Keys.Left, () => move((ToolStripButton)toolStrip.Items["Prev"]))
				.KeyDown(Keys.Right, () => move((ToolStripButton)toolStrip.Items["Next"]));
			return toolStrip;
		}

		public static void UpdatePaginator(this ToolStrip toolStrip, IPager pager)
		{
			toolStrip.Items["PageLabel"].Text = String.Format("Страница {0} из {1}", pager.Page + 1, pager.TotalPages);

			var next = toolStrip.Items["Next"];
			next.Enabled = pager.Page < pager.TotalPages - 1;
			if (next.Enabled)
				next.Tag = pager.Page + 1;
			else
				next.Tag = -1;

			var prev = toolStrip.Items["Prev"];
			prev.Enabled = pager.Page > 0;
			if (prev.Enabled)
				prev.Tag = pager.Page - 1;
			else
				prev.Tag = -1;
		}
	}
}
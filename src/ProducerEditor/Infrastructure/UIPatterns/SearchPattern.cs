using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Common.Tools;

namespace ProducerEditor.Infrastructure.UIPatterns
{
	public class SearchPattern : IUIPattern
	{
		private object _presenter;
		private MethodInfo _method;

		public SearchPattern(object presenter)
		{
			_presenter = presenter;
		}

		public void Apply(Form view)
		{
			var tools = view.Children().OfType<ToolStrip>().ToArray();
			var toolStrip = tools.FirstOrDefault(t => ((string)t.Tag).Match("Searchable"));
			if (toolStrip == null)
				toolStrip = tools.FirstOrDefault();

			if (toolStrip == null) {
				toolStrip = new ToolStrip();
				view.Controls.Add(toolStrip);
			}
			toolStrip.Items.Insert(0, new ToolStripTextBox("SearchText"));
			toolStrip.Items.Insert(1, new ToolStripButton("Поиск") { Name = "Search" });
			toolStrip.Items.Insert(2, new ToolStripSeparator());

			var searchText = ((ToolStripTextBox)toolStrip.Items["SearchText"]);
			toolStrip.Items["Search"].Click += (s, a) => { Invoke(searchText.Text); };
			searchText.KeyDown += (s, a) => {
				if (a.KeyCode == Keys.Enter)
					Invoke(searchText.Text);
			};
		}

		public void Invoke(string text)
		{
			_method.Invoke(_presenter, new object[] { text });
		}

		public bool IsApplicable()
		{
			_method = _presenter.GetType().GetMethod("Search");
			return _method != null;
		}
	}
}
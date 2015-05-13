using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Common.Tools;

namespace ProducerEditor.Infrastructure.UIPatterns
{
	public class SearchPattern : IUIPattern
	{
		private object _presenter;
		private object _view;
		private MethodInfo _method;

		public SearchPattern(object presenter)
		{
			_presenter = presenter;
			_method = _presenter.GetType().GetMethod("Search");
		}

		public void Apply(Form view)
		{
			_view = view;
			_method = _method
				?? view.GetType().GetMethod("Search");

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
			if (_method.DeclaringType != null && _method.DeclaringType.IsInstanceOfType(_presenter))
				_method.Invoke(_presenter, new object[] { text });
			if (_method.DeclaringType != null && _method.DeclaringType.IsInstanceOfType(_view))
				_method.Invoke(_view, new object[] { text });
		}

		public bool IsApplicable(Form view)
		{
			return _method != null || view.GetType().GetMethod("Search") != null;
		}
	}
}
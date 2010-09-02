using System.Linq;
using System.Reflection;
using System.Windows.Forms;

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
			var tools = view.Children().OfType<ToolStrip>().First();
			if (tools == null)
			{
				tools = new ToolStrip();
				view.Controls.Add(tools);
			}
			tools.Items.Insert(0, new ToolStripTextBox("SearchText"));
			tools.Items.Insert(1, new ToolStripButton("Поиск"){Name = "Search"});
			tools.Items.Insert(2, new ToolStripSeparator());

			var searchText = ((ToolStripTextBox)tools.Items["SearchText"]);
			tools.Items["Search"].Click += (s, a) => {
				Invoke(searchText.Text);
			};
			searchText.KeyDown += (s, a) => {
				if (a.KeyCode == Keys.Enter)
					Invoke(searchText.Text);
			};
		}

		public void Invoke(string text)
		{
			_method.Invoke(_presenter, new object[] {text});
		}

		public bool IsApplicable()
		{
			_method = _presenter.GetType().GetMethod("Search");
			return _method != null;
		}
	}
}
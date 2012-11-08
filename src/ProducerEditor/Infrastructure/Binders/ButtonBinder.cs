using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using ProducerEditor.Infrastructure.UIPatterns;
using Subway.VirtualTable;

namespace ProducerEditor.Infrastructure.Binders
{
	public class ButtonBinder
	{
		private object _presenter;

		public Dictionary<string, Keys> knownKeys = new Dictionary<string, Keys> {
			{ "Delete", Keys.Delete },
			{ "Rename", Keys.F2 },
		};

		public ButtonBinder(object presenter)
		{
			_presenter = presenter;
		}

		public IEnumerable<ToolStripButton> Apply(View view, IEnumerable<ToolStripButton> buttons)
		{
			return buttons
				.Where(b => !String.IsNullOrEmpty(b.Name))
				.Where(b => BindAction(view, b, b.Name));
		}

		private bool BindAction(View view, ToolStripButton button, string action)
		{
			var methods = _presenter.GetType().GetMethods()
				.Where(m => m.Name == action
					&& m.GetParameters().Length == 1
					&& m.GetParameters()[0].Name != "flag")
				.ToArray();

			if (methods.Length == 0)
				return false;

			foreach (var methodInfo in methods) {
				//замыкание!
				var method = methodInfo;
				var parameter = methodInfo.GetParameters()[0];
				var table = view.GetTableForParameter(parameter);
				if (table != null
					&& knownKeys.ContainsKey(action)) {
					var hotKey = knownKeys[action];
					table.KeyDown += (sender, args) => {
						if (args.KeyCode == hotKey) {
							Invoke(view, method, parameter);
						}
					};
				}
			}

			button.Click += (sender, args) => {
				var host = view.Children().OfType<TableHost>().FirstOrDefault(h => h.Focused);
				if (host == null)
					return;
				var method = methods.FirstOrDefault(m => view.GetTableForParameter(m.GetParameters()[0]) == host);
				if (method == null)
					return;
				Invoke(view, method, method.GetParameters()[0]);
			};
			return true;
		}

		private void Invoke(View view, MethodInfo methodInfo, ParameterInfo parameter)
		{
			var value = view.GetCurrent(parameter);
			if (value == null)
				return;
			methodInfo.Invoke(_presenter, new[] { value });
		}
	}
}
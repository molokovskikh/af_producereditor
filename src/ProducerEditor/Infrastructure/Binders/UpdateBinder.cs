using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ProducerEditor.Models;
using Subway.VirtualTable;

namespace ProducerEditor.Infrastructure.Binders
{
	public class UpdateBinder : IBinder
	{
		private View view;

		public void Bind(object presenter, View view)
		{
			this.view = view;
			var presenterType = presenter.GetType();
			var update = presenterType.GetEvent("Update");
			if (update == null)
				return;
			update.AddEventHandler(presenter, new Action<string, object>(Bind));
		}

		public void Bind(string name, object value)
		{
			var control = view.Children().FirstOrDefault(c => c.Name == name);
			if (control == null)
				return;
			if (control is TableHost)
			{
				((TableHost) control).Table.TemplateManager.Source = ConvertValue(value);
			}
		}

		private object ToList(object content, Type genericArgument)
		{
			var constructor =
				typeof (List<>).MakeGenericType(genericArgument).GetConstructor(new[]
				{typeof (IEnumerable<>).MakeGenericType(genericArgument)});
			return constructor.Invoke(new [] {content});
		}

		private object ConvertValue(object value)
		{
			if (value is IList)
				return value;
			if (value is IPager)
			{
				var content = value.GetType().GetProperty("Content").GetValue(value, null);
				var genericArgument = value.GetType().GetGenericArguments()[0];
				if (content.GetType() != typeof(List<>).MakeGenericType(genericArgument))
					content = ToList(content, genericArgument);
				return content;
			}
			return null;
		}
	}
}
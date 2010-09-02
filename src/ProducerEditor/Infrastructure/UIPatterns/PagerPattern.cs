﻿using System.Reflection;
using System.Windows.Forms;
using ProducerEditor.Models;

namespace ProducerEditor.Infrastructure.UIPatterns
{
	public class PagerPattern : IUIPattern
	{
		private MethodInfo _method;
		private object _presenter;

		public PagerPattern(object presenter)
		{
			_presenter = presenter;
		}

		public void Apply(Form view)
		{
			var navigation = new ToolStrip()
				.Button("Prev", "Передыдущая страница")
				.Label("PageLabel", "")
				.Button("Next", "Следующая страница");

			view.Controls.Add(navigation);

			var pager = (IPager) _presenter.GetType().GetProperty("page").GetValue(_presenter, null);

			navigation.ActAsPaginator(pager, page => {
				var paginator = Invoke(page);
				return paginator;
			});
		}

		public IPager Invoke(uint page)
		{
			return (IPager)_method.Invoke(_presenter, new object[] {page});
		}

		public bool IsApplicable()
		{
			_method = _presenter.GetType().GetMethod("Page");
			return _method != null;
		}
	}
}
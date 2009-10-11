using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Windows.Forms;
using ProducerEditor.Models;

namespace ProducerEditor.Infrastructure
{
	public class MvcHelper
	{
		public static Type GetViewType(string name)
		{
			var viewType = Assembly.GetExecutingAssembly()
				.GetTypes()
				.Where(t => !String.IsNullOrEmpty(t.Namespace) && t.Namespace.EndsWith(".Views") && t.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase))
				.FirstOrDefault();
			if (viewType == null)
				throw new Exception(String.Format("Не могу найти вид {0}", name));
			return viewType;
		}

		public static string GetViewName<T>(Expression<Func<ProducerService, T>> expression)
		{
			if (!(expression.Body is MethodCallExpression))
				throw new Exception(String.Format("Не могу понять что за вызов такой {0}", expression));
			return ((MethodCallExpression)expression.Body).Method.Name;
		}

		public static void ShowDialog(Type type, params object[] args)
		{
			var form = (Form) Activator.CreateInstance(type, args);
			form.ShowDialog();
		}
	}
}

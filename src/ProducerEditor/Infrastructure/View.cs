using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.ServiceModel;
using System.Windows.Forms;
using Common.Tools;
using log4net;
using ProducerEditor.Infrastructure.UIPatterns;
using ProducerEditor.Models;
using Subway.VirtualTable;
using Subway.VirtualTable.Behaviors.Selection;

namespace ProducerEditor.Infrastructure
{
	public static class Ex
	{
		public static IEnumerable<Control> Children(this Control control)
		{
			return control.Controls.Cast<Control>().Flat(c => c.Controls.Cast<Control>());
		}
	}

	public interface IBinder
	{
		void Bind(object presenter, View view);
	}

	public class CurrentBinder : IBinder
	{
		public void Bind(object presenter, View view)
		{
			var presenterType = presenter.GetType();
			var method = presenterType.GetMethod("CurrentChanged");
			if (method == null)
				return;
			var parameters = method.GetParameters();
			if (parameters.Length != 1)
				return;
			var host = view.GetTableForParameter(parameters[0]);
			if (host == null)
				return;
			host.Table.Behavior<IRowSelectionBehavior>().SelectedRowChanged += (oldIndex, newIndex) => {
				var current = host.Table.Selected();
				if (current == null)
					return;
				method.Invoke(presenter, new[] {current});
			};
		}
	}

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
				typeof (List<>).MakeGenericType(genericArgument).GetConstructor(new Type[]
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

	public abstract class View : Form
	{
		private ILog _log = LogManager.GetLogger(typeof (Form));

		protected object Presenter;

		public IBinder[] binders = new IBinder[] {
			new UpdateBinder(),
			new CurrentBinder()
		};

		public View()
		{
			Presenter = GetPresenter();

			Init();

			if (Presenter == null)
				return;

			WireBinding();

			var buttons = this.Children().OfType<ToolStrip>().SelectMany(t => t.Items.Cast<ToolStripItem>().OfType<ToolStripButton>());
			foreach (var button in buttons)
				WireButtonTo(button, Presenter);
		}

		private void WireBinding()
		{
			foreach (var binder in binders)
				binder.Bind(Presenter, this);
		}

		private void WireButtonTo(ToolStripButton button, object presenter)
		{
			var method = presenter.GetType().GetMethod(button.Name);
			if (method == null)
				return;

			button.Click += (s, a) => {
				TableHost table = null;
				if (method.GetParameters().Any(p => p.Name == "current"))
					table = GetTableForParameter(method.GetParameters().First(p => p.Name == "current"));
				int selectedIndex = 0;
				if (table != null)
					selectedIndex = table.Table.Behavior<IRowSelectionBehavior>().SelectedRowIndex;

				var parameters = BindParameters(method);
				if (parameters == null)
					return;
				method.Invoke(presenter, parameters);

				if (table != null)
					table.Table.Behavior<IRowSelectionBehavior>().MoveSelectionAt(selectedIndex);
			};
		}

		protected virtual void Init()
		{}

		private object[] BindParameters(MethodInfo method)
		{
			var parameters = method.GetParameters();
			if (parameters.Length == 0)
				return new object[0];
			var list = new List<object>();
			foreach (var parameter in parameters)
			{
				if (parameter.Name == "current")
				{
					var current = GetCurrent(parameter);
					if (current == null)
						return null;
					list.Add(current);
				}
				else 
					throw new Exception(String.Format("Не знаю как биндить параметер {0} метода {1}", parameter.Name, method.Name));
			}
			return list.ToArray();
		}

		private object GetCurrent(ParameterInfo parameter)
		{
			var table = GetTableForParameter(parameter);
			if (table == null)
				return null;
			return table.Table.Selected();
		}

		public TableHost GetTableForParameter(ParameterInfo parameter)
		{
			return this.Children().OfType<TableHost>().Where(h => h.Name == parameter.ParameterType.Name + "s").FirstOrDefault();
		}

		private object GetPresenter()
		{
			var type = Assembly.GetExecutingAssembly().GetType("ProducerEditor.Presenters." + GetType().Name + "Presenter");
			if (type == null)
				return null;
			return Activator.CreateInstance(type);
		}

		private IEnumerable<IUIPattern> DetectPatterns(object presenter)
		{
			return new IUIPattern[] {new PagerPattern(presenter), new SearchPattern(presenter)}.Where(p => p.IsApplicable());
		}

		protected virtual Action Controller<T>(Expression<Func<ProducerService, T>> func)
		{
			return () => WithService(s => {
				var viewName = MvcHelper.GetViewName(func);
				var viewType = MvcHelper.GetViewType(viewName);
				var result = func.Compile()(s);
				MvcHelper.ShowDialog(viewType, result);
			});
		}

		protected T Request<T>(Func<ProducerService, T> func)
		{
			var result = default(T);
			WithService(s => {
				result = func(s);
			});
			return result;
		}

		protected void Action(Action<ProducerService> action)
		{
			WithService(action);
		}

		protected void WithService(Action<ProducerService> action)
		{
			ICommunicationObject communicationObject = null;
			try
			{
				var chanel = FactoryHolder.Factory.CreateChannel();
				communicationObject = chanel as ICommunicationObject;
				action(chanel);
				communicationObject.Close();
			}
			catch (Exception e)
			{
				if (communicationObject != null 
					&& communicationObject.State != CommunicationState.Closed)
					communicationObject.Abort();

				_log.Error("Ошибка при обращении к серверу", e);
				throw;
			}
		}

		protected void Wire()
		{
			var patterns = DetectPatterns(Presenter);
			foreach (var pattern in patterns)
				pattern.Apply(this);
		}
	}
}
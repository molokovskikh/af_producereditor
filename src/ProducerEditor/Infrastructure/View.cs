using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.ServiceModel;
using System.Windows.Forms;
using Common.Tools;
using ProducerEditor.Contract;
using log4net;
using ProducerEditor.Infrastructure.Binders;
using ProducerEditor.Infrastructure.UIPatterns;
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

	public abstract class View : Form
	{
		private ILog _log = LogManager.GetLogger(typeof(Form));

		public object Presenter;

		public IBinder[] binders = new IBinder[] {
			new UpdateBinder(),
			new CurrentBinder(),
		};

		public View()
		{
			Presenter = GetPresenter();

			Init();
			new UIContributor().Contribute(this);

			if (Presenter == null)
				return;

			WireBinding();

			var buttons = this.Children().OfType<ToolStrip>().SelectMany(t => t.Items.Cast<ToolStripItem>().OfType<ToolStripButton>());
			var consumedButtons = new ButtonBinder(Presenter).Apply(this, buttons);
			buttons = buttons.Except(consumedButtons);

			foreach (var button in buttons) {
				DefaultWireButtonTo(button, Presenter);
			}

			Wire();
		}

		private void WireBinding()
		{
			foreach (var binder in binders)
				binder.Bind(Presenter, this);
		}

		private void DefaultWireButtonTo(ToolStripButton button, object presenter)
		{
			var method = presenter.GetType().GetMethod(button.Name);
			if (method == null)
				return;

			button.Click += (s, a) => {
				TableHost table = null;
				var parameter = method.GetParameters().FirstOrDefault(p => p.Name == "current");
				if (parameter != null)
					table = GetTableForParameter(parameter);

				int selectedIndex = 0;
				if (table != null)
					selectedIndex = table.Table.Behavior<IRowSelectionBehavior>().SelectedRowIndex;

				var parameters = BindParameters(((ToolStripButton)s), method);
				if (parameters == null)
					return;
				method.Invoke(presenter, parameters);

				if (table != null)
					table.Table.Behavior<IRowSelectionBehavior>().MoveSelectionAt(selectedIndex);
			};
		}

		protected virtual void Init()
		{
		}

		public object[] BindParameters(ToolStripButton button, MethodInfo method)
		{
			var parameters = method.GetParameters();
			if (parameters.Length == 0)
				return new object[0];
			var list = new List<object>();
			foreach (var parameter in parameters) {
				if (parameter.Name == "current") {
					var current = GetCurrent(parameter);
					if (current == null)
						return null;
					list.Add(current);
				}
				else if (parameter.Name == "flag") {
					list.Add(button.Checked);
				}
				else
					throw new Exception(String.Format("Не знаю как биндить параметер {0} метода {1}", parameter.Name, method.Name));
			}
			return list.ToArray();
		}

		public object GetCurrent(ParameterInfo parameter)
		{
			var table = GetTableForParameter(parameter);
			if (table == null)
				return null;
			return table.Table.Selected();
		}

		public TableHost GetTableForParameter(ParameterInfo parameter)
		{
			var name = parameter.ParameterType.Name.Replace("Dto", "") + "s";
			return this.Children().OfType<TableHost>().FirstOrDefault(h => h.Name == name);
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
			var uiPatterns = new IUIPattern[] { new PagerPattern(presenter), new SearchPattern(presenter) };
			return uiPatterns.Where(p => p.IsApplicable());
		}

		protected virtual Action Controller<T>(Expression<Func<IProducerService, T>> func)
		{
			return () => WithService(s => {
				var viewName = MvcHelper.GetViewName(func);
				var viewType = MvcHelper.GetViewType(viewName);
				var result = func.Compile()(s);
				MvcHelper.ShowDialog(viewType, result);
			});
		}

		protected T Request<T>(Func<IProducerService, T> func)
		{
			var result = default(T);
			WithService(s => { result = func(s); });
			return result;
		}

		protected void Action(Action<IProducerService> action)
		{
			WithService(action);
		}

		protected void WithService(Action<IProducerService> action)
		{
			FactoryHolder.WithService(action);
		}

		private void Wire()
		{
			var patterns = DetectPatterns(Presenter);
			foreach (var pattern in patterns)
				pattern.Apply(this);
		}
	}
}
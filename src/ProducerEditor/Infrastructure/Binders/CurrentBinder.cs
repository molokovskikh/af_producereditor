using Subway.VirtualTable.Behaviors.Selection;

namespace ProducerEditor.Infrastructure.Binders
{
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
				method.Invoke(presenter, new[] { current });
			};
		}
	}
}
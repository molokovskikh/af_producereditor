using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Reflection;
using System.ServiceModel;
using System.Windows.Forms;
using ProducerEditor.Contract;
using ProducerEditor.Infrastructure;
using ProducerEditor.Views;
using log4net;

namespace ProducerEditor.Presenters
{
	public class Presenter
	{
		//для тестирования
		public bool UnderTest;

		//для тестирования
		public event Func<Form, DialogResult> Dialog;

		protected ILog _log = LogManager.GetLogger(typeof (ShowExcludesPresenter));
		public event Action<string, object> Update;

		protected void OnUpdate(string name, object value)
		{
			if (Update != null)
				Update(name, value);
		}

		protected void WithService(Action<IProducerService> action)
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

		protected void Action(Action<IProducerService> action)
		{
			WithService(action);
		}

		protected T Request<T>(Func<IProducerService, T> func)
		{
			var result = default(T);
			WithService(s => {
				result = func(s);
			});
			return result;
		}

		protected DialogResult ShowDialog(RenameView rename)
		{
			if (UnderTest)
				return Dialog(rename);
			else
				return rename.ShowDialog();
		}

		protected void RefreshView<T>(ObservableCollection<T> collection)
		{
			collection.GetType()
				.GetMethod("OnCollectionChanged",
					BindingFlags.NonPublic | BindingFlags.Instance,
					null,
					CallingConventions.Any,
					new[] { typeof(NotifyCollectionChangedEventArgs) },
					null)
				.Invoke(collection, new object[] { null });
		}
	}
}
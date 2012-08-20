using System;
using System.ServiceModel;
using ProducerEditor.Infrastructure;
using ProducerEditor.Models;
using log4net;

namespace ProducerEditor.Presenters
{
	public class Presenter
	{
		protected ILog _log = LogManager.GetLogger(typeof (ShowExcludesPresenter));
		public event Action<string, object> Update;

		protected void OnUpdate(string name, object value)
		{
			if (Update != null)
				Update(name, value);
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

		protected void Action(Action<ProducerService> action)
		{
			WithService(action);
		}

		protected T Request<T>(Func<ProducerService, T> func)
		{
			var result = default(T);
			WithService(s => {
				result = func(s);
			});
			return result;
		}
	}
}
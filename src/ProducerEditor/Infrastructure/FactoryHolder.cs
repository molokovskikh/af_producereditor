using System;
using System.ServiceModel;
using System.ServiceModel.Dispatcher;
using ProducerEditor.Contract;
using log4net;

namespace ProducerEditor.Infrastructure
{
	public class FactoryHolder
	{
		private static ILog _log = LogManager.GetLogger(typeof(FactoryHolder));

		public static ChannelFactory<IProducerService> Factory;

		static FactoryHolder()
		{
			Factory = new ChannelFactory<IProducerService>(Settings.Binding, Settings.Endpoint);
			Factory.Endpoint.Behaviors.Add(new MessageInspectorRegistrator(new IClientMessageInspector[] {
				new UserNameInspector()
			}));
		}

		public static void WithService(Action<IProducerService> action)
		{
			ICommunicationObject communicationObject = null;
			try
			{
				var chanel = Factory.CreateChannel();
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
	}
}
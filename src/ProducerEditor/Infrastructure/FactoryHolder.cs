using System.ServiceModel;
using System.ServiceModel.Dispatcher;
using ProducerEditor.Contract;

namespace ProducerEditor.Infrastructure
{
	public class FactoryHolder
	{
		public static ChannelFactory<IProducerService> Factory;

		static FactoryHolder()
		{
			Factory = new ChannelFactory<IProducerService>(Settings.Binding, Settings.Endpoint);
			Factory.Endpoint.Behaviors.Add(new MessageInspectorRegistrator(new IClientMessageInspector[] {
				new UserNameInspector()
			}));
		}
	}
}
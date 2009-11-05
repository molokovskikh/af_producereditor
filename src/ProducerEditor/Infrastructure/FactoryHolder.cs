using System.ServiceModel;
using System.ServiceModel.Dispatcher;
using ProducerEditor.Models;

namespace ProducerEditor.Infrastructure
{
	public class FactoryHolder
	{
		public static ChannelFactory<ProducerService> Factory;

		static FactoryHolder()
		{
			Factory = new ChannelFactory<ProducerService>(Settings.Binding, Settings.Endpoint);
			Factory.Endpoint.Behaviors.Add(new MessageInspectorRegistrator(new IClientMessageInspector[] {
				new UserNameInspector()
			}));
		}
	}
}
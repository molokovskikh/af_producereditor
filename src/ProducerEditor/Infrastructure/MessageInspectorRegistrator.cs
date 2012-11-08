using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;

namespace ProducerEditor.Infrastructure
{
	public class MessageInspectorRegistrator : IEndpointBehavior
	{
		private readonly IClientMessageInspector[] _inspectors;

		public MessageInspectorRegistrator(IClientMessageInspector[] inspectors)
		{
			_inspectors = inspectors;
		}

		public void Validate(ServiceEndpoint endpoint)
		{
		}

		public void AddBindingParameters(ServiceEndpoint endpoint,
			BindingParameterCollection bindingParameters)
		{
		}

		public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
		{
		}

		public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
		{
			foreach (var inspector in _inspectors)
				clientRuntime.MessageInspectors.Add(inspector);
		}
	}
}
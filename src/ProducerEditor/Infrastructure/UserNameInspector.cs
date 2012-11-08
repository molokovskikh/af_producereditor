using System;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;

namespace ProducerEditor.Infrastructure
{
	public class UserNameInspector : IClientMessageInspector
	{
		public object BeforeSendRequest(ref Message request, IClientChannel channel)
		{
			request.Headers.Add(MessageHeader.CreateHeader("UserName", "", Environment.UserName));
			return null;
		}

		public void AfterReceiveReply(ref Message reply, object correlationState)
		{
		}
	}
}
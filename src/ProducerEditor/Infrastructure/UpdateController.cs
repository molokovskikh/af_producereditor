using System;
using System.ServiceModel;
using log4net;
using ProducerEditor.Contract;
using ProducerEditor.Models;
using Subway.Dom;
using Subway.Dom.Input;
using Subway.Table;

namespace ProducerEditor.Infrastructure
{
	public class InputController : InputSupport
	{
		public InputController()
		{
			Click = input => {
				var row = (Row) input.Parent.Parent;
				var producer = Host.Table.Translate(row);
				var value = ((CheckBoxInput) input).Checked;
				if (!input.HasAttr("Name"))
					return;
				var name = input.Attr("Name").ToString();
				new UpdateController().Update(producer, name, value);
			};
		}
	}

	public class UpdateController
	{
		private ILog _log = LogManager.GetLogger(typeof(UpdateController));

		public void Update(object item, string name, object value)
		{
			if (item == null)
				return;

			if (String.IsNullOrEmpty(name))
				return;

			var property = item.GetType().GetProperty(name);
			var field = item.GetType().GetField(name);

			if (field == null && property == null)
				return;

			if (property != null)
				property.SetValue(item, value, null);

			if (field != null)
				field.SetValue(item, value);

			if (item is ProducerDto)
				Action(s => s.UpdateProducer((ProducerDto)item));
			else
				Action(s => s.UpdateAssortment((AssortmentDto)item));
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

	}
}
using System.Collections.Generic;
using System.ServiceModel;

namespace ProducerEditor.Service
{
	public class Offer
	{
		public string Product { get; set; }
		public string Producer { get; set; }
	}

	[ServiceContract]
	public interface ProducerService
	{
		[OperationContract]
		List<Offer> GetOffers(uint producerSynonymId);

		[OperationContract]
		void DeleteProducerSynonym(uint producerSynonymId);
	}
}
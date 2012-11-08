using System.Runtime.Serialization;

namespace ProducerEditor.Contract
{
	[DataContract(Namespace = "http://schemas.datacontract.org/2004/07/ProducerEditor.Service")]
	public class ProductAndProducer
	{
		[DataMember]
		public bool Selected { get; set; }

		[DataMember]
		public long ExistsInRls { get; set; }

		[DataMember]
		public uint ProducerId { get; set; }

		[DataMember]
		public string Producer { get; set; }

		[DataMember]
		public uint CatalogId { get; set; }

		[DataMember]
		public string Product { get; set; }

		[DataMember]
		public long OrdersCount { get; set; }

		[DataMember]
		public long OffersCount { get; set; }
	}
}
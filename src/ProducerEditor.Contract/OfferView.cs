using System.Runtime.Serialization;

namespace ProducerEditor.Contract
{
	[DataContract(Namespace = "http://schemas.datacontract.org/2004/07/ProducerEditor.Service")]
	public class OfferView
	{
		[DataMember]
		public string ProductSynonym { get; set; }

		[DataMember]
		public string ProducerSynonym { get; set; }

		[DataMember]
		public string Supplier { get; set; }
	}
}
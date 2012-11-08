using System.Runtime.Serialization;

namespace ProducerEditor.Contract
{
	[DataContract(Namespace = "http://schemas.datacontract.org/2004/07/ProducerEditor.Service")]
	public class SynonymReportItem
	{
		[DataMember]
		public uint Id { get; set; }

		[DataMember]
		public string User { get; set; }

		[DataMember]
		public string Price { get; set; }

		[DataMember]
		public string Region { get; set; }

		[DataMember]
		public string Synonym { get; set; }

		[DataMember]
		public string Producer { get; set; }

		[DataMember]
		public string Products { get; set; }

		[DataMember]
		public int IsSuspicious { get; set; }

		[DataMember]
		public uint SupplierId { get; set; }
	}
}
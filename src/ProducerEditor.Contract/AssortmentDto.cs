using System.Runtime.Serialization;

namespace ProducerEditor.Contract
{
	[DataContract(Namespace = "http://schemas.datacontract.org/2004/07/ProducerEditor.Service", Name = "Assortment")]
	public class AssortmentDto
	{
		[DataMember]
		public uint Id { get; set; }
		[DataMember]
		public string Product { get; set; }
		[DataMember]
		public string Producer { get; set; }
		[DataMember]
		public uint ProducerId { get; set; }
		[DataMember]
		public bool Checked { get; set; }
	}
}
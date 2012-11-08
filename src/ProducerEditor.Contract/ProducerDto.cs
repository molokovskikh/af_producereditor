using System.Runtime.Serialization;

namespace ProducerEditor.Contract
{
	[DataContract(Namespace = "http://schemas.datacontract.org/2004/07/ProducerEditor.Service" /*, Name = "Producer"*/)]
	public class ProducerDto
	{
		[DataMember]
		public virtual uint Id { get; set; }

		[DataMember]
		public virtual string Name { get; set; }

		[DataMember]
		public virtual bool Checked { get; set; }

		[DataMember]
		public virtual bool HasOffers { get; set; }
	}
}
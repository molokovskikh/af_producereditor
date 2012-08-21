using System.Runtime.Serialization;

namespace ProducerEditor.Contract
{
	[DataContract]
	public class ProducerEquivalentDto
	{
		[DataMember]
		public uint Id { get; set; }

		[DataMember]
		public string Name { get; set; }


		public ProducerEquivalentDto()
		{}

		public ProducerEquivalentDto(uint id, string name)
		{
			Id = id;
			Name = name;
		}
	}
}
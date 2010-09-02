using System.Collections.Generic;

namespace ProducerEditor.Contract
{
	public class ProducerOrEquivalentDto
	{
		public uint Id { get; set; }
		public string Name { get; set; }
	}

	public class ExcludeData
	{
		public List<ProducerOrEquivalentDto> Producers;
		public List<ProducerSynonymDto> Synonyms;
	}
}
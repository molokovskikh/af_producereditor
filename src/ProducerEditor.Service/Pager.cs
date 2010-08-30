using System.Collections.Generic;
using System.Runtime.Serialization;

namespace ProducerEditor.Service
{
	[DataContract]
	public class Pager<T>
	{
		[DataMember]
		public uint Page { get; set; }
		[DataMember]
		public uint TotalPages { get; set; }
		[DataMember]
		public IList<T> Content { get; set; }

		public Pager(uint page, uint total, IList<T> content)
		{
			Page = page;
			TotalPages = total / 100;
			if (TotalPages == 0)
				TotalPages = 1;
			else if (total % 100 != 0)
				TotalPages++;
			Content = content;
		}
	}
}
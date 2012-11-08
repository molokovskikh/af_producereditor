using System.Collections.Generic;
using NUnit.Framework;
using ProducerEditor.Contract;
using ProducerEditor.Views;

namespace ProducerEditor.Tests.View
{
	[TestFixture]
	public class ShowExcludesFixture
	{
		[Test]
		public void Load_view()
		{
			var view = new ShowExcludes(new Pager<ExcludeDto> { Content = new List<ExcludeDto>() });
		}

		[Test]
		public void Convert_content_to_list()
		{
			var view = new ShowExcludes(new Pager<ExcludeDto> { Content = new ExcludeDto[0] });
		}
	}
}
using System.Collections.Generic;
using NUnit.Framework;
using ProducerEditor.Models;
using ProducerEditor.Views;

namespace ProducerEditor.Tests.View
{
	[TestFixture]
	public class ShowExcludesFixture
	{
		[Test]
		public void Load_view()
		{
			var view = new ShowExcludes(new Pager<Exclude>{Content = new List<Exclude>()});
		}

		[Test]
		public void Convert_content_to_list()
		{
			var view = new ShowExcludes(new Pager<Exclude>{Content = new Exclude[0]});
		}
	}
}
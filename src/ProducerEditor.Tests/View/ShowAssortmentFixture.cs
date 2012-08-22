using System.Collections.Generic;
using NUnit.Framework;
using ProducerEditor.Contract;
using ProducerEditor.Infrastructure;
using ProducerEditor.Views;

namespace ProducerEditor.Tests.View
{
	[TestFixture]
	public class ShowAssortmentFixture
	{
		[Test]
		public void Show_assortment()
		{
			Pager<AssortmentDto> data = null;
			FactoryHolder.WithService(s => {
				data = s.ShowAssortment(0);
			});
			var view = new ShowAssortment(data);
		}
	}
}
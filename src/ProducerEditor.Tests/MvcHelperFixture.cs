using NUnit.Framework;
using ProducerEditor.Infrastructure;
using ProducerEditor.Views;

namespace ProducerEditor.Tests
{
	[TestFixture]
	public class MvcHelperFixture
	{
		[Test]
		public void Try_to_get_view_name()
		{
			Assert.That(MvcHelper.GetViewName(s => s.GetEquivalents(1)), Is.EqualTo("GetEquivalents"));
		}

		[Test]
		public void Try_to_get_view_type()
		{
			Assert.That(MvcHelper.GetViewType("RenameView"), Is.EqualTo(typeof(RenameView)));
		}
	}
}
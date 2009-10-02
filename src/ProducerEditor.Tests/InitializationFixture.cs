using NUnit.Framework;
using ProducerEditor.Service;

namespace ProducerEditor.Tests
{
	[TestFixture]
	public class InitializationFixture
	{
		[Test]
		public void Try_to_initialize()
		{
			Global.InitializeNHibernate();
		}
	}
}

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

		[Test]
		public void Try_to_resolve_service()
		{
			var container = Global.Setup();
			var service = container.Resolve<ProducerService>();
		}
	}
}

using NUnit.Framework;

namespace ProducerEditor.Tests
{
	[SetUpFixture]
	public class FixtureSetup
	{
		[SetUp]
		public void Setup()
		{
			Test.Support.Setup.Initialize();
		}
	}
}
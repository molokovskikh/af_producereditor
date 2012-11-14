using NHibernate;
using NUnit.Framework;
using ProducerEditor.Service;

namespace ProducerEditor.Tests
{
	public class BaseFixture
	{
		protected ISession session;
		protected ISessionFactory sessionFactory;

		[SetUp]
		public void Setup()
		{
			sessionFactory = FixtureSetup.sessionFactory;
			session = sessionFactory.OpenSession();
		}

		[TearDown]
		public void TearDown()
		{
			session.Close();
		}
	}
}
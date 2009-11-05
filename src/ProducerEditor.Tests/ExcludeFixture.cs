using log4net.Config;
using NHibernate;
using NUnit.Framework;
using ProducerEditor.Service;

namespace ProducerEditor.Tests
{
	[TestFixture]
	public class ExcludeFixture
	{
		private ISessionFactory sessionFactory;

		[SetUp]
		public void Setup()
		{
			XmlConfigurator.Configure();
			sessionFactory = Global.InitializeNHibernate();
		}

		[Test]
		public void Show_excludes()
		{
			using(var session = sessionFactory.OpenSession())
			{
				Exclude.Load(0, session);
				Exclude.TotalPages(session);
			}
		}
	}
}

using System.Linq;
using log4net.Config;
using NHibernate;
using NHibernate.Linq;
using NUnit.Framework;
using ProducerEditor.Service;

namespace ProducerEditor.Tests
{
	[TestFixture]
	public class ModelsFixture
	{
		private ISessionFactory sessionFactory;

		[SetUp]
		public void Setup()
		{
			XmlConfigurator.Configure();
			sessionFactory = Global.InitializeNHibernate();
		}

		[Test]
		public void Search_suspicious()
		{
			using(var session = sessionFactory.OpenSession())
			{
				session.Linq<SuspiciousProducerSynonym>().Where(s => s.Synonym.Id == 516849u).First();
			}
		}
	}
}

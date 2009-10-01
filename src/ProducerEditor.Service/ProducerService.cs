using System.Collections.Generic;
using System.ServiceModel;
using NHibernate;
using NHibernate.Transform;

namespace ProducerEditor.Service
{
	public class Offer
	{
		public string Product { get; set; }
		public string Producer { get; set; }
	}

	[ServiceContract]
	public class ProducerService
	{
		private readonly ISessionFactory _factory;

		public ProducerService(ISessionFactory sessionFactory)
		{
			_factory = sessionFactory;
		}

		[OperationContract]
		public IList<Offer> GetOffers(uint producerSynonymId)
		{
			using (var session = _factory.OpenSession())
			{
				return session.CreateSQLQuery(@"
select s.Synonym as Product, sfc.Synonym as Producer
from farm.core0 c
	join farm.Synonym s on s.SynonymCode = c.SynonymCode
	join farm.SynonymFirmCr sfc on sfc.SynonymFirmCrCode = c.SynonymFirmCrCode
where c.SynonymFirmCrCode = :producerSynonymId")
					.SetResultTransformer(Transformers.AliasToBean<Offer>())
					.SetParameter("producerSynonymId", producerSynonymId)
					.List<Offer>();
			}
		}
	}
}
﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework.Scopes;
using MySql.Data.MySqlClient;
using NHibernate.Transform;

namespace ProducerEditor
{
	public class Controller
	{
		private List<Producer> producers;

		public IList<Producer> GetAllProducers()
		{
			using (new SessionScope())
			{
				producers = (from producer in Producer.Queryable
				             where !producer.Hidden
				             orderby producer.Name
				             select producer).ToList();
				return producers;
			}
		}

		public void Update(Producer producer)
		{
			producer.Name = producer.Name.ToUpper();
			InMaster(producer.Update);
		}

		public void Join(Producer source, Producer target)
		{
			InMaster(() =>
			         	{
			         		var sessionHolder = ActiveRecordMediator.GetSessionFactoryHolder();

			         		var session = sessionHolder.CreateSession(typeof (Producer));
			         		var producerEquivalent = new ProducerEquivalent
			         		                         	{
			         		                         		Name = source.Name,
			         		                         		Producer = target,
			         		                         	};
			         		try
			         		{
			         			using (var transaction = session.BeginTransaction())
			         			{
			         				session.CreateSQLQuery(
			         					@"
update farm.SynonymFirmCr
set CodeFirmCr = :TargetId
where CodeFirmCr = :SourceId")
			         					.SetParameter("SourceId", source.Id)
			         					.SetParameter("TargetId", target.Id)
			         					.ExecuteUpdate();
			         				session.Save(producerEquivalent);
			         				session.Delete(source);
			         				transaction.Commit();
			         			}
			         			producers.Remove(source);
			         		}
			         		finally
			         		{
			         			sessionHolder.ReleaseSession(session);
			         		}
			         	});
		}

		public IList<SynonymView> Synonyms(Producer producer)
		{
			var sessionHolder = ActiveRecordMediator.GetSessionFactoryHolder();
			var session = sessionHolder.CreateSession(typeof(Producer));
			try
			{
				return session.CreateSQLQuery(@"
select sfc.Synonym,
cd.ShortName as Supplier,
cd.FirmSegment as Segment,
r.Region,
c.Id is not null as HaveOffers
from farm.SynonymFirmCr sfc
  join usersettings.PricesData pd on sfc.PriceCode = pd.PriceCode
    join usersettings.clientsdata cd on cd.FirmCode = pd.FirmCode
      join farm.Regions r on cd.RegionCode = r.RegionCode
  left join farm.Core0 c on c.SynonymFirmCrCode = sfc.SynonymFirmCrCode
where sfc.CodeFirmCr = :ProducerId
group by sfc.SynonymFirmCrCode")
					    .SetParameter("ProducerId", producer.Id)
						.SetResultTransformer(Transformers.AliasToBean(typeof(SynonymView)))
						.List<SynonymView>().ToList();
			}
			finally
			{
				sessionHolder.ReleaseSession(session);
			}
		}

		public List<Producer> SearchProducer(string text)
		{
			return producers.Where(p => p.Name.Contains((text ?? "").ToUpper())).ToList();
		}

		public void InMaster(Action action)
		{
			using (var connection = new MySqlConnection(ConfigurationManager.ConnectionStrings["Master"].ConnectionString))
			{
				connection.Open();
				using(new DifferentDatabaseScope(connection))
				{
					action();
				}
			}
		}
	}
}

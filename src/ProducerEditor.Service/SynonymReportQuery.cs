using System;
using System.Collections.Generic;
using System.Linq;
using Common.Models.Helpers;
using Common.NHibernate;
using NHibernate;
using NHibernate.Transform;
using ProducerEditor.Contract;

namespace ProducerEditor.Service
{
	public class SynonymReportQuery
	{
		public static IList<SynonymReportItem> Load(ISession session, DateTime begin, DateTime end)
		{
			return session.CreateSQLQuery(@"
SELECT sfc.SynonymFirmCrCode as Id,
       sfcl.OperatorName as User,
       s.Name as Price,
       r.Region,
       sfc.Synonym,
       pr.Name as Producer,
       ss.id is not null as IsSuspicious,
       (select group_concat(distinct concat(cn.Name, ' ', cf.Form) separator ', ')
        from farm.core0 c
          left join catalogs.products p on p.id = c.productid
            left join catalogs.catalog cc on cc.Id = p.CatalogId
              left join catalogs.catalognames cn on cn.id = cc.NameId
              left join catalogs.catalogforms cf on cf.id = cc.formid
        where c.codefirmcr = sfc.CodeFirmCr) as Products
FROM logs.SynonymFirmCrLogs sfcl
  join farm.SynonymFirmCr sfc on sfc.SynonymFirmCrCode = sfcl.SynonymFirmCrCode
  left join farm.SuspiciousSynonyms ss on ss.ProducerSynonymId = sfc.SynonymFirmCrCode
    join usersettings.pricesdata pd on pd.pricecode = sfc.pricecode
      join Customers.Suppliers s on pd.FirmCode = s.Id
        join farm.Regions r on r.RegionCode = s.HomeRegion
    join Catalogs.Producers pr on pr.Id = sfc.CodeFirmCr
where ((sfcl.Operation = 0 and sfcl.OperatorName != 'ProcessingSvc') or (sfcl.Operation = 1 and sfcl.OperatorName != 'ProducerEditor' and sfcl.OPeratorHost like 'OPT%')) and sfcl.LogTime between :begin and :end
group by sfc.SynonymFirmCrCode
order by sfc.Synonym;")
				.SetParameter("begin", begin)
				.SetParameter("end", end)
				.ToList<SynonymReportItem>();
		}

		public static IList<SynonymReportItem> Suspicious(ISession session)
		{
			return session.CreateSQLQuery(@"
SELECT sfc.SynonymFirmCrCode as Id,
       sfcl.OperatorName as User,
       s.Name as Price,
       r.Region,
       sfc.Synonym,
       pr.Name as Producer,
	   s.Id as SupplierId,
       ss.id is not null as IsSuspicious,
       (select group_concat(distinct concat(cn.Name, ' ', cf.Form) separator ', ')
        from farm.core0 c
          left join catalogs.products p on p.id = c.productid
            left join catalogs.catalog cc on cc.Id = p.CatalogId
              left join catalogs.catalognames cn on cn.id = cc.NameId
              left join catalogs.catalogforms cf on cf.id = cc.formid
        where c.codefirmcr = sfc.CodeFirmCr) as Products
FROM logs.SynonymFirmCrLogs sfcl
  join farm.SynonymFirmCr sfc on sfc.SynonymFirmCrCode = sfcl.SynonymFirmCrCode
  join farm.SuspiciousSynonyms ss on ss.ProducerSynonymId = sfc.SynonymFirmCrCode
    join usersettings.pricesdata pd on pd.pricecode = sfc.pricecode
      join Customers.Suppliers s on pd.FirmCode = s.Id
        join farm.Regions r on r.RegionCode = s.HomeRegion
    join Catalogs.Producers pr on pr.Id = sfc.CodeFirmCr
where ((sfcl.Operation = 0 and sfcl.OperatorName != 'ProcessingSvc') or (sfcl.Operation = 1 and sfcl.OperatorName != 'ProducerEditor' and sfcl.OPeratorHost like 'OPT%'))
group by sfc.SynonymFirmCrCode
order by sfc.Synonym;")
				.ToList<SynonymReportItem>();
		}
	}
}
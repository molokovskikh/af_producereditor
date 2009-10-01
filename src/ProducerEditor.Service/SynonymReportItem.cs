using System;
using System.Collections.Generic;
using System.Linq;
using NHibernate;
using NHibernate.Transform;

namespace ProducerEditor.Service
{
	public class SynonymReportItem
	{
		public string User { get; set; }
		public string Price { get; set; }
		public string Region { get; set; }
		public string Synonym { get; set; }
		public string Producer { get; set; }
		public string Products { get; set; }

		public static IList<SynonymReportItem> Load(ISession session, DateTime begin, DateTime end)
		{
			return session.CreateSQLQuery(@"
SELECT sfcl.OperatorName as User,
       cd.ShortName as Price,
       r.Region,
       sfc.Synonym,
       pr.Name as Producer,
       (select group_concat(distinct concat(cn.Name, ' ', cf.Form) separator ', ')
        from farm.core0 c
          left join catalogs.products p on p.id = c.productid
            left join catalogs.catalog cc on cc.Id = p.CatalogId
              left join catalogs.catalognames cn on cn.id = cc.NameId
              left join catalogs.catalogforms cf on cf.id = cc.formid
        where c.codefirmcr = sfc.CodeFirmCr) as Products
FROM logs.SynonymFirmCrLogs sfcl
  join farm.SynonymFirmCr sfc on sfc.SynonymFirmCrCode = sfcl.SynonymFirmCrCode
    join usersettings.pricesdata pd on pd.pricecode = sfc.pricecode
      join usersettings.clientsdata cd on pd.FirmCode = cd.FirmCode
        join farm.Regions r on r.RegionCode = cd.RegionCode
    join Catalogs.Producers pr on pr.Id = sfc.CodeFirmCr
where sfcl.Operation = 0 and sfcl.LogTime between :begin and :end
group by sfc.SynonymFirmCrCode;")
					.SetParameter("begin", begin)
					.SetParameter("end", end)
					.SetResultTransformer(Transformers.AliasToBean(typeof (SynonymReportItem)))
					.List<SynonymReportItem>().ToList();
		}
	}
}
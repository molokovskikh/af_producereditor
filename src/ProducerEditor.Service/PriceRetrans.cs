using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
//using Configurer.Models.Catalogs;
//using Configurer.Models.View;
//using Configurer.Services;
using NHibernate;
using RemotePriceProcessor;
using System.ServiceModel;
using Common.Tools;

namespace ProducerEditor.Service
{
	public class PriceRetrans
	{
		private readonly uint _priceItemId;
		private bool _retransed;

		public PriceRetrans(uint itemId)
		{
			_priceItemId = itemId;
		}

		public static IList<PriceRetrans> GetPricsForRetrans(ISession session, IEnumerable<object> items)
		{
			var producerSynonymIds = new List<string>();

			foreach (var entry in items) {
				if (entry is ProducerSynonym)
					producerSynonymIds.Add(((ProducerSynonym)entry).Id.ToString());
			}

			var command = session.Connection.CreateCommand();

			var totalCount = producerSynonymIds.Count;

			if (totalCount == 0)
				return Enumerable.Empty<PriceRetrans>().ToList();

			var coreFilter = "";
			BuildFilter(ref coreFilter, producerSynonymIds, "c.SynonymFirmCrCode");


			if (!String.IsNullOrEmpty(coreFilter))
				command.CommandText = String.Format(@"
select pi.Id as ItemId
from farm.Core0 C
	join usersettings.pricesdata pd on pd.pricecode = c.pricecode
		join Customers.Suppliers s on s.Id = pd.firmcode
	join usersettings.pricescosts pc on pc.PriceCode = pd.PriceCode
		join usersettings.priceitems pi on pi.Id = pc.PriceItemId
			join farm.formrules fr on pi.FormRuleId = fr.Id
				join farm.pricefmts pfmt on pfmt.Id = fr.PriceFormatId
where	s.Disabled = 0
		and pd.AgencyEnabled = 1
		and ({0})
group by pi.Id
", coreFilter);

			return Read(command);
		}

		private static List<PriceRetrans> Read(IDbCommand command)
		{
			var result = new List<PriceRetrans>();
			using (var dataReader = command.ExecuteReader()) {
				while (dataReader.Read()) {
					result.Add(new PriceRetrans((uint)dataReader["ItemId"]));
				}
			}
			return result;
		}

		private static void BuildFilter(ref string filterExpression, List<string> keys, string filterField)
		{
			if (keys.Count == 0)
				return;

			if (!String.IsNullOrEmpty(filterExpression))
				filterExpression += " or ";

			filterExpression += String.Format("{0} in ({1})", filterField, keys.Implode());
		}

		public PriceRetransLogEntity Retrans(PriceProcessorWcfHelper processor)
		{
			try {
				if (!_retransed) {
					_retransed = true;
#if !DEBUG
					if (!processor.RetransPrice(_priceItemId)) {
						_retransed = false;
						return null;
					}
#else
					Retranses.Add(_priceItemId);
#endif
					return new PriceRetransLogEntity(Environment.UserName, Environment.MachineName, _priceItemId);
				}
			}
			catch (FaultException) {
			}
			catch (Exception e) {
				var logger = log4net.LogManager.GetLogger(GetType());
				logger.Error("Ошибка в Редакторе производителей при перепроведении прайс-листа", e);
			}
			return null;
		}


		public static List<uint> Retranses { get; set; }

		public static void RetransAll(IEnumerable<PriceRetrans> retranses, ISession session)
		{
#if !DEBUG
			var priceProcessor = new PriceProcessorWcfHelper(Settings.WcfPriceProcessorUrl);
			foreach (var retrans in retranses) {
				var log = retrans.Retrans(priceProcessor);
				if (log != null)
					session.Save(log);
			}
			priceProcessor.Dispose();
#else
			Retranses = new List<uint>();
			foreach (var retrans in retranses) {
				retrans.Retrans(null);
			}
#endif
		}
	}
}
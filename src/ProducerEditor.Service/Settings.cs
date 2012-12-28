using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;

namespace ProducerEditor.Service
{
	public class Settings
	{
		public static string WcfPriceProcessorUrl
		{
			get { return ConfigurationManager.AppSettings["WCFPriceProcessorUrl"]; }
		}
	}
}
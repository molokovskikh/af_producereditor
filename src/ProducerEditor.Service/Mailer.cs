using System;
using System.Configuration;
using System.Net.Mail;

namespace ProducerEditor.Service
{
	public class Mailer
	{
		private readonly string _smtpServer;
		private readonly string _synonymDeleteNotificationMail;

		public Mailer()
		{
			_smtpServer = ConfigurationManager.AppSettings["SmtpServer"];
			_synonymDeleteNotificationMail = ConfigurationManager.AppSettings["SynonymDeleteNotificationMail"];
		}

		public void SynonymWasDeleted(ProducerSynonym synonym)
		{
			var smtp = new SmtpClient(_smtpServer);
			smtp.Send("tech@analit.net",
				_synonymDeleteNotificationMail,
				"Удален синоним производителя",
				String.Format(@"Синоним: {0}
Производитель: {1}
Поставщик: {2}
Регион: {3}
", synonym.Name,
					synonym.Producer.Name, synonym.Price.Supplier.ShortName, synonym.Price.Supplier.Region.Name));
		}

		public void SynonymWasDeleted(Synonym synonym)
		{
			var smtp = new SmtpClient(_smtpServer);
			smtp.Send("tech@analit.net",
				_synonymDeleteNotificationMail,
				"Удален синоним наименования",
				String.Format(@"Синоним: {0}
Поставщик: {1}
Регион: {2}
", synonym.Name, synonym.Price.Supplier.ShortName, synonym.Price.Supplier.Region.Name));			
		}
	}
}
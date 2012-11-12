using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net.Mail;

namespace ProducerEditor.Service
{
	public class Mailer
	{
		private readonly string _smtpServer;
		private readonly string _synonymDeleteNotificationMail;

		public List<MailMessage> Messages = new List<MailMessage>();

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
					synonym.Producer.Name, synonym.Price.Supplier.Name, synonym.Price.Supplier.Region.Name));
		}

		public void SynonymWasDeleted(Synonym synonym, string productName)
		{
			var smtp = new SmtpClient(_smtpServer);
			var message = new MailMessage("tech@analit.net",
				_synonymDeleteNotificationMail,
				"Удален синоним наименования",
				String.Format(@"Синоним: {0}
Продукт: {1}
Поставщик: {2}
Регион: {3}",
					synonym.Name,
					productName,
					synonym.Price.Supplier.Name,
					synonym.Price.Supplier.Region.Name));
#if DEBUG
			Messages.Add(message);
#endif
			smtp.Send(message);
		}
	}
}
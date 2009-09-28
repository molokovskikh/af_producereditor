using System;
using System.Configuration;
using System.Net.Mail;
using ProducerEditor.Models;

namespace ProducerEditor
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

		public void SynonymWasDeleted(SynonymView synonymView, Producer producer)
		{
			var smtp = new SmtpClient(_smtpServer);
			smtp.Send("tech@analit.net",
			          _synonymDeleteNotificationMail,
			          "Удален синоним производителя",
			          String.Format(@"Синоним: {0}
Производитель: {3}
Поставщик: {1}
Регион: {2}
", synonymView.Name, synonymView.Supplier, synonymView.Region, producer.Name));
		}
	}
}

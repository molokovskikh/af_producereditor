using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Web;
using NHibernate;
using ISession=NHibernate.ISession;

namespace ProducerEditor.Service.Helpers
{
	public class Executor
	{
		private readonly ISessionFactory _factory;

		public Executor(ISessionFactory factory)
		{
			_factory = factory;
		}

		public virtual void WithTransaction(Action<ISession> action)
		{
			using (var session = _factory.OpenSession())
			using (var transaction = session.BeginTransaction())
			{
				try
				{
					var host = Environment.MachineName;
					var user = Environment.UserName;
					if (OperationContext.Current != null)
					{
						host = ((RemoteEndpointMessageProperty)OperationContext.Current.IncomingMessageProperties[RemoteEndpointMessageProperty.Name]).Address;
						user = OperationContext.Current.IncomingMessageHeaders.GetHeader<string>("UserName", "");
					}
					session.CreateSQLQuery(@"
set @InUnser = :user
;
set @InHost = :host
;")
						.SetParameter("user", user)
						.SetParameter("host", host)
						.ExecuteUpdate();
					action(session);

					transaction.Commit();
				}
				catch
				{
					transaction.Rollback();
					throw;
				}
			}
		}
	}
}

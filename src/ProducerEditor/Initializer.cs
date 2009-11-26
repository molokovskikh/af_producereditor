using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework.Config;
using Subway.Dom;
using Subway.Dom.Styles;
using Environment=NHibernate.Cfg.Environment;

namespace ProducerEditor
{
	public class Initializer
	{
		public static void Initialize()
		{
			StylesHolder
				.Instance
				.RegisterStyleForDomElement<Cell>()
				.SetInherit(StyleElementType.CustomBackgroundDraw);

			StylesHolder
				.Instance
				.RegisterClass("CheckBoxColumn")
				.Set(StyleElementType.Width, 20)
				.Set(StyleElementType.IsFixed, true);

			StylesHolder
				.Instance
				.RegisterClass("CheckBoxColumn1")
				.Set(StyleElementType.Width, 60)
				.Set(StyleElementType.IsFixed, true);

			StylesHolder
				.Instance
				.RegisterClass("WithoutOffers")
				.Set(StyleElementType.CustomBackgroundDraw, Predefine.MixWith(Color.FromArgb(153, 231, 231, 200)));

			StylesHolder
				.Instance
				.RegisterClass("NotExistsInRls")
				.Set(StyleElementType.CustomBackgroundDraw, Predefine.MixWith(Color.FromArgb(153, 231, 216, 201)));

			StylesHolder
				.Instance
				.RegisterClass("Suspicious")
				.Set(StyleElementType.CustomBackgroundDraw, Predefine.MixWith(Color.FromArgb(153, 231, 216, 201)));

			StylesHolder
				.Instance
				.RegisterClass("BookmarkGlyph")
				.Set(StyleElementType.Width, 15)
				.Set(StyleElementType.Height, 15)
				.Set(StyleElementType.CustomBackgroundDraw, DrawBookmarkGlyph);

			InitializeAR();
		}

		public static void InitializeAR()
		{
			var config = new InPlaceConfigurationSource();
			config.Add(typeof (ActiveRecordBase),
				new Dictionary<string, string> {
					{Environment.Dialect, "NHibernate.Dialect.MySQLDialect"},
					{Environment.ConnectionDriver, "NHibernate.Driver.MySqlDataDriver"},
					{Environment.ConnectionProvider, "NHibernate.Connection.DriverConnectionProvider"},
					{Environment.ConnectionStringName, "Master"},
					{Environment.ProxyFactoryFactoryClass, "NHibernate.ByteCode.Castle.ProxyFactoryFactory, NHibernate.ByteCode.Castle" },
					{Environment.Hbm2ddlKeyWords, "none"}
				});
			ActiveRecordStarter.Initialize(new[] {Assembly.Load("ProducerEditor")}, config);
		}

		public static void DrawBookmarkGlyph(Graphics graphics, RectangleF area, IReadonlyStyle style)
		{
			graphics.FillRectangle(new SolidBrush(style.Get(StyleElementType.BackgroundColor)), area);
			graphics.DrawLine(Pens.Red, area.X, area.Y, area.X, area.Y + 13);
			graphics.FillRectangle(Brushes.Red, new RectangleF(area.X, area.Y, 8, 6));
		}
	}
}

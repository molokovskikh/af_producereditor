using System.Configuration;
using ProducerEditor.Infrastructure;

namespace ProducerEditor
{
	[SettingsProvider(typeof(UserDataSettingProvider))]
	public class Settings : ApplicationSettingsBase
	{
		private static readonly Settings defaultInstance = ((Settings) (Synchronized(new Settings())));

		public static Settings Default
		{
			get { return defaultInstance; }
		}

		[UserScopedSetting, DefaultSettingValue("0")]
		public uint BookmarkProducerId
		{
			get { return (uint)this["BookmarkProducerId"]; }
			set { this["BookmarkProducerId"] = value; }
		}
	}
}

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;

namespace ProducerEditor.Infrastructure
{
	public class UserDataSettingProvider : SettingsProvider
	{
		private readonly string _userData;

		public UserDataSettingProvider()
		{
			_userData = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)), "User Data");
		}

		public override void Initialize(string name, System.Collections.Specialized.NameValueCollection config)
		{
			if (String.IsNullOrEmpty(name))
				name = "UserDataSettingProvider";
			base.Initialize(name, config);
		}

		public override SettingsPropertyValueCollection GetPropertyValues(SettingsContext context, SettingsPropertyCollection collection)
		{
			var sectionName = GetSectionName(context);
			var userConfig = OpenConfigurationFile();
			var userSection = GetUserSettingsSection(sectionName, userConfig);
			var appSection = GetApplicationSettingsSection(sectionName);

			var propertyValues = new SettingsPropertyValueCollection();
			foreach (SettingsProperty property in collection) {
				var value = new SettingsPropertyValue(property);
				if (IsUser(property))
					ReadValue(property, userSection, value);
				else
					ReadValue(property, appSection, value);
				propertyValues.Add(value);
			}
			return propertyValues;
		}

		private bool IsUser(SettingsProperty property)
		{
			return property.Attributes[typeof(UserScopedSettingAttribute)] != null;
		}

		public override void SetPropertyValues(SettingsContext context, SettingsPropertyValueCollection collection)
		{
			var sectionName = GetSectionName(context);
			var dirtySettings = collection
				.OfType<SettingsPropertyValue>()
				.Where(i => i.IsDirty)
				.Select(i => {
					var element = new SettingElement(i.Name, i.Property.SerializeAs) {
						Value = {
							ValueXml = SerializeToXmlElement(i)
						}
					};
					i.IsDirty = false;
					return element;
				})
				.ToList();

			if (dirtySettings.Count > 0)
				WriteSettings(sectionName, dirtySettings);
		}

		private void ReadValue(SettingsProperty property, ClientSettingsSection userSection, SettingsPropertyValue value)
		{
			if (userSection != null) {
				var element = userSection.Settings.Get(property.Name);
				if (element != null)
					value.SerializedValue = element.Value.ValueXml.InnerXml;
			}
		}

		private void WriteSettings(string sectionName, IEnumerable<SettingElement> settingElements)
		{
			var userConfig = OpenConfigurationFile();
			var section = GetUserSettingsSection(sectionName, userConfig);

			if (section == null) {
				var userSettingsSectionGroup = (UserSettingsGroup)userConfig.SectionGroups["userSettings"];
				if (userSettingsSectionGroup == null) {
					userSettingsSectionGroup = new UserSettingsGroup();
					userConfig.SectionGroups.Add("userSettings", userSettingsSectionGroup);
				}
				section = new ClientSettingsSection();
				userSettingsSectionGroup.Sections.Add(sectionName, section);
			}

			var settings = section.Settings;
			foreach (var element in settingElements)
				settings.Add(element);

			userConfig.Save();
		}

		private ClientSettingsSection GetUserSettingsSection(string sectionName, Configuration userConfig)
		{
			var sectionConfigName = String.Format("userSettings/{0}", sectionName);
			return (ClientSettingsSection)userConfig.GetSection(sectionConfigName);
		}

		private ClientSettingsSection GetApplicationSettingsSection(string sectionName)
		{
			return (ClientSettingsSection)ConfigurationManager.GetSection(String.Format("applicationSettings/{0}", sectionName));
		}

		private Configuration OpenConfigurationFile()
		{
			var map = new ExeConfigurationFileMap {
				ExeConfigFilename = Path.Combine(_userData, "User.config")
			};
			return ConfigurationManager.OpenMappedExeConfiguration(map, ConfigurationUserLevel.None);
		}

		private string GetSectionName(SettingsContext context)
		{
			var group = (string)context["GroupName"];
			var key = (string)context["SettingsKey"];
			var name = group;
			if (!string.IsNullOrEmpty(key)) {
				name = String.Format(CultureInfo.InvariantCulture, "{0}.{1}", name, key);
			}
			return XmlConvert.EncodeLocalName(name);
		}

		private XmlNode SerializeToXmlElement(SettingsPropertyValue value)
		{
			var element = new XmlDocument().CreateElement("value");
			var serializedValue = value.SerializedValue as string;
			if (serializedValue == null) {
				serializedValue = string.Empty;
			}
			else if (value.Property.SerializeAs == SettingsSerializeAs.Binary && value.SerializedValue is byte[]) {
				serializedValue = Convert.ToBase64String((byte[])value.SerializedValue);
			}
			element.InnerText = serializedValue;
			return element;
		}

		public override string ApplicationName { get; set; }
	}
}
using System.Configuration;

namespace bsn.MFilesExtensionGateway.Configuration {
	[ConfigurationCollection(typeof(PrefixElement), AddItemName = PrefixElement.ElementName, CollectionType = ConfigurationElementCollectionType.BasicMap)]
	public class PrefixCollection: ConfigurationElementCollection<PrefixElement> {
		protected override object GetElementKey(PrefixElement element) {
			return element.Url;
		}

		public override ConfigurationElementCollectionType CollectionType => ConfigurationElementCollectionType.BasicMap;

		protected override string ElementName => PrefixElement.ElementName;
	}
}
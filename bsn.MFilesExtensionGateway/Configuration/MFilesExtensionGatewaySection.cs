using System;
using System.Configuration;

namespace bsn.MFilesExtensionGateway.Configuration {
	public class MFilesExtensionGatewaySection: ConfigurationSection {
		[ConfigurationProperty("prefix")]
		public PrefixElement Prefix => (PrefixElement)base["prefix"];

		/*[ConfigurationProperty("prefixes", IsDefaultCollection = true)]
		public PrefixCollection Prefixes => (PrefixCollection)base["prefixes"];*/
	}
}

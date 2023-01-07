using System;
using System.Configuration;

namespace bsn.MFilesExtensionGateway.Configuration {
	public class PrefixElement: ConfigurationElement {
		private int? pathLength;
		public const string ElementName = "prefix";

		public int PathLength {
			get {
				if (!pathLength.HasValue) {
					pathLength = !string.IsNullOrEmpty(Url) ? new Uri(Url.Replace('+', '_').Replace('*', '_')).AbsolutePath.Length-1 : 0;
				}
				return pathLength.Value;
			}
		}

		[ConfigurationProperty("url", IsKey = true, IsRequired = true)]
		public string Url {
			get => (string)this["url"];
			set {
				this["url"] = value;
				pathLength = null;
			}
		}

		[ConfigurationProperty("mFilesWeb", IsRequired = true)]
		public string MFilesWeb {
			get => (string)this["mFilesWeb"];
			set => this["mFilesWeb"] = value;
		}

		[ConfigurationProperty("vault", IsRequired = true)]
		public Guid Vault {
			get => (Guid)(this["vault"] ?? Guid.Empty);
			set => this["vault"] = value;
		}

		[ConfigurationProperty("username")]
		public string Username {
			get => (string)this["username"];
			set => this["username"] = value;
		}

		[ConfigurationProperty("password")]
		public string Password {
			get => (string)this["password"];
			set => this["password"] = value;
		}
	}
}
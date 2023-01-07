using System.Collections.Generic;
using System.Configuration;

namespace bsn.MFilesExtensionGateway.Configuration {
	public abstract class ConfigurationElementCollection<TConfigurationElement>: ConfigurationElementCollection, IList<TConfigurationElement> where TConfigurationElement: ConfigurationElement, new() {
		protected override ConfigurationElement CreateNewElement() {
			return new TConfigurationElement();
		}

		protected sealed override object GetElementKey(ConfigurationElement element) {
			return GetElementKey((TConfigurationElement)element);
		}

		protected abstract object GetElementKey(TConfigurationElement element);

		IEnumerator<TConfigurationElement> IEnumerable<TConfigurationElement>.GetEnumerator() {
			foreach (TConfigurationElement type in this) {
				yield return type;
			}
		}

		public void Add(TConfigurationElement configurationElement) {
			BaseAdd(configurationElement, true);
		}

		public void Clear() {
			BaseClear();
		}

		public bool Contains(TConfigurationElement configurationElement) {
			return !(IndexOf(configurationElement) < 0);
		}

		public void CopyTo(TConfigurationElement[] array, int arrayIndex) {
			base.CopyTo(array, arrayIndex);
		}

		public bool Remove(TConfigurationElement configurationElement) {
			BaseRemove(GetElementKey(configurationElement));
			return true;
		}

		bool ICollection<TConfigurationElement>.IsReadOnly => IsReadOnly();

		public int IndexOf(TConfigurationElement configurationElement) {
			return BaseIndexOf(configurationElement);
		}

		public void Insert(int index, TConfigurationElement configurationElement) {
			BaseAdd(index, configurationElement);
		}

		public void RemoveAt(int index) {
			BaseRemoveAt(index);
		}

		public TConfigurationElement this[int index] {
			get => (TConfigurationElement)BaseGet(index);
			set {
				if (BaseGet(index) != null) {
					BaseRemoveAt(index);
				}
				BaseAdd(index, value);
			}
		}
	}
}

using System.Configuration;
using System.Xml;

using JetBrains.Annotations;

using logPrint.Utils;

namespace logPrint.Config;

[UsedImplicitly]
internal class UsageElement : ConfigurationElement
{
	string _text;


	protected override void DeserializeElement(XmlReader reader, bool serializeCollectionKey)
	{
		_text = (reader.ReadElementContentAs(typeof(string), namespaceResolver: null) as string)
			.SafeTrim();
	}


	public static implicit operator string(UsageElement usage)
	{
		return usage._text;
	}


	public override string ToString()
	{
		return _text;
	}
}

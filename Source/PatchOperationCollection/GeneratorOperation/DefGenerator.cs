// Written in 2023 by bradson
// To the extent possible under law, the author(s) have dedicated all
// copyright and related and neighboring rights to this software to the
// public domain worldwide. This software is distributed without any
// warranty. You should have received a copy of the CC0 Public Domain
// Dedication along with this software. If not, see
// http://creativecommons.org/publicdomain/zero/1.0/.

namespace GeneratorOperation;

[UsedImplicitly]
public class DefGenerator : GeneratorBase
{
	public override string RootNodeName => "Defs";

	protected override bool ApplyResultingNode(XmlDocument xmlDocument, XmlNode matchNode, XmlNode valueChild)
	{
		var ownerDocument = matchNode.OwnerDocument;
		if (ownerDocument is null)
		{
			Log.Error($"'{matchNode}' lacks owner document at\n{matchNode.OuterXml}");
			return false;
		}

		var rootElement = ownerDocument.DocumentElement;
		if (rootElement is null)
		{
			Log.Error($"'{matchNode}' lacks root element at\n{matchNode.OuterXml}");
			return false;
		}

		rootElement.AppendChild(ownerDocument.ImportNode(valueChild, true));
		return true;
	}
}
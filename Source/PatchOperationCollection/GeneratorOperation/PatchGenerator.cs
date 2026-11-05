// Written in 2023 by bradson
// To the extent possible under law, the author(s) have dedicated all
// copyright and related and neighboring rights to this software to the
// public domain worldwide. This software is distributed without any
// warranty. You should have received a copy of the CC0 Public Domain
// Dedication along with this software. If not, see
// http://creativecommons.org/publicdomain/zero/1.0/.

namespace GeneratorOperation;

[UsedImplicitly]
public class PatchGenerator : GeneratorBase
{
	public override string RootNodeName => "Patch";

	protected override bool ApplyResultingNode(XmlDocument xmlDocument, XmlNode matchNode, XmlNode valueChild)
	{
		var patch = DirectXmlToObject.ObjectFromXml<PatchOperation>(valueChild, false);
		patch.sourceFile = sourceFile;
		var result = patch.Apply(xmlDocument);

		if (!result)
		{
			Log.Error($"{patch} at file {patch.sourceFile} {
				(patch is PatchOperationPathed pathed ? $"with xpath '{pathed.xpath}' " : "")}failed.");
		}

		return result;
	}
}
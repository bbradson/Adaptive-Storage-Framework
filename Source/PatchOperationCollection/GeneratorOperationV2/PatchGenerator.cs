// Written in 2024 by bradson
// To the extent possible under law, the author(s) have dedicated all
// copyright and related and neighboring rights to this software to the
// public domain worldwide. This software is distributed without any
// warranty. You should have received a copy of the CC0 Public Domain
// Dedication along with this software. If not, see
// http://creativecommons.org/publicdomain/zero/1.0/.

using System;
using System.Xml;
using JetBrains.Annotations;
using Verse;

namespace GeneratorOperationV2;

[UsedImplicitly]
public class PatchGenerator : GeneratorBase
{
	public override string RootNodeName => "Patch";

	protected override bool ApplyResultingNode(XmlDocument xmlDocument, XmlNode matchNode, XmlNode valueChild)
	{
		var patch = default(PatchOperation);
		var result = false;
		var faulted = false;
		try
		{
			patch = DirectXmlToObject.ObjectFromXml<PatchOperation>(valueChild, false);
			patch.sourceFile = sourceFile;
		}
		catch (Exception ex)
		{
			Log.Error($"Exception caught while trying to parse PatchOperation from xml for {this} at file '{
				sourceFile}' with xpath '{xpath}':\n{ex}");
			
			faulted = true;
		}

		try
		{
			if (patch != null)
				result = patch.Apply(xmlDocument);
		}
		catch (Exception ex)
		{
			Log.Error($"Exception caught while trying to apply PatchOperation from xml for {this} at file '{
				sourceFile}' with xpath '{xpath}':\n{ex}");
			
			faulted = true;
		}

		if (faulted)
			return false;

		if (!result)
		{
			Log.Error($"{patch} at file {patch!.sourceFile} {
				(patch is PatchOperationPathed pathed ? $"with xpath '{pathed.xpath}' " : "")}failed.");
		}

		return result;
	}
}
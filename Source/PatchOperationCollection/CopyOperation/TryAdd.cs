// Written in 2024 by bradson
// To the extent possible under law, the author(s) have dedicated all
// copyright and related and neighboring rights to this software to the
// public domain worldwide. This software is distributed without any
// warranty. You should have received a copy of the CC0 Public Domain
// Dedication along with this software. If not, see
// http://creativecommons.org/publicdomain/zero/1.0/.

using System.Xml;
using JetBrains.Annotations;
using Verse;

namespace CopyOperation;

[UsedImplicitly]
public class TryAdd : Add
{
	public override bool ProcessNodes(XmlNode[] valueNodes, XmlNode targetNode)
	{
		var targetChildNodes = targetNode.ChildNodes;
		var targetOwnerDocument = targetNode.OwnerDocument!;
		
		foreach (var valueNode in valueNodes)
		{
			if (targetChildNodes.ContainsNamed(valueNode.Name))
				continue;

			var newChild = targetOwnerDocument.ImportNode(valueNode, true);

			if (debug)
			{
				Log.Message($"{ToString()} DEBUG: Adding node\n{newChild.OuterXml}\ninto\n{
					targetNode.OuterXml}");
			}

			targetNode.AppendChild(newChild);
		}
		
		if (debug && valueNodes.Length <= 0)
		{
			Log.Message($"{ToString()} DEBUG: Returning false due to no matching values. TargetNode:\n{
				targetNode.OuterXml}");
		}

		return valueNodes.Length > 0;
	}
	
	public override bool ProcessText(string resolvedValue, XmlNode targetNode)
		=> !targetNode.InnerText.NullOrEmpty() || base.ProcessText(resolvedValue, targetNode);
}
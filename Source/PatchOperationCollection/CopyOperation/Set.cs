// Written in 2024 by bradson
// To the extent possible under law, the author(s) have dedicated all
// copyright and related and neighboring rights to this software to the
// public domain worldwide. This software is distributed without any
// warranty. You should have received a copy of the CC0 Public Domain
// Dedication along with this software. If not, see
// http://creativecommons.org/publicdomain/zero/1.0/.

using System;
using System.Linq;
using System.Xml;
using JetBrains.Annotations;
using Verse;

namespace CopyOperation;

[UsedImplicitly]
public class Set : PatchBase
{
	public override bool ProcessNodes(XmlNode[] valueNodes, XmlNode targetNode)
	{
		var ownerDocument = targetNode.OwnerDocument!;

		if (debug)
		{
			Log.Message($"{ToString()} DEBUG: Replacing node\n{targetNode.OuterXml}\nwith\n{
				string.Join("\n", valueNodes.Select(static valueNode => valueNode.OuterXml))}");
		}

		return valueNodes.ContainsOnlyText() && targetNode is XmlElement
			? ReplaceTextNodes(targetNode, valueNodes, ownerDocument)
			: ReplaceTargetNode(targetNode, valueNodes, ownerDocument);
	}

	public override bool ProcessText(string resolvedValue, XmlNode targetNode)
	{
		if (debug)
		{
			Log.Message($"{ToString()} DEBUG: Replacing text within node\n{targetNode.OuterXml}\nwith '{
				resolvedValue}'");
		}

		targetNode.InnerText = resolvedValue;
		return true;
	}

	protected static bool ReplaceTextNodes(XmlNode targetNode, XmlNode[] valueNodes, XmlDocument ownerDocument)
	{
		object[] targetChildNodes = [..targetNode.ChildNodes];
		var lastTextNode = (XmlText)Array.FindLast(targetChildNodes, static node => node is XmlText);

		for (var i = valueNodes.Length; --i >= 0;)
		{
			if (valueNodes[i] is XmlText newTextNode)
				targetNode.InsertAfter(ownerDocument.ImportNode(newTextNode, true), lastTextNode);
		}

		for (var i = targetChildNodes.Length; --i >= 0;)
		{
			if (targetChildNodes[i] is XmlText targetTextNode)
				targetNode.RemoveChild(targetTextNode);
		}

		return true;
	}

	protected static bool ReplaceTargetNode(XmlNode targetNode, XmlNode[] valueNodes, XmlDocument ownerDocument)
	{
		var parentNode = targetNode.ParentNode;
		if (parentNode is null)
			return false;

		for (var i = valueNodes.Length; --i >= 0;)
			parentNode.InsertAfter(ownerDocument.ImportNode(valueNodes[i], true), targetNode);

		parentNode.RemoveChild(targetNode);
		return true;
	}
}
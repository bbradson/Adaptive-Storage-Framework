// Written in 2024 by bradson
// To the extent possible under law, the author(s) have dedicated all
// copyright and related and neighboring rights to this software to the
// public domain worldwide. This software is distributed without any
// warranty. You should have received a copy of the CC0 Public Domain
// Dedication along with this software. If not, see
// http://creativecommons.org/publicdomain/zero/1.0/.

using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.XPath;
using JetBrains.Annotations;
using Verse;

namespace PatchOperationSet;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class Operation : PatchOperationPathed
{
	protected XmlContainer? value;

	protected bool debug;

	protected override bool ApplyWorker(XmlDocument xml)
	{
		if (debug)
			Log.Message(GetDebugInfo());

		var valueNode = value?.node;

		if (valueNode is null)
			return LogError(this, "is missing a value.");

		object[]? valueChildNodes = null;

		return SetNodeRecursively(xml, xml.CreateNavigator()!, xpath, targetNode =>
		{
			var ownerDocument = targetNode.OwnerDocument!;

			valueChildNodes ??= [..valueNode.ChildNodes];

			if (debug)
				Log.Message($"{ToString()} DEBUG: Replacing node\n{targetNode.OuterXml}\nwith\n{valueNode.OuterXml}");

			var valueIsText = valueChildNodes.ContainsOnlyText();
			if (valueIsText && targetNode is XmlElement)
				ReplaceTextNodes(targetNode, valueChildNodes, ownerDocument);
			else if (valueIsText && targetNode is XmlAttribute)
				ReplaceAttributeText(targetNode, valueChildNodes);
			else
				ReplaceTargetNode(targetNode, valueChildNodes, ownerDocument);
		});
	}

	private static void ReplaceAttributeText(XmlNode targetNode, object[] valueChildNodes)
		=> targetNode.InnerText = valueChildNodes.GetInnerText();

	protected static void ReplaceTextNodes(XmlNode targetNode, object[] valueChildNodes, XmlDocument ownerDocument)
	{
		object[] targetChildNodes = [..targetNode.ChildNodes];
		var lastTextNode = (XmlText)Array.FindLast(targetChildNodes, static node => node is XmlText);

		for (var i = valueChildNodes.Length; --i >= 0;)
		{
			if (valueChildNodes[i] is XmlText newTextNode)
				targetNode.InsertAfter(ownerDocument.ImportNode(newTextNode, true), lastTextNode);
		}

		for (var i = targetChildNodes.Length; --i >= 0;)
		{
			if (targetChildNodes[i] is XmlText targetTextNode)
				targetNode.RemoveChild(targetTextNode);
		}
	}

	protected static void ReplaceTargetNode(XmlNode targetNode, object[] valueChildNodes, XmlDocument ownerDocument)
	{
		var parentNode = targetNode.ParentNode!;
		for (var i = valueChildNodes.Length; --i >= 0;)
			parentNode.InsertAfter(ownerDocument.ImportNode((XmlNode)valueChildNodes[i], true), targetNode);

		// InsertAfter is much faster than InsertBefore, which ReplaceChild is implemented with

		parentNode.RemoveChild(targetNode);
	}

	protected bool SetNodeRecursively(XmlNode xml, XPathNavigator xpathNavigator, string xpathString, Action<XmlNode> action)
	{
		var xpathExpression = XPathExpression.Compile(xpathString);

		xpathString = xpathExpression.Expression.StripFunctionCall();

		if (xpathString.GetParentXpathNode() is { } parentXpath)
		{
			var childNodeName = xpathString.Substring(parentXpath.Length + 1);
			SetNodeRecursively(xml, xpathNavigator, parentXpath, node => AppendChildIfMissing(node, childNodeName));
		}
		else if (xpathString.Length > 0 && xpathString.IndexOfAny(['/', '[', ']']) < 0)
		{
			AppendChildIfMissing(xml, xpathString);
		}

		var matchNavigator = xpathNavigator.Select(xpathExpression);
		var matchingNodes = SimplePool<List<XmlNode>>.Get();
		matchingNodes.Clear();
		int matchCount;

		try
		{
			while (matchNavigator.MoveNext())
				matchingNodes.Add(((IHasXmlNode)matchNavigator.Current).GetNode());

			matchCount = matchingNodes.Count;
			for (var i = 0; i < matchCount; i++)
				action(matchingNodes[i]);
		}
		finally
		{
			matchingNodes.Clear();
			SimplePool<List<XmlNode>>.Return(matchingNodes);
		}

		if (matchCount > 0)
			return true;

		if (debug)
		{
			Log.Message($"{ToString()} DEBUG: Returning false due to no matching nodes. XpathExpression: {
				xpathExpression.Expression}");
		}

		return false;
	}

	private void AppendChildIfMissing(XmlNode node, string childNodeName)
	{
		if (node is not (XmlElement or XmlDocument or XmlEntity or XmlEntityReference or XmlDocumentFragment))
			return;

		var childNodes = node.ChildNodes;
		if (childNodeName == "text()")
		{
			if (!childNodes.ContainsTextNode())
				node.AppendChild(node.OwnerDocument!.CreateTextNode(string.Empty));

			return;
		}

		if (node.ChildNodes.ContainsNamed(childNodeName))
			return;

		var ownerDocument = node.OwnerDocument!;
		if (childNodeName.StartsWith("@"))
		{
			var attributes = node.Attributes;
			if (attributes is null)
				return;

			var newNode = ownerDocument.CreateAttribute(childNodeName.Substring(1, childNodeName.Length - 1));

			if (debug)
				Log.Message($"{ToString()} DEBUG: Adding attribute '{newNode.OuterXml}' to\n{node.OuterXml}");

			attributes.Append(newNode);
		}
		else
		{
			var newNode = ownerDocument.CreateElement(childNodeName);

			if (debug)
				Log.Message($"{ToString()} DEBUG: Adding node\n{newNode.OuterXml}\ninto\n{node.OuterXml}");

			node.AppendChild(newNode);
		}
	}

	protected virtual string GetDebugInfo()
		=> $"DEBUG: Running '{GetType().FullName}' from file '{sourceFile}' with xpath '{xpath}' and value '{
			value?.node?.OuterXml ?? "NULL"}'.";

	protected static bool LogError(PatchOperation patch, string message)
	{
		Log.Error($"'{patch}' in file '{patch.sourceFile}' {message}");
		return false;
	}

	public override string ToString() => $"{GetType().FullName}({xpath})";
}
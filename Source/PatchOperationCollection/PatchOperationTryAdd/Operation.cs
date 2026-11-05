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

namespace PatchOperationTryAdd;

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

		var valueChildNodes = default(object[]);

		return SetNodeRecursively(xml, xml.CreateNavigator()!, xpath, targetNode =>
		{
			if (valueNode is null)
				return;

			valueChildNodes ??= [..valueNode.ChildNodes];

			if (valueChildNodes.Length == 0
				|| Array.TrueForAll(valueChildNodes, static valueChildNode
					=> valueChildNode is XmlWhitespace or XmlComment))
			{
				return;
			}

			var ownerDocument = targetNode.OwnerDocument!;

			object[] targetChildNodes = [..targetNode.ChildNodes];

			foreach (XmlNode valueChildNode in valueChildNodes)
				TryAddNode(valueChildNode, targetChildNodes, ownerDocument, targetNode);
		});
	}

	protected void TryAddNode(XmlNode valueChildNode, object[] targetChildNodes, XmlDocument ownerDocument,
		XmlNode targetNode)
	{
		if (Array.Find(targetChildNodes,
				valueChildNode is XmlText
					? static targetChildNode => targetChildNode is XmlText targetTextNode
						&& !targetTextNode.Value.NullOrEmpty()
					: targetChildNode
						=> ((XmlNode)targetChildNode).Name == valueChildNode.Name)
			!= null)
		{
			return;
		}

		var newNode = ownerDocument.ImportNode(valueChildNode, true);

		if (debug)
			Log.Message($"{ToString()} DEBUG: Adding node\n{newNode.OuterXml}\ninto\n{targetNode.OuterXml}");

		if (newNode is XmlText
			&& Array.Find(targetChildNodes, static targetChildNode => targetChildNode is XmlText) is XmlText
				emptyTextNode)
		{
			targetNode.AppendChild(newNode);
			targetNode.RemoveChild(emptyTextNode);
		}
		else if (newNode is XmlText && targetNode is XmlAttribute)
		{
			targetNode.InnerText += newNode.InnerText;
		}
		else
		{
			targetNode.AppendChild(newNode);
		}
	}

	protected bool SetNodeRecursively(XmlNode xml, XPathNavigator xpathNavigator, string xpathString,
		Action<XmlNode> action)
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
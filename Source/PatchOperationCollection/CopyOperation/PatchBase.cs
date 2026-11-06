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

namespace CopyOperation;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public abstract class PatchBase : PatchOperationPathed
{
	public string?
		value,
		destination;

	public bool debug;

	protected override bool ApplyWorker(XmlDocument xml)
	{
		if (debug)
			Log.Message(GetDebugInfo());

		if (destination is null)
			return LogError(this, "is missing a destination.");

		if (xpath is { Length: > 0 })
		{
			var anySuccesses = false;
			object[] contextNodes = [..xml.SelectNodes(xpath)];
			foreach (XmlNode contextNode in contextNodes)
			{
				if (TryApplyPatchFor(contextNode, value, destination))
					anySuccesses = true;
			}

			return anySuccesses;
		}
		else
		{
			return TryApplyPatchFor(xml, value, destination);
		}
	}

	public bool TryApplyPatchFor(XmlNode xml, string? valuePath, string destinationPath)
	{
		var nodeNavigator = xml.CreateNavigator();
		var resolvedValue = valuePath != null ? nodeNavigator.Evaluate(valuePath) : (XmlNode[]) [xml];
		if (resolvedValue is null)
		{
			if (debug)
			{
				Log.Message($"{ToString()} DEBUG: Returning false due to no matching values. valuePath: '{
					valuePath}', xml:\n{xml.OuterXml}");
			}

			return false;
		}

		if (resolvedValue is XPathNodeIterator nodeIterator)
		{
			if (CreateArray(nodeIterator) is { Length: > 0 } nodeArray)
			{
				resolvedValue = nodeArray;
			}
			else
			{
				if (debug)
				{
					Log.Message($"{ToString()} DEBUG: Returning false due to no matching values. valuePath: '{
						valuePath}', xml:\n{xml.OuterXml}");
				}

				return false;
			}
		}

		return ApplyWorker(xml, nodeNavigator, resolvedValue, destinationPath);
	}

	protected virtual bool ApplyWorker(XmlNode xml, XPathNavigator nodeNavigator, object resolvedValue,
		string destinationPath)
	{
		var result = false;
		return SetNodeRecursively(xml, nodeNavigator, destinationPath, targetNode =>
			{
				if (resolvedValue is XmlNode[] valueNodes)
				{
					if (targetNode is XmlAttribute && valueNodes.ContainsOnlyText())
					{
						resolvedValue = valueNodes.GetInnerText();
					}
					else if (ProcessNodes(valueNodes, targetNode))
					{
						result = true;
						return;
					}
				}

				if (ProcessText(resolvedValue.ToString(), targetNode))
					result = true;
			})
			&& result;
	}

	public abstract bool ProcessNodes(XmlNode[] valueNodes, XmlNode targetNode);

	public abstract bool ProcessText(string resolvedValue, XmlNode targetNode);

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

		if (childNodes.ContainsNamed(childNodeName))
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

	protected static XmlNode[] CreateArray(XPathNodeIterator nodeIterator)
	{
		XmlNode[] result;
		var list = SimplePool<List<XmlNode>>.Get();
		list.Clear();
		try
		{
			while (nodeIterator.MoveNext())
				list.Add(((IHasXmlNode)nodeIterator.Current).GetNode());
		}
		finally
		{
			result = list.ToArray();
			list.Clear();
			SimplePool<List<XmlNode>>.Return(list);
		}

		return result;
	}

	protected virtual string GetDebugInfo()
		=> $"DEBUG: Running '{GetType().FullName}' from file '{sourceFile}' with xpath '{xpath}', destination '{
			destination}' and value '{value}'.";

	protected static bool LogError(PatchOperation patch, string message)
	{
		Log.Error($"'{patch}' in file '{patch.sourceFile}' {message}");
		return false;
	}

	public override string ToString()
		=> $"{GetType().FullName}(xpath: '{xpath}', destination: '{destination}',value: '{value}')";
}
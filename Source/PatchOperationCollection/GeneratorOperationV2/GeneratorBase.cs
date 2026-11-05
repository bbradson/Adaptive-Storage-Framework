// Written in 2024 by bradson
// To the extent possible under law, the author(s) have dedicated all
// copyright and related and neighboring rights to this software to the
// public domain worldwide. This software is distributed without any
// warranty. You should have received a copy of the CC0 Public Domain
// Dedication along with this software. If not, see
// http://creativecommons.org/publicdomain/zero/1.0/.

using System;
using System.Text;
using System.Xml;
using System.Xml.XPath;
using JetBrains.Annotations;
using Verse;

namespace GeneratorOperationV2;

[PublicAPI]
public abstract class GeneratorBase : PatchOperationPathed
{
	public string? value;

	public string Xpath => xpath;

	public abstract string RootNodeName { get; }

	protected override bool ApplyWorker(XmlDocument xml)
	{
		if (value is null)
			return LogError("had a null value");

		var result = true;
		object[] matchingNodes = [..xml.SelectNodes(xpath)!];
		
		foreach (XmlNode matchingNode in matchingNodes)
		{
			if (GetNodeFromString(SubstituteVariables(matchingNode, value)) is not { } valueNode
				|| !ApplyResultingContainer(xml, matchingNode, valueNode))
			{
				result = false;
			}
		}

		return result;
	}

	protected virtual bool ApplyResultingContainer(XmlDocument xmlDocument, XmlNode matchNode, XmlElement generatedNode)
	{
		var parentNode = matchNode.ParentNode;
		if (parentNode is null)
		{
			Log.Error($"'{matchNode}' lacks parent node at\n{matchNode.OuterXml}");
			return false;
		}

		if (generatedNode.Name != RootNodeName)
			return ApplyResultingNode(xmlDocument, matchNode, generatedNode);

		var valueChildNodes = generatedNode.ChildNodes;
		var result = true;
		var anyChildren = false;

		foreach (var valueChildNode in valueChildNodes)
		{
			if (valueChildNode is not XmlElement elementNode)
				continue;

			anyChildren = true;
			
			if (!ApplyResultingNode(xmlDocument, matchNode, elementNode))
				result = false;
		}

		return result & anyChildren;
	}

	protected abstract bool ApplyResultingNode(XmlDocument xmlDocument, XmlNode matchNode, XmlNode valueChild);

	protected bool LogError(string message)
	{
		Log.Error($"'{ToString()}' in '{sourceFile}' {message}");
		return false;
	}

	protected static string SubstituteVariables(XmlNode context, string xmlString)
	{
		var contextNavigator = context.CreateNavigator();
		var builder = new StringBuilder(xmlString);
		var closingBraceCount = 0;

		for (var i = builder.Length; --i >= 0;)
		{
			var character = builder[i];
			if (character == '}')
				closingBraceCount++;

			if (character != '{')
				continue;

			var variableStart = i;

			while (true)
			{
				if (++i >= builder.Length)
				{
					throw new XmlException($"Xpath patch value has opening brace at index {
						variableStart.ToStringCached()} but no matching closing brace");
				}
				else if (builder[i] == '}')
				{
					break;
				}
			}

			var variableEnd = i;
			var variableLength = variableEnd - variableStart - 1;
			var variable = builder.ToString(variableStart + 1, variableLength);

			var xpathResult = default(object);
			try
			{
				xpathResult = contextNavigator.Evaluate(variable);
				if (xpathResult is XPathNodeIterator iterator)
					xpathResult = iterator.MoveNext() ? ((IHasXmlNode)iterator.Current).GetNode() : null;
			}
			catch (Exception e)
			{
				Log.Error($"Error in xpath for variable {{{variable}}} in\n{builder}\n{e}");
			}

			if (xpathResult is null)
			{
				Log.Error($"No match found for xpath {{{variable}}} in\n{builder}");
				continue;
			}

			variableLength += 2;
			builder.Remove(variableStart, variableLength);
			closingBraceCount--;
			i -= variableLength;

			builder.Insert(variableStart,
				xpathResult is XmlNode node
					? variableStart > 0
					&& variableStart < builder.Length
					&& builder[variableStart] == '>'
					&& builder[variableStart - 1] == '<'
						? node.InnerText
						: node.OuterXml
					: xpathResult.ToString());
		}

		if (closingBraceCount != 0)
			throw new XmlException("Xpath patch value has more closing braces than opening braces");

		return builder.ToString();
	}

	protected static XmlElement? GetNodeFromString(string xmlString)
	{
		var doc = new XmlDocument();
		doc.LoadXml(xmlString);
		return doc.DocumentElement;
	}
}
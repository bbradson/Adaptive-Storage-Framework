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

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class Insert : PatchBase
{
	protected enum Order
	{
		Append,
		Prepend
	}

	protected Order order = Order.Prepend;
	
	public override bool ProcessNodes(XmlNode[] valueNodes, XmlNode targetNode)
	{
		var parentNode = targetNode.ParentNode;
		if (parentNode is null)
			return false;
		
		var targetOwnerDocument = parentNode.OwnerDocument!;
		var currentOrder = order;

		foreach (var valueNode in valueNodes)
		{
			var newChild = targetOwnerDocument.ImportNode(valueNode, true);

			if (debug)
			{
				Log.Message($"{ToString()} DEBUG: Inserting node\n{newChild.OuterXml}\ninto\n{
					parentNode.OuterXml}{(currentOrder == Order.Prepend ? "\nbefore\n" : "\nafter\n")}{
						targetNode.OuterXml}");
			}

			if (currentOrder == Order.Prepend)
			{
				parentNode.InsertBefore(newChild, targetNode);
				currentOrder = Order.Append;
			}
			else
			{
				parentNode.InsertAfter(newChild, targetNode);
			}
			
			targetNode = newChild;
		}

		if (valueNodes.Length > 0)
			return true;

		if (debug)
		{
			Log.Message($"{ToString()} DEBUG: Returning false due to no matching values. TargetNode:\n{
				targetNode.OuterXml}");
		}

		return false;
	}

	public override bool ProcessText(string resolvedValue, XmlNode targetNode)
	{
		if (debug)
		{
			Log.Message($"{ToString()} DEBUG: {(order == Order.Prepend ? "prepending" : "appending")} text '{
				resolvedValue}' into\n{targetNode.OuterXml}");
		}

		targetNode.InnerText = order == Order.Prepend
			? resolvedValue + targetNode.InnerText
			: targetNode.InnerText + resolvedValue;

		return true;
	}
}
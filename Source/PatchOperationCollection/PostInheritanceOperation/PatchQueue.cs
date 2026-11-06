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
using Verse;

namespace PostInheritanceOperation;

public static class PatchQueue
{
	private static readonly Queue<(PatchOperation Patch, string ModIdentifier)> _patches = [];
	
	public static void Enqueue(PatchOperation patch, string modIdentifier) => _patches.Enqueue((patch, modIdentifier));

	internal static void ApplyPatches(XmlDocument xmlDocument)
	{
		if (_patches.Count < 1)
			return;
		
		// Verse.XmlInheritance holds resolved nodes in a dictionary and does not apply the changes on the underlying
		// XmlDocument. Xpath patches require a proper document though, so this has to be a little intrusive here
		
		ApplyXmlInheritanceOnDocument(xmlDocument);

		while (_patches.TryDequeue(out var tuple))
			ApplyPatch(tuple.Patch, xmlDocument, tuple.ModIdentifier);
	}

	private static void ApplyXmlInheritanceOnDocument(XmlDocument xmlDocument)
	{
		var originElement = xmlDocument.DocumentElement!;

		// XmlNodeList is implemented as linked list where each element holds a reference to the first and next node,
		// but not to the previous. Count is not stored in a field, the property enumerates through the whole collection
		// instead. Indexing into the list, inserting before a node, removing a node and replacing a node are very slow.
		// Inserting after a node is very fast.
		
		object[] originNodes = [..originElement.ChildNodes];
		
		// array copy to allow modifying the collection without breaking the enumeration. This break happens silently,
		// without exception. The collection expression is implemented with Count for one array and then foreach to add
		// elements, which is several times faster than a for loop
		
		var resolvedNodes = XmlInheritance.resolvedNodes;

		foreach (var anyNode in originNodes)
		{
			if (anyNode is not XmlElement originalNode
				|| !resolvedNodes.TryGetValue(originalNode, out var inheritanceNode))
			{
				// ignore comments and other kinds of xml nodes. resolvedNodes only contains elements with ParentName
				// attribute
				
				continue;
			}

			ref var resolvedNode = ref inheritanceNode.resolvedXmlNode;
			
			if (originalNode == resolvedNode
				|| resolvedNode is null
				|| !resolvedNodes.TryAdd(resolvedNode, inheritanceNode))
			{
				// this should normally not happen, but could turn out required if another mod were to do the same
				// changes
				
				continue;
			}

			if (xmlDocument != resolvedNode.OwnerDocument)
			{
				// normally false, as xml gets merged into one document and resolved nodes seem to get created using the
				// same parent. May be needed for mods that customize xml inheritance in some way though
				
				resolvedNode = xmlDocument.ImportNode(resolvedNode, true);
			}

			originElement.InsertAfter(resolvedNode, originalNode);
			originElement.RemoveChild(originalNode);

			// RemoveChild is not fast, but doing it this way avoids an InsertBefore within the ReplaceChild
			// implementation, roughly cutting execution time in half
		}
		
		// stopwatch returns about 0.2 seconds for vanilla + 4 DLCs on my end for the whole method
	}

	private static void ApplyPatch(PatchOperation patch, XmlDocument resolvedXmlDocument, string modIdentifier)
	{
		try
		{
			patch.Apply(resolvedXmlDocument);
		}
		catch (Exception ex)
		{
			Log.Error($"Exception in patch.Apply() of '{patch}' from mod '{modIdentifier}':\n{ex}");
		}

		try
		{
			patch.Complete(modIdentifier);
		}
		catch (Exception ex)
		{
			Log.Error($"Exception in patch.Complete() of '{patch}' from mod '{modIdentifier}':\n{ex}");
		}
	}
}
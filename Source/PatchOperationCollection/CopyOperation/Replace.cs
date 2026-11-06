// Written in 2024 by bradson
// To the extent possible under law, the author(s) have dedicated all
// copyright and related and neighboring rights to this software to the
// public domain worldwide. This software is distributed without any
// warranty. You should have received a copy of the CC0 Public Domain
// Dedication along with this software. If not, see
// http://creativecommons.org/publicdomain/zero/1.0/.

using System.Xml;
using System.Xml.XPath;
using JetBrains.Annotations;
using Verse;

namespace CopyOperation;

[UsedImplicitly]
public class Replace : Set
{
	protected override bool ApplyWorker(XmlNode xml, XPathNavigator nodeNavigator, object resolvedValue,
		string destinationPath)
	{
		var targetNodes = CreateArray(nodeNavigator.Select(destinationPath));
		foreach (var targetNode in targetNodes)
		{
			if (resolvedValue is XmlNode[] valueNodes)
			{
				if (!ProcessNodes(valueNodes, targetNode))
					return false;
			}
			else
			{
				ProcessText(resolvedValue.ToString(), targetNode);
			}
		}

		if (targetNodes.Length > 0)
			return true;

		if (debug)
			Log.Message($"{ToString()} DEBUG: Returning false due to no matching targets for destinationPath");

		return false;
	}
}
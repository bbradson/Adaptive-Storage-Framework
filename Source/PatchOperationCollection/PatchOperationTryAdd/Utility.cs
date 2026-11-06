// Written in 2024 by bradson
// To the extent possible under law, the author(s) have dedicated all
// copyright and related and neighboring rights to this software to the
// public domain worldwide. This software is distributed without any
// warranty. You should have received a copy of the CC0 Public Domain
// Dedication along with this software. If not, see
// http://creativecommons.org/publicdomain/zero/1.0/.

using System.Xml;

namespace PatchOperationTryAdd;

public static class Utility
{
	public static bool ContainsNamed(this XmlNodeList xmlNodeList, string name)
	{
		foreach (var node in xmlNodeList)
		{
			if (((XmlNode)node).Name == name)
				return true;
		}

		return false;
	}

	public static bool ContainsTextNode(this XmlNodeList xmlNodeList)
	{
		foreach (var node in xmlNodeList)
		{
			if (node is XmlText)
				return true;
		}

		return false;
	}

	public static string StripFunctionCall(this string xpathString)
	{
		if (!xpathString.EndsWith(")"))
			return xpathString;

		var parentheses = 0;

		for (var i = xpathString.Length; --i >= 0;)
		{
			switch (xpathString[i])
			{
				case ')':
					parentheses++;
					break;
				case '(':
					parentheses--;
					break;
				case '/':
					if (parentheses != 0)
						continue;

					return xpathString.Substring(0, i);
			}
		}

		return xpathString;
	}

	public static string? GetParentXpathNode(this string xpathString)
	{
		var separatorIndex = xpathString.LastIndexOfAny(_xpathSeparators);
		return separatorIndex <= 0 || xpathString[separatorIndex] != '/' || xpathString[separatorIndex - 1] == '/'
			? null
			: xpathString.Substring(0, separatorIndex);
	}

	private static readonly char[] _xpathSeparators = ['/', ']'];
}
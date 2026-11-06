// Written in 2024 by bradson
// To the extent possible under law, the author(s) have dedicated all
// copyright and related and neighboring rights to this software to the
// public domain worldwide. This software is distributed without any
// warranty. You should have received a copy of the CC0 Public Domain
// Dedication along with this software. If not, see
// http://creativecommons.org/publicdomain/zero/1.0/.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace DefNameLink;

public record struct DefNameLink(string DefName, string TargetDef, Type DefType)
{
	public void Apply(GenerationPhase phase)
	{
		var defDatabaseType = typeof(DefDatabase<>).MakeGenericType(DefType);
		
		if (defDatabaseType.GetMethod(nameof(DefDatabase<Def>.GetNamedSilentFail))!
			.Invoke(null, [TargetDef]) is { } targetDef)
		{
			((IDictionary)defDatabaseType.GetField("defsByName", BindingFlags.Static | BindingFlags.NonPublic)!
				.GetValue(null))[DefName] = targetDef;

			typeof(OfType<>).MakeGenericType(DefType).GetMethod(nameof(OfType<Def>.Set))!
				.Invoke(null, [DefName, targetDef]);
		}
		else
		{
			Log.Error($"No valid target def found during {phase} called '{TargetDef}'");
		}
	}

	public static class OfType<T> where T : Def
	{
		private static readonly Dictionary<string, T> _allLinks = [];

		public static bool Any => _allLinks.Count > 0;

		public static T? TryGet(string defName) => _allLinks.TryGetValue(defName, out var value) ? value : null;

		public static void Set(string defName, T targetDef) => _allLinks[defName] = targetDef;
	}
}
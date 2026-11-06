// Written in 2024 by bradson
// To the extent possible under law, the author(s) have dedicated all
// copyright and related and neighboring rights to this software to the
// public domain worldwide. This software is distributed without any
// warranty. You should have received a copy of the CC0 Public Domain
// Dedication along with this software. If not, see
// http://creativecommons.org/publicdomain/zero/1.0/.

using System;
using System.Collections.Generic;

namespace SaveGameCompatibility;

public static class PatchDatabase
{
	private static readonly Dictionary<string, List<Operation>>
		_patchesByDef = [],
		_patchesByStuff = [],
		_patchesByType = [];

	public static bool Any => _patchesByDef.Count > 0 || _patchesByStuff.Count > 0;

	public static IReadOnlyList<Operation> PatchesForDef(string defName) => GetOrEmpty(_patchesByDef, defName);

	public static IReadOnlyList<Operation> PatchesForStuff(string defName) => GetOrEmpty(_patchesByStuff, defName);

	public static IReadOnlyList<Operation> PatchesForType(string typeName) => GetOrEmpty(_patchesByType, typeName);

	public static void Add(Operation patch)
	{
		if (patch.previousDefName is { Length: > 0 } defName)
			GetOrAdd(_patchesByDef, defName).Add(patch);

		if (patch.previousStuff is { Length: > 0 } stuffName)
			GetOrAdd(_patchesByStuff, stuffName).Add(patch);

		if (patch.previousClassName is { Length: > 0 } className)
			GetOrAdd(_patchesByType, className).Add(patch);
	}

	private static List<Operation> GetOrAdd(Dictionary<string, List<Operation>> dictionary, string key)
		=> dictionary.TryGetValue(key, out var value) ? value : dictionary[key] = [];

	private static IReadOnlyList<Operation> GetOrEmpty(Dictionary<string, List<Operation>> dictionary, string key)
		=> dictionary.TryGetValue(key, out var value) ? value : Array.Empty<Operation>();
}
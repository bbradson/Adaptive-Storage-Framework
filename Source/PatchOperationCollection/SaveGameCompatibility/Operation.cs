// Written in 2024 by bradson
// To the extent possible under law, the author(s) have dedicated all
// copyright and related and neighboring rights to this software to the
// public domain worldwide. This software is distributed without any
// warranty. You should have received a copy of the CC0 Public Domain
// Dedication along with this software. If not, see
// http://creativecommons.org/publicdomain/zero/1.0/.

using System;
using System.Xml;
using JetBrains.Annotations;
using Verse;

namespace SaveGameCompatibility;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class Operation : PatchOperation
{
	public string?
		previousDefName,
		previousStuff,
		previousClassName,
		newDefName,
		newStuff,
		newClassName;
	
	protected bool debug;

	[Unsaved]
	private ThingDef?
		_newDef,
		_newStuff;

	[Unsaved]
	private Type? _newType;

	public ThingDef? TryGetNewDef() => TryGetDefFor(ref _newDef, newDefName);

	public ThingDef? TryGetNewStuff() => TryGetDefFor(ref _newStuff, newStuff);

	public Type? TryGetNewType()
	{
		if (newClassName.NullOrEmpty())
			return null;

		var type = _newType ??= GenTypes.GetTypeInAnyAssembly(newClassName);
		if (type is null)
			Log.Error($"Failed to find Type named '{newClassName}' for '{ToString()}' from file '{sourceFile}'");

		return type;
	}

	private ThingDef? TryGetDefFor(ref ThingDef? field, string? name)
	{
		if (name.NullOrEmpty())
			return null;

		var def = field ??= DefDatabase<ThingDef>.GetNamedSilentFail(name);
		if (def is null)
			Log.Error($"Failed to find ThingDef named '{name}' for '{ToString()}' from file '{sourceFile}'");

		return def;
	}

	protected override bool ApplyWorker(XmlDocument xml)
	{
		if (debug)
			Log.Message(GetDebugInfo());

		if (previousDefName.NullOrEmpty() && previousStuff.NullOrEmpty())
			return LogError(this, "is missing a previousDefName or previousStuff value.");

		if (newDefName.NullOrEmpty() && newStuff.NullOrEmpty())
			return LogError(this, "is missing a newDefName or newStuff value.");

		PatchDatabase.Add(this);

		return true;
	}

	protected virtual string GetDebugInfo()
		=> $"DEBUG: Running '{this}' from file '{sourceFile}' with previousDefName '{
			previousDefName ?? "NULL"}', previousStuff '{previousStuff ?? "NULL"}', newDefName '{
				newDefName ?? "NULL"}' and newStuff '{newStuff ?? "NULL"}'.";

	protected static bool LogError(PatchOperation patch, string message)
	{
		Log.Error($"'{patch}' in file '{patch.sourceFile}' {message}");
		return false;
	}

	public override string ToString() => GetType().FullName!;
}
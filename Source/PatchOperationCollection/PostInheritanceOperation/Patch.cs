// Written in 2024 by bradson
// To the extent possible under law, the author(s) have dedicated all
// copyright and related and neighboring rights to this software to the
// public domain worldwide. This software is distributed without any
// warranty. You should have received a copy of the CC0 Public Domain
// Dedication along with this software. If not, see
// http://creativecommons.org/publicdomain/zero/1.0/.

using System.Linq;
using System.Xml;
using JetBrains.Annotations;
using Verse;

namespace PostInheritanceOperation;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class Patch : PatchOperation
{
	protected PatchOperation? operation;
	
	protected bool debug;

	protected override bool ApplyWorker(XmlDocument xml)
	{
		if (debug)
			Log.Message(GetDebugInfo());

		if (operation is null)
			return LogError(this, "is missing an operation value.");

		PatchQueue.Enqueue(operation, LoadedModManager.RunningModsListForReading.Find(mod
				=> (mod.Patches.Contains(this)))?.Name
			?? "UNKNOWN");

		return true;
	}

	protected virtual string GetDebugInfo()
		=> $"DEBUG: Running '{this}' from file '{sourceFile}' with operation '{operation}'.";

	protected static bool LogError(PatchOperation patch, string message)
	{
		Log.Error($"'{patch}' in file '{patch.sourceFile}' {message}");
		return false;
	}

	public override string ToString() => GetType().FullName!;
}
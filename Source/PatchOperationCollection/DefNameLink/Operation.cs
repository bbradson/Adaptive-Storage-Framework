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

namespace DefNameLink;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class Operation : PatchOperation
{
	protected string?
		defName,
		targetDef,
		defType = "Verse.ThingDef";

	protected GenerationPhase generationPhase = GenerationPhase.PreResolve;
	
	protected bool debug;

	protected override bool ApplyWorker(XmlDocument xml)
	{
		if (debug)
			Log.Message(GetDebugInfo());

		if (defName.NullOrEmpty())
			return LogError(this, "is missing a defName value.");

		if (targetDef.NullOrEmpty())
			return LogError(this, "is missing a targetDef value.");

		if (defType.NullOrEmpty())
			return LogError(this, "is missing a defType value.");

		if (GenTypes.GetTypeInAnyAssembly(defType) is not { } resolvedDefType)
		{
			return LogError(this, $"has an invalid defType with a value of '{
				defType}', which could not be mapped to any loaded type.");
		}

		DefDatabaseInsertion.EnqueueLink(generationPhase, defName!, targetDef!, resolvedDefType);

		return true;
	}

	private string GetDebugInfo()
		=> $"Running '{this}' from file '{sourceFile}' with defName '{defName}', targetDef '{
			targetDef}', defType '{defType}' and generationPhase '{generationPhase}'.";

	protected static bool LogError(PatchOperation patch, string message)
	{
		Log.Error($"'{patch}' in file '{patch.sourceFile}' {message}");
		return false;
	}
}
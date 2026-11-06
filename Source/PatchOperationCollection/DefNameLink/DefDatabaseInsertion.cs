// Written in 2024 by bradson
// To the extent possible under law, the author(s) have dedicated all
// copyright and related and neighboring rights to this software to the
// public domain worldwide. This software is distributed without any
// warranty. You should have received a copy of the CC0 Public Domain
// Dedication along with this software. If not, see
// http://creativecommons.org/publicdomain/zero/1.0/.

using System;
using System.Collections.Generic;
using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using Verse;

namespace DefNameLink;

[HarmonyPatch]
public static class DefDatabaseInsertion
{
	[HarmonyPrefix]
	[UsedImplicitly]
	[HarmonyPatch(typeof(DirectXmlCrossRefLoader), nameof(DirectXmlCrossRefLoader.ResolveAllWantedCrossReferences))]
	[HarmonyPriority(Priority.First)]
	public static void CrossRefLoaderPrefix(FailMode failReportMode)
		=> ApplyLinks(failReportMode switch
		{
			FailMode.Silent => _preDefGenerationLinks,
			FailMode.LogErrors => _preResolveLinks,
			_ => null
		}, failReportMode switch
		{
			FailMode.Silent => GenerationPhase.PreDefGeneration,
			FailMode.LogErrors => GenerationPhase.PreResolve,
			_ => default
		});

	[HarmonyPostfix]
	[UsedImplicitly]
	[HarmonyPatch(typeof(DefGenerator), nameof(DefGenerator.GenerateImpliedDefs_PostResolve))]
	[HarmonyPriority(Priority.Last)]
	public static void DefGeneratorPostfix() => ApplyLinks(_postResolveLinks, GenerationPhase.PostResolve);

	public static void ApplyLinks(Queue<DefNameLink>? links, GenerationPhase phase)
	{
		if (links is null)
			return;
		
		while (links.TryDequeue(out var link))
			link.Apply(phase);
	}

	public static void EnqueueLink(GenerationPhase phase, string defName, string targetDef, Type defType)
		=> (phase switch
		{
			GenerationPhase.PreDefGeneration => _preDefGenerationLinks,
			GenerationPhase.PreResolve => _preResolveLinks,
			GenerationPhase.PostResolve => _postResolveLinks,
			_ => throw new NotSupportedException(phase.ToString())
		}).Enqueue(new(defName, targetDef, defType));

	private static readonly Queue<DefNameLink>
		_preDefGenerationLinks = [],
		_preResolveLinks = [],
		_postResolveLinks = [];
}
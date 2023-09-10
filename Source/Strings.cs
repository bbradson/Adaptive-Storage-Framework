// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

using HarmonyLib;
// ReSharper disable UnassignedReadonlyField
// ReSharper disable InconsistentNaming

namespace AdaptiveStorage;

#pragma warning disable CS8618
public static class Strings
{
	public static readonly string

	#region Vanilla
		MassDescription = StatDefOf.Mass.description;
#endregion
	
	// static Strings() => AssignAllStringFields(typeof(Strings));

	public static string Stacks(int count, int totalSlots)
		=> TranslatedWithBackup.X_Stacks.Formatted(string.Concat(count.ToStringCached(), " / ",
				totalSlots.ToStringCached()));

	public static class Keys
	{
		public static readonly string

		#region Vanilla
			TabTransporterContents,
			ContainedItems,
			CommandNotForbiddenDesc,
			CommandForbiddenDesc,
			DesignatorForbidDesc,
			DesignatorUnforbidDesc,
		#endregion

		#region This mod
			ASF_Empty,
			ASF_XStacks,
			ASF_MaxNumStacks,
			ASF_MaxNumStacksDesc,
		#endregion

		#region Other mods
			LWM_DS_Empty,
			LWM_XStacks = "LWM.XStacks",
			LWM_DS_maxNumStacks,
			LWM_DS_maxNumStacksDesc;
	#endregion
		
		static Keys() => AssignAllStringFields(typeof(Keys));
	}
	
	public static class Translated
	{
		public static readonly string

		#region Vanilla
			LinkedStorageSettings,
			StoresThings,
			ContainedItems,
			RemoveSliderText,
			NumBuildings,
			ThingMadeOfStuffLabel,
			None,
			DaysUntilRotTip,
			ConfirmRemoveItemDialog,
		#endregion

		#region This mod
			ASF_MapFilled,
			ASF_SettingContentsTabSelection;
	#endregion
		
		static Translated() => AssignAllStringFields(typeof(Translated), Translator.TranslateSimple);
	}

	public static class TranslatedWithBackup
	{
		public static readonly string

		#region Vanilla
			CommandNotForbiddenDesc = Keys.CommandNotForbiddenDesc.TranslateWithBackup(Keys.DesignatorForbidDesc),
			CommandForbiddenDesc = Keys.CommandForbiddenDesc.TranslateWithBackup(Keys.DesignatorUnforbidDesc),
		#endregion

		#region Mods
			Empty = Keys.ASF_Empty.TranslateWithBackup(Keys.LWM_DS_Empty),
			X_Stacks = Keys.ASF_XStacks.TranslateWithBackup(Keys.LWM_XStacks),
			MaxNumStacks = Keys.ASF_MaxNumStacks.TranslateWithBackup(Keys.LWM_DS_maxNumStacks),
			MaxNumStacksDesc = Keys.ASF_MaxNumStacksDesc.TranslateWithBackup(Keys.LWM_DS_maxNumStacksDesc);
	#endregion
	}
	
	private static void AssignAllStringFields(Type type, Func<string, string>? func = null)
	{
		foreach (var field in type.GetFields(AccessTools.allDeclared))
		{
			if (field.FieldType != typeof(string))
				continue;

			var text = field.GetValue(null) as string ?? field.Name;
			field.SetValue(null, func != null ? func.Invoke(text) : text);
		}
	}
}
#pragma warning restore CS8618
// Copyright (c) 2024 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

namespace AdaptiveStorage.Utility;

/// <summary>
/// Scribe_Deep normally creates a new instance when loading data. Scribe on this class prevents that. Also includes a
/// try-catch block
/// </summary>
public sealed record Exposable(IExposable Parent) : IExposable
{
	public readonly IExposable Parent = Parent;

	public void ExposeData()
	{
		try
		{
			Parent.ExposeData();
		}
		catch (Exception ex)
		{
			Log.Error($"{ex}");
		}
	}

	public override string ToString() => Parent.ToString();

	public static void Scribe(IExposable iExposable, string label)
	{
		var wrappedExposable = new Exposable(iExposable);
		Scribe_Deep.Look(ref wrappedExposable, label, iExposable);
	}
}
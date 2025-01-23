// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

namespace AdaptiveStorage.Utility;

public static class WorkerClassMaker<T>
{
	public static T? MakeWorker<TDef>(Type? workerClass, TDef? def, params object[] arguments) where TDef : Def
	{
		var worker = default(T);
		if (workerClass != null)
		{
			try
			{
				worker = (T)Activator.CreateInstance(workerClass, arguments);
			}
			catch (Exception ex)
			{
				Log.Error($"Exception thrown while initializing workerClass for {typeof(TDef).Name} '{
					def?.defName}' from mod '{def?.modContentPack?.Name}':\n{ex}");
			}
		}

		if (worker is null)
		{
			Log.Error($"Missing worker in {typeof(TDef)} '{def?.defName}' from mod '{
				def?.modContentPack?.Name}'. Assigning default.");
		}

		return worker;
	}
}
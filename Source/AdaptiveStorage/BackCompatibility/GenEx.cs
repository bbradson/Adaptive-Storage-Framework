// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

#if V1_4
using System.Reflection;
#endif

namespace AdaptiveStorage.BackCompatibility;

public static class GenEx
{
	public static void MemberwiseShallowCopy(object from, object to)
	{
		
#if !V1_4
		Gen.MemberwiseShallowCopy(from, to);
#else
		var fields = from.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		foreach (var field in fields)
			field.SetValue(to, field.GetValue(from));
#endif
	}
}
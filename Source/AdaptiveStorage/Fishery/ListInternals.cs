using System.Collections.Generic;
using System.Reflection;

namespace AdaptiveStorage.Fishery;

internal static class ListInternals<T>
{
	internal static readonly FieldInfo Items = typeof(List<T>).GetField("_items",
		BindingFlags.NonPublic | BindingFlags.Instance)!;
	internal static readonly FieldInfo Version = typeof(List<T>).GetField("_version",
		BindingFlags.NonPublic | BindingFlags.Instance)!;
}

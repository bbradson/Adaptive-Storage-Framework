// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

using System.Reflection;
using HarmonyLib;

namespace AdaptiveStorage.ModCompatibility;

public static class ReflectionUtility
{
	public static MethodInfo DeclaredMethod(string typeColonName, Type[]? parameters = null, Type[]? generics = null)
		=> AccessTools.DeclaredMethod(typeColonName, parameters, generics)
			?? throw new MissingMethodException(typeColonName
				+ (!generics.NullOrEmpty() ? $"<{generics.ToStringSafeEnumerable()}>" : null)
				+ (!parameters.NullOrEmpty() ? $"({parameters.ToStringSafeEnumerable()})" : null));

	public static FieldInfo DeclaredField(string typeColonName)
		=> AccessTools.DeclaredField(typeColonName) ?? throw new MissingFieldException(typeColonName);

	public static FieldInfo DeclaredField(Type type, string name)
		=> AccessTools.DeclaredField(type, name) ?? throw new MissingFieldException($"{type.FullName}:{name}");

	public static Type TypeByName(string name)
		=> AccessTools.TypeByName(name) ?? throw new MissingMemberException(name);

	public static EventInfo DeclaredEvent(Type type, string name)
		=> type.GetEvent(name, AccessTools.allDeclared) ?? throw new MissingMemberException($"{type.FullName}:{name}");

	public static T CreateDelegate<T>(this MethodInfo methodInfo) where T : Delegate
		=> (T)methodInfo.CreateDelegate(typeof(T));

	public static T CreateDelegate<T>(this MethodInfo methodInfo, object target) where T : Delegate
		=> (T)methodInfo.CreateDelegate(typeof(T), target);

	public static T CreateDelegate<T>(string typeColonName) where T : Delegate
		=> (T)DeclaredMethod(typeColonName, GetDelegateParameters<T>()).CreateDelegate(typeof(T));

	public static T CreateDelegate<T>(string typeColonName, object target) where T : Delegate
		=> (T)DeclaredMethod(typeColonName, GetDelegateParameters<T>()).CreateDelegate(typeof(T), target);

	public static AccessTools.FieldRef<T> StaticFieldRefAccess<T>(string typeColonName)
		=> AccessTools.StaticFieldRefAccess<T>(DeclaredField(typeColonName));

	public static AccessTools.FieldRef<T> StaticFieldRefAccess<T>(Type type, string name)
		=> AccessTools.StaticFieldRefAccess<T>(DeclaredField(type, name));

	public static AccessTools.FieldRef<TField> StaticFieldRefAccess<TClass, TField>(string name)
		=> AccessTools.StaticFieldRefAccess<TField>(DeclaredField(typeof(TClass), name));

	public static AccessTools.FieldRef<TClass, TField> FieldRefAccessByFullName<TClass, TField>(string typeColonName)
		=> AccessTools.FieldRefAccess<TClass, TField>(DeclaredField(typeColonName));

	public static AccessTools.FieldRef<TClass, TField> FieldRefAccess<TClass, TField>(Type type, string name)
		=> AccessTools.FieldRefAccess<TClass, TField>(DeclaredField(type, name));

	public static AccessTools.FieldRef<TClass, TField> FieldRefAccess<TClass, TField>(string name)
		=> AccessTools.FieldRefAccess<TClass, TField>(DeclaredField(typeof(TClass), name));

	public static Type[] GetDelegateParameters<T>() where T : Delegate => GetDelegateParameters(typeof(T));

	public static Type[] GetDelegateParameters(Type delegateType)
		=> delegateType
			.GetMethod(nameof(Action.Invoke), BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)!
			.GetParameters()
			.ToTypes();

	public static Type[] ToTypes(this ParameterInfo[] parameterInfos)
	{
		var result = new Type[parameterInfos.Length];
		
		for (var i = 0; i < parameterInfos.Length; i++)
			result[i] = parameterInfos[i].ParameterType;

		return result;
	}
}
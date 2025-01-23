// Copyright (c) 2024 bradson
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Security;
using AdaptiveStorage.Fishery.Pools;
using AdaptiveStorage.Fishery.Utility.Diagnostics;

namespace AdaptiveStorage.Fishery.FunctionPointers;

public abstract partial class MethodFactory(MethodInfo[] methods, string namePrefix, bool byRef)
{
	protected readonly MethodInfo[] _methods = methods;

	protected readonly ConcurrentDictionary<Type, MethodInfo> _methodsByType = [];

	protected readonly ConcurrentBag<Delegate> _delegateStorage = [];

	protected readonly string _namePrefix = namePrefix;

	protected readonly bool _byRef = byRef;

	public MethodInfo GetGeneric(string name, params Type[] typeArguments)
		=> GetNamed(name).MakeGenericMethod(typeArguments);

	public MethodInfo GetNamed(string name)
	{
		foreach (var method in _methods)
		{
			if (method.Name == name)
				return method;
		}

		throw new ArgumentException($"No method found with name: {name}");
	}

	protected MethodInfo? GetSpecializedMethod(Type type)
	{
		if (_byRef)
			type = type.MakeByRefType();

		for (var i = 0; i < _methods.Length; i++)
		{
			var method = _methods[i];
			if (!method.IsGenericMethod && method.GetParameters()[0].ParameterType == type)
				return method;
		}

		return null;
	}

	public MethodInfo GetForType(Type type)
		=> _methodsByType.TryGetValue(type, out var value)
			? value
			: GetAndLog(type);

	[MethodImpl(MethodImplOptions.NoInlining)]
	private MethodInfo GetAndLog(Type type)
	{
		var result = GetOrMakeMethodForType(type);
#if DEBUG
		if (!type.Name.Contains("FullName"))
		{
			Log.Message($"{GetType().FullName + (_byRef ? " (ByRef)" : string.Empty)} assigning method '{
				result.FullDescription()}' for type '{type.FullName}'");
		}
#endif
		return _methodsByType[type] = result;
	}

	protected abstract MethodInfo GetOrMakeMethodForType(Type type);

	[SecuritySafeCritical]
	public IntPtr GetFunctionPointer(Type type) => GetForType(type).GetFunctionPointer();

	protected static MethodInfo[] GetMethodInfoArray(Type type, Func<MethodInfo, bool> predicate)
		=> type.GetMethods(Reflection.BindingFlags.PublicDeclaredStatic).Where(predicate).ToArray();

	internal static bool HasCompatibleSignature(MethodInfo method, Type returnType, ReadOnlySpan<Type> argumentTypes,
		out bool needsWrapper)
	{
		needsWrapper = false;
		if (method.ReturnType != returnType)
			return false;

		using var methodParameters = GetMethodParameterTypesIncludingInstance(method);
		if (methodParameters.Count != argumentTypes.Length)
			return false;

		for (var i = methodParameters.Count; i-- > 0;)
		{
			var methodParameter = methodParameters[i];
			var argument = argumentTypes[i];

			if (methodParameter == argument)
				continue;

			if (methodParameter.GetElementTypeIfByRef() != argument.GetElementTypeIfByRef())
				return false;

			needsWrapper = true;
		}

		return true;
	}

	protected static PooledList<Type> GetMethodParameterTypesIncludingInstance(MethodBase method)
	{
		var list = new PooledList<Type>();

		if (!method.IsStatic && method is not ConstructorInfo)
		{
			var declaringType = method.DeclaringType;
			list.Add(declaringType!.IsValueType ? declaringType.MakeByRefType() : declaringType);
		}

		list.AddRange(method.GetParameters(), static parameter => parameter.ParameterType);

		return list;
	}

	internal static PooledList<FishTranspiler.Container> GetMethodCallInstructions(MethodBase method,
		ReadOnlySpan<Type> argumentTypes)
	{
		var list = new PooledList<FishTranspiler.Container>();
		using var targetParameters = GetMethodParameterTypesIncludingInstance(method);

		Guard.IsLessThanOrEqualTo(argumentTypes.Length, targetParameters.Count);

		for (var i = 0; i < argumentTypes.Length; i++)
		{
			var sourceArgument = argumentTypes[i];
			var targetParameter = targetParameters[i];
			
			var byRef = sourceArgument.IsByRef;
			var needsValue = !targetParameter.IsByRef;
			
			list.Add(byRef | needsValue
				? FishTranspiler.Argument(i)
				: FishTranspiler.ArgumentAddress(i));

			if (byRef & needsValue)
				list.Add(FishTranspiler.LoadIndirectly(targetParameter));

			var targetElementType = targetParameter.GetElementTypeIfByRef();
			var sourceElementType = sourceArgument.GetElementTypeIfByRef();
			if (targetElementType.IsAssignableFrom(sourceElementType))
				continue;

			if (sourceElementType.IsAssignableFrom(targetElementType))
				list.Add(FishTranspiler.Cast(targetParameter));
			else
				ThrowForIncompatibleArgumentType(targetParameter, sourceArgument);
		}

		list.Add(FishTranspiler.Call(method));
		list.Add(FishTranspiler.Return);

		return list;
	}

	[DoesNotReturn]
	protected static void ThrowForIncompatibleArgumentType(Type targetParameter, Type sourceArgument)
		=> ThrowHelper.ThrowArgumentException($"Attempted to generate method call with parameter of type '{
			targetParameter.FullName}' using argument of type '{sourceArgument.FullName}'");
}
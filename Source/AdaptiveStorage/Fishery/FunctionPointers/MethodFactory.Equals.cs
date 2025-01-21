// Copyright (c) 2024 bradson
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using AdaptiveStorage.Fishery.Pools;
using AdaptiveStorage.Fishery.Utility.Diagnostics;
using HarmonyLib;

namespace AdaptiveStorage.Fishery.FunctionPointers;

public abstract partial class MethodFactory
{
	public new class Equals(Type type, string dynamicMethodNamePrefix, bool byRef)
		: MethodFactory(GetMethodInfoArraySafely(type), dynamicMethodNamePrefix, byRef)
	{
		protected override MethodInfo GetOrMakeMethodForType(Type type)
			=> GetSpecializedMethod(type)
				?? (type.IsAssignableTo(typeof(IEquatable<>), type)
						? GetGeneric(type.IsValueType
							? nameof(FunctionPointers.Equals.Methods.EquatableValueType)
							: nameof(FunctionPointers.Equals.Methods.EquatableReferenceType), type)
						: type.IsNullable(out var genericArgument)
						&& genericArgument.IsAssignableTo(typeof(IEquatable<>), genericArgument)
							? GetGeneric(nameof(FunctionPointers.Equals.Methods.Nullable), genericArgument)
							: type.IsEnum
								? GetGeneric(Type.GetTypeCode(Enum.GetUnderlyingType(type))
									switch
									{
										TypeCode.Int16
											or TypeCode.SByte
											or TypeCode.Byte
											or TypeCode.UInt16
											or TypeCode.Int32
											or TypeCode.UInt32 => nameof(FunctionPointers.Equals.Methods.Enum),
										TypeCode.Int64
											or TypeCode.UInt64 => nameof(FunctionPointers.Equals.Methods.LongEnum),
										_ => ThrowHelper.ThrowNotSupportedException<string>()
									}, type)
								: type.IsValueType
									? TryGetMethodByName(type) ?? CompileEqualsMethod(type)
									: GetGeneric(nameof(FunctionPointers.Equals.Methods.Object), type));

		protected MethodInfo? TryGetMethodByName(Type type)
		{
			var methods = type.GetMethods(AccessTools.allDeclared);
			if (_byRef)
				type = type.MakeByRefType();

			using var argumentTypes = new PooledList<Type>();
			argumentTypes.Fill(type, 2);
			var argumentSpan = argumentTypes.ReadOnlySpan;

			for (var i = 0; i < methods.Length; i++)
			{
				var method = methods[i];
				if (method.Name != nameof(object.Equals) || method.IsStatic)
					continue;

				if (HasCompatibleSignature(method, typeof(bool), argumentSpan, out var needsWrapper))
					return needsWrapper ? CompileWrapperMethod(type, method) : method;
			}

			MethodInfo? methodThatNeedsWrapper = null;

			for (var i = 0; i < methods.Length; i++)
			{
				var method = methods[i];
				if (method.Name != "op_Equality"
					|| !method.IsStatic
					|| !HasCompatibleSignature(method, typeof(bool), argumentSpan, out var needsWrapper))
				{
					continue;
				}

				if (!needsWrapper)
					return method;
				else
					methodThatNeedsWrapper = method;
			}

			return methodThatNeedsWrapper != null ? CompileWrapperMethod(type, methodThatNeedsWrapper) : null;
		}

		protected MethodInfo CompileWrapperMethod(Type type, MethodInfo method)
		{
#if DEBUG
			Log.Message($"Method {method.FullDescription()} determined to be in need of wrapper");
#endif
			try
			{
				Type[] parameterTypes = [type, type];
				var dm = new DynamicMethod($"{_namePrefix}{type.Name}", typeof(bool), parameterTypes,
					type.GetElementTypeIfByRef(), true);

				using var pooledInstructions = GetMethodCallInstructions(method, parameterTypes);
				dm.GetILGenerator().EmitRange(pooledInstructions);

				return CompileAndInitialize(type, dm);
			}
			catch (Exception ex)
			{
				return LogErrorAndReturnFallback(type, ex);
			}
		}

		protected MethodInfo CompileAndInitialize(Type type, DynamicMethod dm)
		{
#if DEBUG
			Log.Message($"Compiling {dm.FullDescription()}");
#endif

			var func = dm.CreateDelegate(_byRef
				? typeof(ByRefFunc<,,>)
				: typeof(Func<,,>).MakeGenericType(type, type, typeof(bool)));

			_delegateStorage.Add(func);
			func.DynamicInvoke(null, null);
			return func.Method;
		}

		protected MethodInfo CompileEqualsMethod(Type type)
		{
			try
			{
				var dm = new DynamicMethod($"{_namePrefix}{type.Name}", typeof(bool),
					_byRef ? [type.MakeByRefType(), type.MakeByRefType()] : [type, type], type, true);
				var il = dm.GetILGenerator();

				var fields = type.GetFields(Reflection.BindingFlags.AnyInstance);

				var needsJump = fields.Length > 1;

				var falseLabel = needsJump ? il.DefineLabel() : default;
				var trueLabel = needsJump ? il.DefineLabel() : default;

				for (var i = 0; i < fields.Length; i++)
				{
					var field = fields[i];

					for (var j = 0; j < 2; j++)
					{
						il.Emit(FishTranspiler.Argument(j));
						il.Emit(_byRef ? FishTranspiler.FieldAddress(field) : FishTranspiler.Field(field));
					}

					il.Emit(FishTranspiler.Call(GetForType(field.FieldType)));

					if (needsJump)
					{
						il.Emit(i == fields.Length - 1
							? FishTranspiler.GoTo_Short(trueLabel)
							: FishTranspiler.IfFalse_Short(falseLabel));
					}
				}

				if (needsJump)
				{
					il.MarkLabel(falseLabel);
					il.Emit(FishTranspiler.Constant(0));

					il.MarkLabel(trueLabel);
				}

				il.Emit(FishTranspiler.Return);

				return CompileAndInitialize(type, dm);
			}
			catch (Exception e)
			{
				return LogErrorAndReturnFallback(type, e);
			}
		}

		protected MethodInfo LogErrorAndReturnFallback(Type type, Exception e)
		{
			Log.Error($"Failed compiling specialized Equals method for {
				type.FullDescription()}. Returning fallback instead.\n{e}{new StackTrace()}");

			return GetGeneric(nameof(FunctionPointers.Equals.Methods.ValueType), type);
		}

		protected static MethodInfo[] GetMethodInfoArraySafely(Type type)
		{
			try
			{
				return GetMethodInfoArray(type, static m
					=> m.ReturnType == typeof(bool)
					&& m.GetParameters().Length == 2);
			}
			catch (Exception e)
			{
				Log.Error($"Exception while preparing method info array for type {type}:\n{e}");
				throw;
			}
		}

		protected delegate TResult ByRefFunc<T1, T2, out TResult>(ref T1 arg1, ref T2 arg2);
	}
}
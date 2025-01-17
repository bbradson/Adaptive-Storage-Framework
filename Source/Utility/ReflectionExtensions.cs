// Copyright (c) 2024 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

using System.Linq;
using System.Reflection;
using AdaptiveStorage.Fishery.Utility.Diagnostics;
using AdaptiveStorage.ModCompatibility;

namespace AdaptiveStorage.Utility;

public static class ReflectionExtensions
{
	public static IEnumerable<Type> GetSubclassesWithMethodOverride(this Type type, string methodName)
	{
		Guard.IsNotNull(type);
		Guard.IsNotNull(methodName);
		Guard.IsNotEmpty(methodName);

		return type.AllSubclassesNonAbstract().Where((type, methodName),
			static (c, type) => type.OverridesMethodFrom(c.type, c.methodName));
	}
	
	public static IEnumerable<Type> GetSubclassesWithAnyMethodOverride(this Type type, params string[] methodNames)
	{
		Guard.IsNotNull(type);
		Guard.IsNotNull(methodNames);
		Guard.IsNotEmpty(methodNames);

		return type.AllSubclassesNonAbstract().Where((type, methodNames),
			static (c, type) => type.HasAnyMethodOverridden(c.type, c.methodNames));
	}

	public static IEnumerable<Type> GetSubclassesWithNoMethodOverride(this Type type, params string[] methodNames)
	{
		Guard.IsNotNull(type);
		Guard.IsNotNull(methodNames);
		Guard.IsNotEmpty(methodNames);

		return type.AllSubclassesNonAbstract().Where((type, methodNames),
			static (c, type) => !type.HasAnyMethodOverridden(c.type, c.methodNames));
	}

	public static IEnumerable<Type> GetSubclassesWithAllMethodOverrides(this Type type, params string[] methodNames)
	{
		Guard.IsNotNull(type);
		Guard.IsNotNull(methodNames);
		Guard.IsNotEmpty(methodNames);

		return type.AllSubclassesNonAbstract().Where((type, methodNames),
			static (c, type) => type.HasAllMethodsOverridden(c.type, c.methodNames));
	}

	public static IEnumerable<Type> GetSubclassesWithNoMethodOverrideAndSelf(this Type type,
		params string[] methodNames)
	{
		Guard.IsNotNull(type);
		Guard.IsNotNull(methodNames);
		Guard.IsNotEmpty(methodNames);

		return type.GetSubclassesWithNoMethodOverride(methodNames).Prepend(type);
	}

	// public static bool OverridesMethodFrom(this Type type, Type declaringType, string methodName)
	// 	=> type.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
	// 			is { } method
	// 		&& method.DeclaringType != declaringType;
	
	// method.DeclaringType is meant to return the declaring type of inherited method overrides, and documented that
	// way, and does do that in coreCLR, but does not behave that way in unity. There it incorrectly returns the virtual
	// base definition instead, meaning for example Verse.Graphic for Verse.Graphic_MealVariations instead of
	// Verse.Graphic_StackCount as it should. So the workaround below becomes necessary to get past this unity mess
	// covers edge cases like new virtual and ambiguous matches too

	public static bool OverridesMethodFrom(this Type type, Type declaringType, string methodName)
	{
		Guard.IsNotNull(type);
		Guard.IsNotNull(declaringType);
		Guard.IsNotNull(methodName);
		
		var baseDefinition = default(MethodInfo);
		var parameterTypes = default(Type[]);
		
		do
		{
			if (type == declaringType || type == typeof(object))
				return false;

			if (baseDefinition is null)
			{
				baseDefinition = declaringType.GetMethod(methodName,
					BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.GetBaseDefinition();
				parameterTypes = baseDefinition.GetParameters().ToTypes();
			}

			var declaredMethod = type.GetMethod(methodName,
				BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
				null, parameterTypes!, []);
			
			if (declaredMethod != null
				&& declaredMethod.Attributes is var methodAttributes
				&& (methodAttributes & MethodAttributes.Virtual) != 0
				&& (methodAttributes & (MethodAttributes.NewSlot | MethodAttributes.Abstract)) == 0
				// override is implemented as MethodAttributes.Virtual,
				// new virtual uses MethodAttributes.NewSlot | MethodAttributes.Virtual in IL
				&& declaredMethod.GetBaseDefinition() == baseDefinition)
			{
				return true;
			}
		}
		while ((type = type.BaseType!) != null);

		return false;
	}

	public static bool HasAnyMethodOverridden(this Type type, Type declaringType, params string[] methodNames)
	{
		Guard.IsNotNull(type);
		Guard.IsNotNull(methodNames);
		Guard.IsNotEmpty(methodNames);

		for (var i = 0; i < methodNames.Length; i++)
		{
			if (type.OverridesMethodFrom(declaringType, methodNames[i]))
				return true;
		}

		return false;
	}

	public static bool HasAllMethodsOverridden(this Type type, Type declaringType, params string[] methodNames)
	{
		Guard.IsNotNull(type);
		Guard.IsNotNull(methodNames);
		Guard.IsNotEmpty(methodNames);

		for (var i = 0; i < methodNames.Length; i++)
		{
			if (!type.OverridesMethodFrom(declaringType, methodNames[i]))
				return false;
		}

		return true;
	}

	public static IEnumerable<Type> WithGraphicSubclassesNotOverridingPrintOrDraw(this Type graphicType)
		=> graphicType.GetSubclassesWithNoMethodOverrideAndSelf(nameof(Graphic.Print),
			nameof(Graphic.DrawWorker));

	public static IEnumerable<Type> WithThingSubclassesNotOverridingPrintOrDraw(this Type thingType)
		=> thingType.GetSubclassesWithNoMethodOverrideAndSelf(nameof(Thing.Print),
#if !V1_4
			nameof(Thing.DynamicDrawPhaseAt),
#endif
			"DrawAt");
}
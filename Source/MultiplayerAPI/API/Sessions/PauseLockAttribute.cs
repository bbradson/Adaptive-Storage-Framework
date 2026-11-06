using System;
using Verse;

namespace Multiplayer.API;

/// <summary>
/// An attribute that marks a method for pause lock checking It needs a <see cref="bool"/> return type and a single <see cref="Map"/> parameter.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class PauseLockAttribute : Attribute
{ }
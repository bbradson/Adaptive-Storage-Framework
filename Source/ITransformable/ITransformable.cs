// Copyright (c) 2025 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

global using System;
global using UnityEngine;
global using Verse;
global using JetBrains.Annotations;

namespace ITransformable;

[PublicAPI]
public interface ITransformable
{
	public void DrawAt(in TransformData transformData);

	public void PrintAt(SectionLayer layer, in TransformData transformData);
}

[PublicAPI]
public interface ITransformable<in T>
{
	public void DrawAt(T context, in TransformData transformData);

	public void PrintAt(SectionLayer layer, T context, in TransformData transformData);
}
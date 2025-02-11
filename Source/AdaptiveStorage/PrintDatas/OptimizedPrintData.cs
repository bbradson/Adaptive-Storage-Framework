// Copyright (c) 2024 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

using System.Diagnostics;
using System.Linq;
using UnityEngine.Rendering;

namespace AdaptiveStorage.PrintDatas;

public class OptimizedPrintData : PrintData
{
	protected Material _material = null!;
	protected Mesh _mesh = null!;
	protected Matrix4x4 _matrix;
	protected float _graphicRotation = float.NaN;
	protected Vector3 _graphicDrawOffset;
	protected bool _flipUv;
	protected Vector2[]? _uvs;
	protected Color32[]? _colors;

	public Vector3 GraphicDrawOffset
	{
		get => _graphicDrawOffset;
		set
		{
			if (value == _graphicDrawOffset)
				return;

			_graphicDrawOffset = value;
			UpdateMatrix();
		}
	}

	public Vector3 CombinedDrawOffset
	{
		get => DrawOffset + GraphicDrawOffset;
		set
		{
			if (value != CombinedDrawOffset)
				DrawOffset = value - GraphicDrawOffset;
		}
	}

	public float GraphicRotation
	{
		get => _graphicRotation;
		set
		{
			if (Mathf.Approximately(value, _graphicRotation))
				return;

			_graphicRotation = value;
			UpdateMatrix();
		}
	}

	public float CombinedRotation
	{
		get => GraphicRotation + ExtraRotation;
		set
		{
			if (!Mathf.Approximately(value, CombinedRotation))
				ExtraRotation = value - GraphicRotation;
		}
	}

	public bool FlipUv
	{
		get => _flipUv;
		set
		{
			if (value == _flipUv)
				return;

			if (Graphic!.data is { } graphicData)
				GraphicRotation += value ? graphicData.flipExtraRotation : -graphicData.flipExtraRotation;

			_flipUv = value;
		}
	}

	public Material Material
	{
		get => _material;
		protected set
		{
			if (value == _material)
				return;

			_material = value;
			InitializeFromAtlas(Thing.GetAtlasGroup());
		}
	}

	public override bool ShouldPrint => true;

	public override bool ShouldDraw => false;

	public override void PrintAt(SectionLayer layer, in TransformData transformData)
	{
		var drawPos = transformData.Position + CombinedDrawOffset.ScaledBy(transformData.Scale);
		if (layer == null!)
		{
			LogFailedPrintAttempt(drawPos);
			return;
		}

		var rotation = CombinedRotation + transformData.CombinedRotation;
		var scale = RotatedDrawScale * transformData.Scale;
		var flipUv = FlipUv;
		var scaledDrawSize = scale * RotatedDrawSize;
		
		if (scaledDrawSize.IsFlipped())
		{
			flipUv = !flipUv;
			scaledDrawSize = scaledDrawSize.Flip();
		}

		Printer_Plane.PrintPlane(layer, drawPos, scaledDrawSize, Material, rotation.AsFloat, flipUv, _uvs,
			_colors);

		if (ShadowData is { } shadowData)
			PrintUtility.PrintShadowAt(shadowData, drawPos, ThingRotation, layer);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private void LogFailedPrintAttempt(Vector3 drawPos)
		=> Log.Error($"Tried printing '{Thing}' at drawLoc '{drawPos}' with null section layer.\n{
			new StackTrace(true)}");

	public override void DrawAt(in TransformData transformData)
	{
		var drawLoc = transformData.Position + CombinedDrawOffset.ScaledBy(transformData.Scale);
		var scale = transformData.Scale;
		var extraRotation = transformData.CombinedRotation;

		if (extraRotation == default && scale == Vector2.one)
			DrawAtInternal(drawLoc);
		else
			DrawAtInternal(drawLoc, scale, extraRotation.AsFloat);
	}
	
	private void DrawAtInternal(in Vector3 drawLoc)
	{
		ref var matrix = ref _matrix;
		var previousPosition = matrix.GetPosition();
		matrix.SetPosition(drawLoc);
		Draw(ref matrix);
		matrix.SetPosition(previousPosition);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private void DrawAtInternal(Vector3 drawLoc, Vector2 drawScale, float extraRotation)
	{
		var matrix = Matrix4x4.TRS(drawLoc,
			(extraRotation += CombinedRotation) == 0f
				? Quaternion.identity
				: Quaternion.AngleAxis(extraRotation, Vector3.up),
			(drawScale * RotatedDrawScale).ToVector3());
		
		Draw(ref matrix);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void Draw(ref Matrix4x4 matrix)
		=> Graphics.Internal_DrawMesh_Injected(_mesh, 0, ref matrix, Material, 0, null, null,
			ShadowCastingMode.On, true, null, LightProbeUsage.BlendProbes, null);

	public override void NotifyMaterialPossiblyChanged()
	{
		base.NotifyMaterialPossiblyChanged();

		Material = Graphic!.MatAt(ThingRotation, Thing);
	}

	protected override void UpdateMatrix()
		=> _matrix = Matrix4x4.TRS(CombinedDrawOffset, Quaternion.AngleAxis(CombinedRotation, Vector3.up),
			RotatedDrawScale.ToVector3());

	public override void SetThingRotation(Rot4 thingRotation)
	{
		var previousFlipUv = FlipUv;
		var flip = _flipUv = ShouldFlip;

		_mesh = Graphic!.MeshAt(thingRotation);
		_graphicDrawOffset = Graphic.DrawOffset(thingRotation);
		
		var newGraphicRotation = Graphic.AngleFromRot(thingRotation);
		if (flip && Graphic.data is { } graphicData)
			newGraphicRotation += graphicData.flipExtraRotation;

		_graphicRotation = newGraphicRotation;
		UpdateMatrix();

		if (TryUpdateMaterial(thingRotation) || previousFlipUv != flip)
			InitializeFromAtlas(Thing.GetAtlasGroup());
	}

	private bool TryUpdateMaterial(Rot4 thingRotation)
	{
		var newMaterial = Graphic!.MatAt(thingRotation, Thing);
		if (_material == newMaterial)
			return false;

		_material = newMaterial;
		return true;
	}

	private void InitializeFromAtlas(TextureAtlasGroup atlasGroup)
	{
		Graphic.TryGetTextureAtlasReplacementInfo(Material, atlasGroup, FlipUv, true,
			out _material, out _uvs, out var vertexColor);

		Array.Fill(_colors ??= new Color32[4], vertexColor);
	}

	public static bool IsCompatibleGraphic(Graphic? graphic)
		=> graphic != null
			&& CompatibleGraphicTypes is var compatibleTypes
#if !V1_4
			&& (graphic is not Graphic_Collection collection
				|| (compatibleTypes.Contains(collection.SingleGraphicType) // 1.4 always loads Graphic_Single and Multi
					&& compatibleTypes.Contains(collection.MultiGraphicType)))
#endif
			&& compatibleTypes.Contains(graphic.GetType());

	public static bool IsCompatibleThing(Thing thing) => CompatibleThingTypes.Contains(thing.GetType()); // TODO: handle comps

	// TODO: discover valid Print/Draw overrides with MethodBodyReader by looking for Printer_Plane or Graphics.DrawMesh calls
	public static HashSet<Type> CompatibleGraphicTypes { get; }
		= ((Type[])
		[
			typeof(Graphic), typeof(Graphic_RandomRotated),

		#region CollectionSubclasses // these simply override DrawWorker with SubGraphicFor().DrawWorker()
			typeof(Graphic_StackCount), typeof(Graphic_Random), typeof(Graphic_Appearances),
#if !V1_4
			typeof(Graphic_Indexed),
#endif
		#endregion
		])
		.SelectMany(static graphic => graphic.WithGraphicSubclassesNotOverridingPrintOrDraw())
		.ToHashSet();

	public static HashSet<Type> CompatibleThingTypes { get; } =
	[
		..typeof(Thing).WithThingSubclassesNotOverridingPrintOrDraw(),
		..typeof(ThingWithComps).WithThingSubclassesNotOverridingPrintOrDraw()
	];
	
	public new class Factory : PrintData.Factory
	{
		public override bool IsCompatibleWith(Thing thing, Graphic? graphic, bool ignoreThingType)
			=> (ignoreThingType || IsCompatibleThing(thing)) && IsCompatibleGraphic(graphic);

		public override PrintData CreateFor(Thing thing, Graphic? graphic) => new OptimizedPrintData();
	}
}
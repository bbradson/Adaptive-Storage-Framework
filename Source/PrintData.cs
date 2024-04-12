// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

using System.Diagnostics;
using UnityEngine.Rendering;

namespace AdaptiveStorage;

public class PrintData
{
	private Thing _thing;
	private Graphic _graphic;
	private Material _material;
	private Mesh _mesh;
	public SectionLayer? Layer { get; set; }
	private Matrix4x4 _matrix;
	private Vector3
		_drawLoc,
		_drawOffset;
	private Rot4 _thingRotation;
	private Vector2 _drawSize;
	private float
		_extraRotation,
		_rotation;
	private bool _flipUv;
	private Vector2[]? _uvs;
	public ShadowData? ShadowData { get; set; }
	public Color32[]? Colors { get; set; }

	public Thing Thing => _thing;

	public Graphic Graphic
	{
		get => _graphic;
		set => SetGraphic(value);
	}

	public Mesh Mesh
	{
		get => _mesh;
		set
		{
			_mesh = value;
			UpdateMatrix();
		}
	}

	public Matrix4x4 Matrix => _matrix;

	public Vector2 DrawSize
	{
		get => _drawSize;
		set
		{
			_drawSize = !Graphic.ShouldDrawRotated && ThingRotation.IsHorizontal ? value.Rotated() : value;
			UpdateMatrix();
		}
	}

	public Vector3 DrawLoc
	{
		get => _drawLoc;
		set
		{
			_drawLoc = value;
			_matrix.SetPosition(value + _drawOffset);
		}
	}

	public Vector3 DrawOffset
	{
		get => _drawOffset;
		set
		{
			_drawOffset = value;
			_matrix.SetPosition(DrawLoc + value);
		}
	}

	public Rot4 ThingRotation
	{
		get => _thingRotation;
		set => SetThingRotation(value);
	}

	public float ExtraRotation
	{
		get => _extraRotation;
		set
		{
			_extraRotation = value;
			var newRotation = value + Graphic.AngleFromRot(ThingRotation);
			if (FlipUv && Graphic.data is { } graphicData)
				newRotation += graphicData.flipExtraRotation;

			Rotation = newRotation;
		}
	}

	public float Rotation
	{
		get => _rotation;
		set
		{
			_rotation = value;
			UpdateMatrix();
		}
	}

	public bool FlipUv
	{
		get => _flipUv;
		set
		{
			if (value == _flipUv)
				return;

			if (Graphic.data is { } graphicData)
				Rotation = _rotation + (value ? graphicData.flipExtraRotation : -graphicData.flipExtraRotation);
			
			_flipUv = value;
		}
	}

	public Vector2[]? Uvs
	{
		get => _uvs;
		// ReSharper disable once PropertyCanBeMadeInitOnly.Global
		set => _uvs = value;
	}

	public Material Material
	{
		get => _material;
		set => _material = value;
	}

	public bool DrawShadow
	{
		get => ShadowData != null;
		set
			=> ShadowData = value
				? (ShadowData == _defaultShadowData ? Graphic.ShadowGraphic?.shadowInfo : null) ?? _defaultShadowData
				: null;
	}

	public void Print() => PrintAt(_drawLoc);

	public void PrintAt(Vector3 drawLoc)
	{
		if (Layer is null)
		{
			LogFailedPrintAttempt();
			return;
		}

		drawLoc += DrawOffset;

		if (Thing is MinifiedThing minifiedThing)
		{
			minifiedThing.PrintAt(Layer, drawLoc);
			return;
		}

		Printer_Plane.PrintPlane(Layer, drawLoc, DrawSize, Material, Rotation, FlipUv, Uvs, Colors);

		if (ShadowData is { } shadowData && shadowData != _defaultShadowData)
			PrintUtility.PrintShadowAt(shadowData, drawLoc, ThingRotation, Layer);
	}

	private void LogFailedPrintAttempt()
		=> Log.Error($"Tried printing '{Thing}' at drawLoc '{_drawLoc}' with null section layer.\n{
			new StackTrace(true)}");

	public void DrawAt(in Vector3 drawLoc)
	{
		var previousPosition = _matrix.GetPosition();
		_matrix.SetPosition(drawLoc + DrawOffset);
		Draw();
		_matrix.SetPosition(previousPosition);
	}

	public void Draw()
		=> Graphics.Internal_DrawMesh_Injected(Mesh, 0, ref _matrix, Material, 0, null, null,
			ShadowCastingMode.On, true, null, LightProbeUsage.BlendProbes, null);

	public PrintData(Thing thing, in Vector3 drawLoc, in Vector3 drawOffset, Rot4 thingRotation, SectionLayer? layer, float drawScale,
		float extraRotation, bool drawShadow, Vector2 maxDrawSize)
		: this(thing, thing.Graphic, null!, null!, layer, drawLoc, thingRotation, maxDrawSize, drawOffset,
			extraRotation, shadowData: drawShadow ? _defaultShadowData : null)
		=> InitializeFromThing(drawScale);

	public PrintData(Thing thing, Graphic graphic, Material material, TextureAtlasGroup atlasGroup, in Vector3 drawLoc,
		in Vector3 drawOffset, Rot4 thingRotation, SectionLayer? layer, Vector2 drawSize, float extraRotation,
		bool drawShadow)
		: this(thing, graphic, material, null!, layer, drawLoc, thingRotation, drawSize, drawOffset, extraRotation,
			shadowData: drawShadow ? _defaultShadowData : null)
		=> InitializeFromGraphic(atlasGroup);

	private void UpdateMatrix()
	{
		var meshSize = Mesh.bounds.size;
		if (meshSize.x != 0f && meshSize.z != 0f)
		{
			meshSize.x = DrawSize.x / meshSize.x;
			meshSize.z = DrawSize.y / meshSize.z;
		}
		_matrix = Matrix4x4.TRS(_drawLoc, Quaternion.AngleAxis(Rotation, Vector3.up), meshSize);
	}

	public void SetDrawSize(Vector2 drawSize, float drawScale, Vector2 maxDrawSize)
	{
		SetDrawSizeDirect(drawSize, drawScale, maxDrawSize);
		DrawSize = _drawSize;
	}

	private void SetDrawSizeDirect(Vector2 drawSize, float drawScale, Vector2 maxDrawSize)
	{
		drawSize *= drawScale;

		if (drawSize.x > maxDrawSize.x)
			drawSize *= maxDrawSize.x / drawSize.x;

		if (drawSize.y > maxDrawSize.y)
			drawSize *= maxDrawSize.y / drawSize.y;

		_drawSize = drawSize;
	}

	public void SetThingRotation(Rot4 thingRotation, TextureAtlasGroup? atlasGroup = null, bool updateMaterial = true)
	{
		if (_thingRotation == thingRotation)
			return;

		if (updateMaterial)
			updateMaterial = TryUpdateMaterial(thingRotation);

		if (!Graphic.ShouldDrawRotated && _thingRotation.IsHorizontal != thingRotation.IsHorizontal)
			_drawSize = DrawSize.Rotated();

		var flipUvChanged = false;
		
		if (!Graphic.ShouldDrawRotated)
		{
			if (_thingRotation.IsHorizontal != thingRotation.IsHorizontal)
				_drawSize = DrawSize.Rotated();
			
			var newFlipUv = thingRotation.AsInt switch
			{
				Rot4.WestInt => Graphic.WestFlipped,
				Rot4.EastInt => Graphic.EastFlipped,
				_ => false
			};

			if (newFlipUv != FlipUv)
			{
				_flipUv = newFlipUv;
				flipUvChanged = true;
			}
		
			if (flipUvChanged && Graphic.data is { } graphicData)
				_rotation += newFlipUv ? graphicData.flipExtraRotation : -graphicData.flipExtraRotation;
		}
		
		_mesh = Graphic.MeshAt(thingRotation);

		_thingRotation = thingRotation;
		UpdateMatrix();
		
		if (updateMaterial || flipUvChanged)
			InitializeFromAtlas(atlasGroup ?? Thing.GetAtlasGroup());
	}

	private bool TryUpdateMaterial(Rot4 thingRotation)
	{
		var newMaterial = Graphic.MatAt(thingRotation, Thing);
		if (_material == newMaterial)
			return false;

		_material = newMaterial;
		return true;
	}

	public void SetGraphic(Graphic graphic, TextureAtlasGroup? atlasGroup = null, bool updateMaterial = true)
	{
		_graphic = graphic;

		if (updateMaterial)
			_material = graphic.MatAt(ThingRotation, Thing);
		
		InitializeFromGraphic(atlasGroup ?? Thing.GetAtlasGroup());
	}

	private void InitializeFromThing(float drawScale)
	{
		SetDrawSizeDirect(Graphic.drawSize, drawScale, DrawSize);
		InitializeFromThing();
	}

	private void InitializeFromThing()
	{
		_material = Graphic.MatAt(ThingRotation, Thing);
		InitializeFromGraphic(Thing.GetAtlasGroup());
	}

	private void InitializeFromGraphic(TextureAtlasGroup atlasGroup)
	{
		var graphic = _graphic;
		_rotation = ExtraRotation + graphic.AngleFromRot(ThingRotation);
		_flipUv = !graphic.ShouldDrawRotated;

		if (FlipUv)
		{
			if (ThingRotation.IsHorizontal)
				_drawSize = DrawSize.Rotated();
			
			_flipUv = ThingRotation.AsInt switch
			{
				Rot4.WestInt => graphic.WestFlipped,
				Rot4.EastInt => graphic.EastFlipped,
				_ => false
			};
		
			if (FlipUv && graphic.data is { } graphicData)
				_rotation += graphicData.flipExtraRotation;
		}
		
		_mesh = graphic.MeshAt(ThingRotation);
		UpdateMatrix();
		
		InitializeFromAtlas(atlasGroup);
		
		if (ShadowData != null)
			ShadowData = graphic.ShadowGraphic?.shadowInfo ?? _defaultShadowData;
	}

	private void InitializeFromAtlas(TextureAtlasGroup atlasGroup)
	{
		Graphic.TryGetTextureAtlasReplacementInfo(Material, atlasGroup, FlipUv, true,
			out _material, out _uvs, out var vertexColor);
		
		Colors ??= new Color32[4];
		Array.Fill(Colors, vertexColor);
	}

	private static ShadowData _defaultShadowData = new();

	public PrintData(Thing thing, Graphic graphic, Material material, Mesh mesh, SectionLayer? layer,
		in Vector3 drawLoc, Rot4 thingRotation, Vector2 drawSize, in Vector3 drawOffset = default,
		float extraRotation = 0f, float rotation = 0f, bool flipUv = false, ShadowData? shadowData = null,
		Vector2[]? uvs = null, Color32[]? colors = null)
	{
		_thing = thing;
		_graphic = graphic;
		_material = material;
		_mesh = mesh;
		Layer = layer;
		_drawLoc = drawLoc;
		_thingRotation = thingRotation;
		_drawSize = drawSize;
		_drawOffset = drawOffset;
		_extraRotation = extraRotation;
		_rotation = rotation;
		_flipUv = flipUv;
		ShadowData = shadowData;
		Uvs = uvs;
		Colors = colors;
	}
}
// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

using System.Xml;

namespace AdaptiveStorage;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class ItemGraphic : IHasPosition
{
	public static Vector3 DefaultStackOffset
		=> new(DEFAULT_STACK_OFFSET_X, DEFAULT_STACK_OFFSET_Y, DEFAULT_STACK_OFFSET_Z);
	
	public const float
		DEFAULT_STACK_OFFSET_X = 0.11f,
		DEFAULT_STACK_OFFSET_Y = 3f / 740f,
		DEFAULT_STACK_OFFSET_Z = 0.24f;
	
	public static Vector2 DefaultMaxDrawSize => new(float.MaxValue, float.MaxValue);

	public static ItemGraphic Default { get; } = new();
	
	public int? position;

	public bool
		visible = true,
		drawShadow = true;
	
	public float
		drawScale = 1f,
		rotation,
		stackRotation,
		stackOffsetFactor = 1f;

	public Rot4? textureOrientation;
	
	public Vector2 maxDrawSize = DefaultMaxDrawSize;

	public StackBehaviour stackBehaviour;

	public Vector3
		stackOffset = DefaultStackOffset,
		drawOffset;

	public Vector3?
		stackOffsetNorth,
		stackOffsetEast,
		stackOffsetSouth,
		stackOffsetWest,
		drawOffsetNorth,
		drawOffsetEast,
		drawOffsetSouth,
		drawOffsetWest;
	
	int? IHasPosition.Position
	{
		get => position;
		set => position = value;
	}
	
	public void LoadDataFromXmlCustom(XmlNode xmlRoot)
	{
		if (xmlRoot.Name.Length > 1 && int.TryParse(xmlRoot.Name[1..], out var value))
			position = value;

		foreach (XmlNode childNode in xmlRoot.ChildNodes)
		{
			if (childNode.NodeType == XmlNodeType.Comment)
				continue;

			var field = GetType().GetField(childNode.Name);
			if (ParseHelper.FromString(childNode.InnerText, field.FieldType) is { } parsedValue)
				field.SetValue(this, parsedValue);
		}
	}

	public Vector3 StackOffsetForRot(Rot4 rot)
		=> rot.AsInt switch
		{
			Rot4.NorthInt => stackOffsetNorth,
			Rot4.EastInt => stackOffsetEast,
			Rot4.SouthInt => stackOffsetSouth,
			Rot4.WestInt => stackOffsetWest,
			_ => stackOffset
		} ?? stackOffset;

	public Vector3 DrawOffsetForRot(Rot4 rot)
		=> rot.AsInt switch
		{
			Rot4.NorthInt => drawOffsetNorth,
			Rot4.EastInt => drawOffsetEast,
			Rot4.SouthInt => drawOffsetSouth,
			Rot4.WestInt => drawOffsetWest,
			_ => drawOffset
		} ?? drawOffset;
}
// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

namespace AdaptiveStorage;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class ItemGraphic : CellData
{
	public static Vector3 DefaultStackOffset
		=> new(DEFAULT_STACK_OFFSET_X, DEFAULT_STACK_OFFSET_Y, DEFAULT_STACK_OFFSET_Z);
	
	public const float
		DEFAULT_STACK_OFFSET_X = 0.11f,
		DEFAULT_STACK_OFFSET_Y = Altitudes.AltInc / 10f,
		DEFAULT_STACK_OFFSET_Z = 0.24f;
	
	public static Vector2 DefaultMaxDrawSize => new(float.MaxValue, float.MaxValue);

	public static ItemGraphic Default { get; } = InitializeDefaultGraphic();

	public Type workerClass = typeof(ItemGraphicWorker);

	[Unsaved]
	private ItemGraphicWorker _worker = null!;

	public bool
		visible = true,
		drawShadow = true;
	
	public float
		rotation,
		stackRotation,
		stackOffsetFactor = 1f,
		multipleItemsDrawSizeFactor = 0.8f;

	public Vector2 drawScale = Vector2.one;

	public Rot4? textureOrientation;
	
	public Vector2 maxDrawSize = DefaultMaxDrawSize;

	public StackBehaviour stackBehaviour = StackBehaviour.Default;

	public RotateInShelvesMode rotateInShelvesMode = RotateInShelvesMode.Reverse;

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

	public ItemGraphicWorker Worker => _worker;

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

	public virtual void Initialize(GraphicsDef? parent)
		=> _worker = WorkerClassMaker<ItemGraphicWorker>.MakeWorker(workerClass, parent!, this, parent!)
			?? new ItemGraphicWorker(this, parent);

	private static ItemGraphic InitializeDefaultGraphic()
	{
		var graphic = new ItemGraphic { rotateInShelvesMode = RotateInShelvesMode.Default };
		graphic.Initialize(null);
		return graphic;
	}
}
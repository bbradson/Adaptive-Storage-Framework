// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

// ReSharper disable InconsistentNaming
namespace AdaptiveStorage;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class StorageGraphic
{
	protected StorageGraphicData? graphicData;

	public List<StorageGraphicData> graphicDatas = [];

	public Type workerClass = typeof(StorageGraphicWorker);

	[Unsaved]
	private StorageGraphicWorker _worker = null!;

	public bool?
		showContainedItems,
		showBaseGraphic;

	public ContentColorSource useDominantContentColor;

	public int minimumStackCount;

	public StorageGraphicWorker Worker => _worker;

	public virtual void Initialize(GraphicsDef parent)
	{
		_worker = WorkerClassMaker<StorageGraphicWorker>.MakeWorker(workerClass, parent, this, parent)
			?? new StorageGraphicWorker(this, parent);
		
		// ReSharper disable once NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
		graphicDatas ??= [];
		
		if (graphicData != null)
			graphicDatas.AddDistinct(graphicData);
		
		foreach (var graphic in graphicDatas)
			graphic.Resolve(this, parent);
	}
}
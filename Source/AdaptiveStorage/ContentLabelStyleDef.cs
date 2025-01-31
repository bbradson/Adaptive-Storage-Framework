// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

namespace AdaptiveStorage;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class ContentLabelStyleDef : Def
{
	public Type? workerClass;

	[Unsaved]
	private ContentLabelWorker? _contentLabelWorker;

	public ContentLabelWorker ContentLabelWorker => _contentLabelWorker!;

	public override void ResolveReferences()
		=> (_contentLabelWorker = WorkerClassMaker<ContentLabelWorker>.MakeWorker(workerClass, this, this)
			?? new ContentLabelWorker.Automatic(this)).ResolveReferences();
}
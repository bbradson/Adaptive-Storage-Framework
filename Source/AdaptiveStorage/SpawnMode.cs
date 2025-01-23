// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

namespace AdaptiveStorage;

[Flags]
public enum SpawnMode
{
	Default = 0,
	RespawningAfterLoad = 1 << 0,
	Minify = 1 << 1,
	PackContents = 1 << 2
}
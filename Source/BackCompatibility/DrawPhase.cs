// Copyright (c) 2024 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

#if V1_4
namespace AdaptiveStorage.BackCompatibility;

public enum DrawPhase
{
	EnsureInitialized,
	ParallelPreDraw,
	Draw,
}
#endif
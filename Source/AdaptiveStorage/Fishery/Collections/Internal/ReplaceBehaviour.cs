// Copyright (c) 2024 bradson
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

namespace AdaptiveStorage.Fishery.Collections.Internal;

public enum ReplaceBehaviour
{
	Replace,
	ThrowDuplicate,
	Return,
	ThrowConcurrent
}
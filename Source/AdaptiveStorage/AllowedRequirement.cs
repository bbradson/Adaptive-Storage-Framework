// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

namespace AdaptiveStorage;

public enum AllowedRequirement
{
	Any,
	All,
	Majority,
	Minority,
	MajorityOrEqual,
	MinorityOrEqual,
	Equal,
	None,
	AnyNot,
	Always,
	Never
}
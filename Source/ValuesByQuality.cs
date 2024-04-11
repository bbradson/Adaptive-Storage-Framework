// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

namespace AdaptiveStorage;

[UsedImplicitly(ImplicitUseTargetFlags.Members)]
public class ValuesByQuality
{
	public int? awful, poor, normal, good, excellent, masterwork, legendary;
	
	private int? GetForDirectly(QualityCategory quality)
		=> quality switch
		{
			QualityCategory.Awful => awful,
			QualityCategory.Poor => poor,
			QualityCategory.Normal => normal,
			QualityCategory.Good => good,
			QualityCategory.Excellent => excellent,
			QualityCategory.Masterwork => masterwork,
			QualityCategory.Legendary => legendary,
			_ => default
		};

	public int? GetFor(QualityCategory quality)
	{
		var qualityCopy = quality + 1;
		do
		{
			if (GetForDirectly(--qualityCopy) is { } result)
				return result;
		}
		while (qualityCopy > QualityCategory.Awful);

		qualityCopy = quality;
		do
		{
			if (GetForDirectly(++qualityCopy) is { } result)
				return result;
		}
		while (qualityCopy < QualityCategory.Legendary);

		if ((uint)quality <= (uint)QualityCategory.Legendary)
			return default;
		else
			throw new NotSupportedException(quality.ToString());
	}
}
// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

#if V1_4
namespace AdaptiveStorage.BackCompatibility;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class StatModifierQuality
{
	public StatDef? stat;

	public float awful, poor, normal, good, excellent, masterwork, legendary;

	public string ToStringAsOffsetRange
		=> stat!.Worker.ValueToString(GetValue(QualityCategory.Awful), finalized: false, ToStringNumberSense.Offset)
			+ " ~ "
			+ stat.Worker.ValueToString(GetValue(QualityCategory.Legendary), finalized: false,
				ToStringNumberSense.Offset);

	public string ToStringAsFactorRange
		=> GetValue(QualityCategory.Awful).ToStringByStyle(ToStringStyle.PercentZero)
			+ " ~ "
			+ GetValue(QualityCategory.Legendary).ToStringByStyle(ToStringStyle.PercentZero);

	public float GetValue(QualityCategory qc)
		=> qc switch
		{
			QualityCategory.Awful => awful,
			QualityCategory.Poor => poor,
			QualityCategory.Normal => normal,
			QualityCategory.Good => good,
			QualityCategory.Excellent => excellent,
			QualityCategory.Masterwork => masterwork,
			QualityCategory.Legendary => legendary,
			_ => throw new ArgumentOutOfRangeException(qc.ToString()),
		};
}
#endif
// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

using System.Linq;
using System.Xml;

namespace AdaptiveStorage.Utility;

[UsedImplicitly(ImplicitUseTargetFlags.Members)]
public class ValuesByQuality : CellData
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

	public int? Max()
	{
		var max = default(int?);
		for (var i = (int)QualityCategory.Legendary + 1; --i >= 0;)
		{
			var next = GetForDirectly((QualityCategory)i);
			max = max is null ? next
				: next is null ? max
				: Math.Max(max.GetValueOrDefault(), next.GetValueOrDefault());
		}

		return max;
	}

	public override void LoadDataFromXmlCustom(XmlNode xmlRoot)
	{
		base.LoadDataFromXmlCustom(xmlRoot);

		if (!xmlRoot.ChildNodes.Cast<XmlNode>().Any(static node => node.NodeType != XmlNodeType.Comment)
			&& xmlRoot.InnerText is { } text
			&& !text.NullOrEmpty())
		{
			normal = ParseHelper.FromString<int>(xmlRoot.InnerText);
		}
	}
}
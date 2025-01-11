// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

using System.Xml;

namespace AdaptiveStorage.Utility;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class CellData : IHasPosition
{
	public int? position;

	int? IHasPosition.Position
	{
		get => position;
		set => position = value;
	}

	public virtual void LoadDataFromXmlCustom(XmlNode xmlRoot)
	{
		if (xmlRoot.Name.Length > 1 && int.TryParse(xmlRoot.Name[1..], out var value))
			position = value;

		foreach (XmlNode childNode in xmlRoot.ChildNodes)
		{
			if (childNode.NodeType == XmlNodeType.Comment)
				continue;

			var field = GetType().GetField(childNode.Name);
			if (ParseHelper.FromString(childNode.InnerText, field.FieldType) is { } parsedValue)
				field.SetValue(this, parsedValue);
		}
	}
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class CellData<T> : CellData
{
#pragma warning disable CS8618
	public T value;
#pragma warning restore CS8618
}
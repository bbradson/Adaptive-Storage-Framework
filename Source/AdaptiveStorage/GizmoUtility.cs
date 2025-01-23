// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

using System.Linq;

namespace AdaptiveStorage;

public static class GizmoUtility
{
	public static IEnumerable<Gizmo> Filter(IEnumerable<Gizmo> gizmos)
		=> !AdaptiveStorageFrameworkSettings.ShowContentsTab ? gizmos : RemoveSelectStackButtons(gizmos);

	private static IEnumerable<Gizmo> RemoveSelectStackButtons(IEnumerable<Gizmo> gizmos)
		=> gizmos.Where(static gizmo => gizmo is not Command_SelectStorage);
}
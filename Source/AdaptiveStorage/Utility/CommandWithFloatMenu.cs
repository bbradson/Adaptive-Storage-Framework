// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

using System.Linq;

namespace AdaptiveStorage.Utility;

public class CommandWithFloatMenu : Command_Action
{
	public IEnumerable<FloatMenuOption>? floatMenuOptions;

	public Func<IEnumerable<FloatMenuOption>>
		floatMenuOptionsGetter,
		floatMenuOptionsInitializer = static () => Enumerable.Empty<FloatMenuOption>();

	public override IEnumerable<FloatMenuOption> RightClickFloatMenuOptions => floatMenuOptionsGetter();

	public CommandWithFloatMenu() => floatMenuOptionsGetter = () => floatMenuOptions ??= floatMenuOptionsInitializer();
}
using CharaChorderInterface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CharaChorderInterfaceTests;

public class ActionTests
{
	[Test] public void HasActions() => Assert.That(Maps.ActionMap, Is.Not.Empty);

	[Test] public void MaxActionLengthAssumption() => Assert.That(Maps.ActionMap.Max(action => action.Length), Is.LessThanOrEqualTo(13));
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CharaChorderInterfaceTests;

public class ChordTests
{
	[Theory]
	public void ChordSerialization((string[] chord, string chordHex) point)
	{
		Assert.Multiple(() =>
		{
			Assert.That(() => CharaChorderInterface.Chordmap.ActionsToHexChord(point.chord), Is.EqualTo(point.chordHex));
			Assert.That(() => CharaChorderInterface.Chordmap.HexChordToActions(point.chordHex), Is.EqualTo(NormalizeActions(point.chord)));
		});
	}

	[DatapointSource]
	private static IEnumerable<(string[], string)> GenerateChordTests()
	{
		yield return (AsciiToActions("uto"), "001D4741BC0000000000000000000000");
		yield return (AsciiToActions("xq"), "001E0710000000000000000000000000");
		yield return (AsciiToActions("abc"), "0018C621840000000000000000000000");
		yield return (AsciiToActions("qwerty"), "001E4771D0721C465000000000000000");
		yield return (AsciiToActions("yuqlk"), "001E4751C46C1AC00000000000000000");
		yield return (new[] { "y", "b" }, "001E4620000000000000000000000000");
		yield return (new[] { "w", "r", "o", "l", "d" }, "001DC721BC6C19000000000000000000");
		yield return (new[] { "v", "h", "e", "a" }, "001D8681946100000000000000000000");
		yield return (new[] { "u", "s", "o", "l", "h", "d" }, "001D4731BC6C1A064000000000000000");
		yield return (new[] { "u", "n", "e", "d" }, "001D46E1946400000000000000000000");
		yield return (new[] { "e", "c", "a" }, "00194631840000000000000000000000");
		yield return (new[] { "?", "-" }, "000FC2D0000000000000000000000000");
		yield return (new[] { "RIGHT_SHIFT", "LEFT_SHIFT", "h", "c" }, "00816011A06300000000000000000000");
		yield return (new[] { "DUP", "u", "t", "o", "n", "m" }, "00860751D06F1B86D000000000000000");
		yield return (new[] { "u", "t", "o" }, "001D4741BC0000000000000000000000");
	}
	static string[] AsciiToActions(string ascii)
	{
		var actions = ascii.ToCharArray().Select(c => c.ToString());
		//var normalized = NormalizeActions(actions); // do not normalize here, so the function is fully tested
		return actions.ToArray();
	}

	static IEnumerable<string> NormalizeActions(IEnumerable<string> actions) => actions.Distinct().OrderByDescending(GetActionIndex);
	static int GetActionIndex(string action) => Array.IndexOf(CharaChorderInterface.Maps.ActionMap, action);
}

public class PhraseTests
{
	[Theory]
	public void PhraseSerialization((string[] phrase, string phraseHex) point)
	{
		Assert.Multiple(() =>
		{
			Assert.That(() => CharaChorderInterface.Chordmap.ActionsToHexPhrase(point.phrase), Is.EqualTo(point.phraseHex));
			Assert.That(() => CharaChorderInterface.Chordmap.HexPhraseToActions(point.phraseHex), Is.EqualTo(point.phrase));
		});
	}

	[DatapointSource]
	private static IEnumerable<(string[], string)> GenerateChordTests()
	{
		yield return (AsciiToActions("mountain"), "6D6F756E7461696E");
		yield return (AsciiToActions("eye"), "657965");
		yield return (AsciiToActions("your"), "796F7572");
		yield return (AsciiToActions("CharaChorder"), "436861726143686F72646572");
		yield return (AsciiToActions("family"), "66616D696C79");
		yield return (AsciiToActions("cut"), "637574");
		yield return (AsciiToActions("test chord"), "746573742063686F7264");
		yield return (AsciiToActions("carry"), "6361727279");
		yield return (AsciiToActions("put"), "707574");
		yield return (AsciiToActions("up"), "7570");
		yield return (AsciiToActions("us"), "7573");
		yield return (AsciiToActions("different"), "646966666572656E74");
		yield return (AsciiToActions("carpe diem"), "6361727065206469656D");
		yield return (AsciiToActions("test phrase"), "7465737420706872617365");
		yield return (AsciiToActions("hello world"), "68656C6C6F20776F726C64");
		yield return (new[] { "i", "f", "KM_2_L", "(", "KM_2_L", ")", "ARROW_LF" }, "69660226280226290150");
		yield return (new[] { "i", "f", "KM_2_L", "(", "KM_2_L", ")", "KM_2_L", "{", "KM_2_L", "W", "BKSP", "KM_2_L", "}", "ARROW_LF", "ARROW_LF", "ARROW_LF" }, "696602262802262902267B022657012A02267D015001500150");
	}
	static string[] AsciiToActions(string ascii)
		=> ascii.ToCharArray()
			.Select(i => i.ToString())
			.ToArray();
}

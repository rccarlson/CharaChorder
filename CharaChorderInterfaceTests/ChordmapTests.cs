using CharaChorderInterface;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CharaChorderInterfaceTests;

public class ChordmapTests
{
	[TestCase("test phrase", ExpectedResult = "7465737420706872617365")]
	[TestCase("hello world", ExpectedResult = "68656C6C6F20776F726C64")]
	[TestCase("CharaChorder", ExpectedResult = "436861726143686F72646572")]
	public string HumanStringToHexPhrase(string asciiPhrase)
	{
		var actions = asciiPhrase.Select(c => c.ToString()).ToArray();
		return Chordmap.ActionsToHexPhrase(actions);
	}

	[TestCase("xq", ExpectedResult = "001E0710000000000000000000000000")]
	[TestCase("abc", ExpectedResult = "0018C621840000000000000000000000")]
	[TestCase("cba", ExpectedResult = "0018C621840000000000000000000000")]
	[TestCase("cba", ExpectedResult = "0018C621840000000000000000000000")]
	[TestCase("qwerty", ExpectedResult = "001E4771D0721C465000000000000000")]
	[TestCase("qwerty", ExpectedResult = "001E4771D0721C465000000000000000")]
	public string HumanStringToHexChord(string asciiChord)
	{
		var actions = asciiChord.Select(c => c.ToString()).ToArray();
		return Chordmap.ActionsToHexChord(actions);
	}

	[TestCase("001E4751C46C1AC00000000000000000", ExpectedResult = "y + u + q + l + k")]
	[TestCase("001E4620000000000000000000000000", ExpectedResult = "y + b")]
	[TestCase("001DC721BC6C19000000000000000000", ExpectedResult = "w + r + o + l + d")]
	[TestCase("001D8681946100000000000000000000", ExpectedResult = "v + h + e + a")]
	[TestCase("001D4731BC6C1A064000000000000000", ExpectedResult = "u + s + o + l + h + d")]
	[TestCase("001D46E1946400000000000000000000", ExpectedResult = "u + n + e + d")]
	[TestCase("00194631840000000000000000000000", ExpectedResult = "e + c + a")]
	[TestCase("000FC2D0000000000000000000000000", ExpectedResult = "? + -")]
	[TestCase("00816011A06300000000000000000000", ExpectedResult = "RIGHT_SHIFT + LEFT_SHIFT + h + c")]
	[TestCase("00860751D06F1B86D000000000000000", ExpectedResult = "DUP + u + t + o + n + m")]
	[TestCase("001D4741BC0000000000000000000000", ExpectedResult = "u + t + o")]
	public string HexChordToActions(string hex) => string.Join(" + ", Chordmap.HexChordToActions(hex));

	[TestCase("6D6F756E7461696E", ExpectedResult = "mountain")]
	[TestCase("657965", ExpectedResult = "eye")]
	[TestCase("796F7572", ExpectedResult = "your")]
	[TestCase("436861726143686F72646572", ExpectedResult = "CharaChorder")]
	[TestCase("66616D696C79", ExpectedResult = "family")]
	[TestCase("637574", ExpectedResult = "cut")]
	[TestCase("746573742063686F7264", ExpectedResult = "test chord")]
	[TestCase("6361727279", ExpectedResult = "carry")]
	[TestCase("707574", ExpectedResult = "put")]
	[TestCase("7570", ExpectedResult = "up")]
	[TestCase("7573", ExpectedResult = "us")]
	[TestCase("646966666572656E74", ExpectedResult = "different")]
	[TestCase("6361727065206469656D", ExpectedResult = "carpe diem")]
	public string HexPhraseToAscii(string hex) => string.Join("", Chordmap.HexPhraseToActions(hex));

	[TestCase("69660226280226290150", ExpectedResult = new string[] { "i", "f", "KM_2_L", "(", "KM_2_L", ")", "ARROW_LF" })]
	[TestCase("696602262802262902267B022657012A02267D015001500150", ExpectedResult = new string[] { "i", "f", "KM_2_L", "(", "KM_2_L", ")", "KM_2_L", "{", "KM_2_L", "W", "BKSP", "KM_2_L", "}", "ARROW_LF", "ARROW_LF", "ARROW_LF" })]
	public string[] HexPhraseToActions(string hex) => Chordmap.HexPhraseToActions(hex);

	[TestCase(@"hello world")]
	[TestCase(@"test string")]
	[TestCase(@"short")]
	[TestCase(@"This is a very long string to test how the code will handle more lengthy outputs. This likely will not come up, but it should be possible to handle.")]
	[TestCase(@"The quick brown fox jumps over the lazy dog")]
	[TestCase(@"!@#$%^&*()_+-={}[]<>,.?|")]
	[TestCase(@"/")]
	[TestCase(@"\")]
	[TestCase(@" ")]
	public void RoundTripAsciiPhrase(string start)
	{
		var startActions = start.ToCharArray().Select(c => c.ToString()).ToArray();
		var hexPhrase = Chordmap.ActionsToHexPhrase(startActions);
		var endActions = Chordmap.HexPhraseToActions(hexPhrase);
		Assert.That(endActions, Is.EqualTo(startActions), message: $"'{start}' -> '{hexPhrase}' -> {string.Join(", ", endActions.Select(a => $"'{a}'"))}");
	}

	[TestCase("KSC_00")]
	[TestCase("KSC_02")]
	[TestCase("KEY_A")]
	[TestCase("KEY_Z")]
	[TestCase("KEY_1")]
	[TestCase("KEY_5")]
	[TestCase("KEY_9")]
	[TestCase("LEFT_CTRL")]
	[TestCase("LEFT_ALT")]
	[TestCase("RESTART")]
	[TestCase("DUP")]
	[TestCase("KM_1_R")]
	[Description("Test that a single Action can be encoded as a hex phrase, then decoded back to a string without any loss. " +
		"This test targets specific actions")]
	public void RoundTripSingleActionPhrase(string action)
	{
		var actionArr = new[] { action };
		var hexPhrase = Chordmap.ActionsToHexPhrase(actionArr);
		var endActions = Chordmap.HexPhraseToActions(hexPhrase);
		Assert.That(endActions, Is.EqualTo(actionArr), message: $"'{action}' -> '{hexPhrase}' -> {string.Join(", ", endActions.Select(a => $"'{a}'"))}");
	}

	[Description("Test that any single Action can be encoded as a hex phrase, then decoded back to a string without any loss. " +
		"This test runs through all non-empty actions")]
	[Test] public void RoundTripAllSingleActionPhrases()
	{
		Assert.Multiple(() =>
		{
			foreach (var action in Maps.ActionMap)
			{
				if (string.IsNullOrEmpty(action)) continue;
				RoundTripSingleActionPhrase(action);
			}
		});
	}
}

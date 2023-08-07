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

	[Test]
	[Description("Generates a sample chord library from Chords and Phrases and checks that no changes occurred during a write/read round trip")]
	public void SaveLoadChordmaps()
	{
		using MemoryStream memStrm = new();
		var sampleChordLibrary = GenerateChordLibary().ToArray();
		Chordmap.Write(memStrm, sampleChordLibrary);
		memStrm.Position = 0;
		var readLibrary = Chordmap.Read(memStrm);
		Assert.That(readLibrary.Length, Is.EqualTo(sampleChordLibrary.Length), "Not the same length");
		var zipped = Enumerable.Zip(readLibrary, sampleChordLibrary);
		foreach (var (actual, expected) in zipped)
		{
			Assert.That(() => actual, Is.EqualTo(expected));
		}

		IEnumerable<Chordmap> GenerateChordLibary()
		{
			var phrases = PhraseTests.GenerateChordTests().Select(tuple => tuple.Hex);
			var chords = ChordTests.GenerateChordTests().Select(tuple => tuple.Hex);
			foreach (var phrase in phrases)
			{
				foreach (var chord in chords)
				{
					yield return Chordmap.FromHex(chord, phrase);
				}
			}
		}
	}
}

using CharaChorderInterface.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CharaChorderInterface;

[DebuggerDisplay("{HumanReadable,nq}")]
public class Chordmap
{
	public string HexChord { get; init; } = string.Empty;
	public string HexPhrase { get; init; } = string.Empty;

	public string HumanReadable
	{
		get
		{
			var chordActions = ChordHexToActions(HexChord);
			var phraseActions = PhraseHexToActions(HexPhrase);
			var asciiActions = string.Join(" + ", chordActions);
			var asciiPhrase = string.Join("", phraseActions);
			return $"{asciiActions} => {asciiPhrase}";
		}
	}

	public static Chordmap FromHex(string hexChord, string hexPhrase)
	{
		var chord = new Chordmap()
		{
			HexChord = hexChord,
			HexPhrase = hexPhrase,
		};
		return chord;
	}

	/// <summary> Gets the component actions from a given hex chord </summary>
	internal static string[] ChordHexToActions(string hex)
	{
		if (hex is null) return Array.Empty<string>();

		var binChord = HexUtility.HexToBin(hex);//.PadLeft(66, '0').PadRight(128, '0'); // todo: add the pads?
#pragma warning disable IDE0059 // Unnecessary assignment of a value
		var chainIndex = binChord.Substring(0, 8); //unused right now; this is used for phrases that have more than 192 bytes
#pragma warning restore IDE0059 // Unnecessary assignment of a value
		List<string> humanChords = new();

		for(int i = 0; i < 12; i++)
		{
			var binAction = binChord.Substring(8 + (i * 10), 10); //take 10 bits at a time
			var actionCode = HexUtility.BinToDec(binAction);
			var humanReadable = Maps.ActionMap[actionCode];
			humanChords.Add(humanReadable);
		}

		var lastInput = humanChords.FindLastIndex(action => action != string.Empty);
		return humanChords.Take(lastInput + 1).ToArray();
	}

	internal static string[] PhraseHexToActions(string hex)
	{
		List<string> actions = new();
		for (int i = 0; i < hex.Length; i += 2)
		{
			var length = Math.Min(2, hex.Length - i);
			var actionCodeStr = hex.Substring(i, length);
			var index = int.Parse(actionCodeStr, System.Globalization.NumberStyles.HexNumber);
			var actionCode = Maps.ActionMap[index];
			actions.Add(actionCode);
		}
		return actions.ToArray();
	}
}

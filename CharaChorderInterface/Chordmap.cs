using CharaChorderInterface.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CharaChorderInterface;

[DebuggerDisplay("{HumanReadable,nq}")]
public class Chordmap : IEquatable<Chordmap?>
{
	public string HexChord { get; }
	public string HexPhrase { get; }

	public string HumanReadable
	{
		get
		{
			var chordActions = HexChordToActions(HexChord);
			var phraseActions = HexPhraseToActions(HexPhrase);
			var asciiActions = string.Join(" + ", chordActions);
			var asciiPhrase = string.Join("", phraseActions);
			return $"{asciiActions} => {asciiPhrase}";
		}
	}

	public string[] ChordActions => HexChordToActions(HexChord);
	public string[] PhraseActions => HexPhraseToActions(HexPhrase);

	public string AsciiPhrase => string.Join(string.Empty, PhraseActions);

	private Chordmap(string hexChord, string hexPhrase)
	{
		HexChord = hexChord;
		HexPhrase = hexPhrase;
	}

	public static Chordmap FromHex(string hexChord, string hexPhrase) => new Chordmap(hexChord, hexPhrase);

	public static Chordmap FromActions(string[] chordActions, string[] phraseActions)
		=> FromHex(ActionsToHexChord(chordActions), ActionsToHexPhrase(phraseActions));

	/// <summary>
	/// Creates a chord using standard ASCII. For any chords that need non-ASCII chord actions, use <see cref="FromActions(string[], string[])"/>.
	/// </summary>
	/// <param name="chord"> All <see cref="char"/>s to be used in the chord </param>
	/// <param name="phrase"> The output of the chord </param>
	public static Chordmap FromAscii(char[] chord, string phrase)
	{
		return FromActions(
			chordActions: chord.Select(c => c.ToString()).ToArray(),
			phraseActions: phrase.Select(c => c.ToString()).ToArray()
			);
	}

	/// <inheritdoc cref="FromAscii(char[], string)"/>
	public static Chordmap FromAscii(string chord, string phrase) => FromAscii(chord.ToCharArray(), phrase);

	/// <summary> Gets the component actions from a given hex chord </summary>
	internal static string[] HexChordToActions(string hex)
	{
		if (hex is null) return Array.Empty<string>();

		var binChord = NumberUtility.HexToBin(hex);//.PadLeft(66, '0').PadRight(128, '0'); // todo: add the pads?
#pragma warning disable IDE0059 // Unnecessary assignment of a value
		var chainIndex = binChord.Substring(0, 8); //unused right now; this is used for phrases that have more than 192 bytes
#pragma warning restore IDE0059 // Unnecessary assignment of a value
		List<string> humanChords = new();

		for (int i = 0; i < 12; i++)
		{
			var binAction = binChord.Substring(8 + (i * 10), 10); //take 10 bits at a time
			var actionCode = int.Parse(NumberUtility.BinToDec(binAction));
			var humanReadable = Maps.ActionMap[actionCode];
			humanChords.Add(humanReadable);
		}

		var lastInput = humanChords.FindLastIndex(action => action != string.Empty);
		return humanChords.Take(lastInput + 1).ToArray();
	}

	/// <summary> Converts a hexadecemal string into an array of actions from <see cref="Maps.ActionMap"/> </summary>
	internal static string[] HexPhraseToActions(string hex)
	{
		const int WordSize = 2;
		const int LongWordSize = 4;
		const int LargeWordCutoff = 0x20;
		List<string> actions = new();
		int idx = 0;
		while (idx < hex.Length)
		{
			int maxLength = hex.Length - idx;

			var word = hex.Substring(idx, Math.Min(WordSize, maxLength));
			var wordValue = int.Parse(word, System.Globalization.NumberStyles.HexNumber);
			string? wordAction = Maps.ActionMap.ElementAtOrDefault(wordValue);

			var longWord = hex.Substring(idx, Math.Min(LongWordSize, maxLength));
			var longWordValue = int.Parse(longWord, System.Globalization.NumberStyles.HexNumber);
			string? longWordAction = Maps.ActionMap.ElementAtOrDefault(longWordValue);

			if (!string.IsNullOrEmpty(longWordAction))
			{
				actions.Add(longWordAction);
				idx += LongWordSize;
			}
			else
			{
				actions.Add(wordAction);
				idx += WordSize;
			}
		}
		return actions.ToArray();
	}

	internal static string ActionsToHexPhrase(string[] actions)
	{
		var chordsNotInActionMap = actions.Where(action => !Maps.ActionMap.Contains(action)).ToArray();

		var actionIndices = actions
			.Select(action =>
			{
				var idx = Array.IndexOf(Maps.ActionMap, action);
				int padSize = ((int)Math.Log(idx, 16) + 2) & ~1; // bit fiddling to get even pad size multiple of 2
				return Convert.ToString(idx, 16).PadLeft(padSize, '0').ToUpperInvariant();
			});

		return string.Join(string.Empty, actionIndices); // TODO: This squashes any null action indices
	}

	internal static string ActionsToHexChord(string[] actions)
	{
		static string NormalizeHex(string hex, int targetLength) => hex.TrimStart('0').PadLeft(targetLength, '0');
		if (actions.Length > 12) throw new ArgumentOutOfRangeException("Only support up to 12 keys");

		var actionIndices = actions
			.Select(action => Array.IndexOf(Maps.ActionMap, action)) // get index of action
			.OrderByDescending(idx => idx);

		var chainIndex = 0; // left as a todo by Dot I/O
		var binChord = new StringBuilder()
			.Append(NormalizeHex(NumberUtility.DecToBin(chainIndex), 8));

		foreach (var decChordPart in actionIndices)
		{
			binChord.Append(NormalizeHex(NumberUtility.DecToBin(decChordPart), 10));
		}

		var hexChord = NumberUtility.BinToHex(binChord.ToString().PadRight(128, '0'));

		return hexChord;
	}

	public static Chordmap?[] ReadAllFromDevice(CharaChorder cc, Action<int, int>? callback = null)
	{
		var count = cc.GetChordmapCount() ?? 0;
		var chords = new Chordmap?[count];
		for (int i = 0; i < count; i++)
		{
			chords[i] = cc.GetChordmapByIndex((ushort)i);
			if (i % 3 == 0) callback?.Invoke(count, i);
		}
		return chords;
	}

	public override bool Equals(object? obj) => Equals(obj as Chordmap);

	public bool Equals(Chordmap? other)
	{
		return other is not null &&
					 HexChord == other.HexChord &&
					 HexPhrase == other.HexPhrase;
	}

	public override int GetHashCode() => HashCode.Combine(HexChord, HexPhrase);
}

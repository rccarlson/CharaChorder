using CharaChorder.Utility;
using CharaChorderInterface.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CharaChorderInterface;

[System.Diagnostics.DebuggerDisplay("{HumanReadable,nq}")]
public class Chordmap : IEquatable<Chordmap?>
{
	public string HexChord { get; }
	public string HexPhrase { get; }

	public string HumanReadable
	{
		get
		{
			var asciiActions = string.Join(" + ", ChordActions);
			return $"{asciiActions} => {AsciiPhrase}";
		}
	}

	public string[] ChordActions => HexChordToActions(HexChord);
	public string[] PhraseActions => HexPhraseToActions(HexPhrase);

	public string AsciiPhrase => string.Join(string.Empty, PhraseActions.Select(ConvertActionMapToUserFriendly));

	private Chordmap(string hexChord, string hexPhrase)
	{
		HexChord = hexChord;
		HexPhrase = hexPhrase;
	}

	public static Chordmap FromHex(string hexChord, string hexPhrase) => new Chordmap(hexChord, hexPhrase);

	public static Chordmap FromActions(IEnumerable<string> chordActions, IEnumerable<string> phraseActions)
		=> FromHex(
			ActionsToHexChord(chordActions),
			ActionsToHexPhrase(phraseActions.Where(Maps.ActionMap.Contains).ToArray())
			);

	/// <inheritdoc cref="FromActions(string[], string)"/>
	public static Chordmap FromAscii(IEnumerable<char> chord, string phrase)
	{
		return FromAscii(chord.Select(c => c.ToString()).ToArray(), phrase);
	}

	/// <inheritdoc cref="FromAscii(char[], string)"/>
	public static Chordmap FromAscii(string chord, string phrase) => FromAscii(chord.ToCharArray(), phrase);

	/// <summary>
	/// Creates a chord using standard ASCII. For any chords that need non-ASCII chord actions, use <see cref="FromActions(string[], string[])"/>.
	/// </summary>
	/// <param name="chord"> All <see cref="char"/>s to be used in the chord </param>
	/// <param name="phrase"> The output of the chord </param>
	public static Chordmap FromAscii(IEnumerable<string> chord, string phrase)
	{
		return FromActions(
			chordActions: chord,
			phraseActions: phrase
				.Select(c => c.ToString())
				.Select(ConvertToActionMapFriendly)
				.ToArray()
			);
	}

	#region HEX/ACTION TRANSLATIONS
	/// <summary> Gets the component actions from a given hex chord </summary>
	public static string[] HexChordToActions(string hex)
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
	public static string[] HexPhraseToActions(string hex)
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

	public static string ActionsToHexPhrase(IEnumerable<char> actions) => ActionsToHexPhrase(actions.Select(action => action.ToString()));
	public static string ActionsToHexPhrase(IEnumerable<string> actions)
	{
#if DEBUG
		var chordsNotInActionMap = actions.Where(action => !Maps.ActionMap.Contains(action)).ToArray();
		System.Diagnostics.Debug.Assert(chordsNotInActionMap.Length == 0, $"Actions not in action map: {string.Join(", ", chordsNotInActionMap)}");
#endif

		var actionIndices = actions
			.Where(action => action is not null)
			.Select(action =>
			{
				var idx = Array.IndexOf(Maps.ActionMap, action);
				int padSize = ((int)Math.Log(idx, 16) + 2) & ~1; // bit fiddling to get even pad size multiple of 2
				return Convert.ToString(idx, 16).PadLeft(padSize, '0').ToUpperInvariant();
			});

		return string.Join(string.Empty, actionIndices);
	}

	public static string ActionsToHexChord(IEnumerable<char> actions) => ActionsToHexChord(actions.Select(action => action.ToString()));
	public static string ActionsToHexChord(IEnumerable<string> actions)
	{
		static string NormalizeHex(string hex, int targetLength) => hex.TrimStart('0').PadLeft(targetLength, '0');
		if (actions.Count() > 12) throw new ArgumentOutOfRangeException("Only support up to 12 keys");

		var actionIndices = actions
			.Select(action => Array.IndexOf(Maps.ActionMap, action)) // get index of action
			.OrderByDescending(idx => idx);

		var chainIndex = 0; // It is used internally to store longer chord outputs. It isn’t really accessible via the serial api yet. - Matt Swarts, 8/25/23
		var binChord = new StringBuilder()
			.Append(NormalizeHex(NumberUtility.DecToBin(chainIndex), 8));

		foreach (var decChordPart in actionIndices)
		{
			binChord.Append(NormalizeHex(NumberUtility.DecToBin(decChordPart), 10));
		}

		var hexChord = NumberUtility.BinToHex(binChord.ToString().PadRight(128, '0'));

		return hexChord;
	}
	#endregion HEX/ACTION TRANSLATIONS

	/// <summary>
	/// Takes an ASCII
	/// </summary>
	/// <param name="action"></param>
	/// <returns></returns>
	public static string ConvertToActionMapFriendly(string action)
	{
		return action switch
		{
			"\n" => "ENTER",
			"\t" => "TAB",
			_ => action
		};
	}

	public static string ConvertActionMapToUserFriendly(string action)
	{
		return action switch
		{
			"ENTER" => "\n",
			"TAB" => "\t",
			_ => action
		};
	}

	#region Serialization
	public static void Write(string filepath, Chordmap?[] chordmaps)
	{
		using var file = File.OpenWrite(filepath);
		Write(file, chordmaps);
	}
	public static void Write(Stream stream, Chordmap?[] chordmaps)
	{
		using StreamWriter writer = new(stream, leaveOpen: true);
		writer.WriteLine(string.Join(",", new[] { "Hexadecimal Chord", "Hexadecimal Phrase", "Chord Actions", "Phrase ASCII" }));
		foreach (var chordmap in chordmaps.WhereNotNull())
		{
			var chordActions = string.Join(" + ", chordmap.ChordActions);
			var line = string.Join(",", new[] { chordmap.HexChord, chordmap.HexPhrase, chordActions, chordmap.AsciiPhrase });
			writer.WriteLine(line);
		}
	}

	public static Chordmap[] Read(string filepath)
	{
		using var file = File.OpenRead(filepath);
		return Read(file);
	}
	public static Chordmap[] Read(Stream stream)
	{
		using StreamReader reader = new(stream, leaveOpen: true);
		var header = reader.ReadLine();
		List<Chordmap> chordmaps = new();
		while (!reader.EndOfStream)
		{
			var line = reader.ReadLine();
			var split = line?.Split(',');
			var (chord, phrase) = (split?[0], split?[1]);
			if (chord is null || phrase is null) continue;
			var chordmap = FromHex(chord, phrase);
			chordmaps.Add(chordmap);
		}
		return chordmaps.ToArray();
	}
	#endregion Serialization

	public override bool Equals(object? obj) => Equals(obj as Chordmap);

	public bool Equals(Chordmap? other)
	{
		return other is not null &&
					 HexChord == other.HexChord &&
					 HexPhrase == other.HexPhrase;
	}

	public override int GetHashCode() => HashCode.Combine(HexChord, HexPhrase);
}

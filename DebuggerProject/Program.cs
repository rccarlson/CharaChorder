namespace CharaChorderInterface;

internal class Program
{
	static void Main(string[] args)
	{
		var actionmap = Maps.ActionMap;
		var ports = CharaChorder.GetSerialPorts();
		using var cc = CharaChorder.FromSerial("COM3");
		if (cc is null) throw new NullReferenceException(nameof(cc));

		var chordCount = cc?.GetChordmapCount();
		var id = cc?.GetID();
		var version = cc?.GetVersion();

		//var chords = ReadAllChords(cc);
		//var test = chords.Where(chrd => chrd.HumanReadable.Contains("i + f + c")).ToArray();

		var testChord = cc.GetChordmapByIndex(23);

		for (int i = 0; i < 89; i++)
		{
			var keymap = cc.GetKeymap(Keymap.Primary, i);
			Console.WriteLine($"{i}: {keymap}");
		}

		Thread.Sleep(1_000);
	}

	static Chordmap?[] ReadAllChords(CharaChorder cc)
	{
		int count = cc?.GetChordmapCount() ?? 0;
		Chordmap?[] chordmaps = new Chordmap?[count];
		for(ushort i = 0; i < count; i++)
		{
			chordmaps[i] = cc?.GetChordmapByIndex(i);
		}
		return chordmaps;
	}

	static void ChordCreateDeleteTest(CharaChorder cc)
	{
		var newChord = Chordmap.FromAscii("gw".ToCharArray(), "test chord");
		cc.SetChordmap(newChord);

		var retrievedChord = cc?.GetChordmapByChord(newChord.HexChord);
		cc.DeleteChordmap(newChord);

		var retrievedChord2 = cc.GetChordmapByChord(newChord.HexChord);
	}
}
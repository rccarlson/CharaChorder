namespace CharaChorderInterface;

internal class Program
{
	static void Main(string[] args)
	{
		var actionmap = Maps.ActionMap;
		var ports = CharaChorder.GetSerialPorts();
		using var cc = new CharaChorder("COM3");
		if (cc is null) throw new NullReferenceException(nameof(cc));
		cc.LoggingAction = Console.WriteLine;
		cc.Open();

		//var smallChord = Chordmap.FromAscii(new[] { 's', 'h', 'r' }, "Shrek 2");
		////cc.SetChordmap(smallChord);
		//cc.DeleteChordmap(smallChord);

		//return;


// shrek limit: 124 actions, 256 long hex
// lorem ipsum: 125 actions, 250 long hex
		var shrek = CompressChord(new string[] { "s", "h", "r" }, File.ReadAllText(@"C:\Users\Riley\Downloads\Shrek.txt"), 125);
		//cc.SetChordmap(shrek);

		var loremIpsum = CompressChord(new string[] { "l", "i", "p" }, LoremIpsum, 126);

		//cc.SetChordmap(loremIpsumChord);
	}

	static Chordmap CompressChord(IEnumerable<string> chordActions, string phrase, int phraseActionCount)
	{
		var largeChord = Chordmap.FromAscii(chordActions, phrase);
		var compressedChord = Chordmap.FromActions(largeChord.ChordActions, largeChord.PhraseActions.Take(phraseActionCount));
		return compressedChord;
	}

	const string LoremIpsum = """
Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.
""";
}
namespace CharaChorderInterface;

internal class Program
{
	static void Main(string[] args)
	{
		var actionmap = Maps.ActionMap;
		var ports = CharaChorder.GetSerialPorts();
		using var cc = CharaChorder.FromSerial("COM3");
		if (cc is null) throw new NullReferenceException(nameof(cc));
		cc.LoggingAction = Console.WriteLine;
		cc.Open();

		var beeMovieChord = Chordmap.FromAscii(new string[] { "b", "e", "t" }, TheBeeMovie);
	}
}
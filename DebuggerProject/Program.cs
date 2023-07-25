namespace CharaChorderInterface;

internal class Program
{
	static void Main(string[] args)
	{
		var actionmap = Maps.ActionMap;
		var ports = CharaChorder.GetSerialPorts();
		using var cc = CharaChorder.FromSerial("COM3");
		var chordCount = cc?.GetChordmapCount();
		var id = cc?.GetID();
		var version = cc?.GetVersion();
		var chord = cc?.GetChordmapByIndex(23);
		var chordByHex = cc?.GetChordmapByChord(chord.HexChord);

		Thread.Sleep(1_000);
	}
}
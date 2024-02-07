namespace ChordLibraryChecker;
using CharaChorderInterface;
using System.Diagnostics;

internal class Program
{
	const string Disclaimer = $"This tool is a community creation and is NOT affiliated, associated, authorized, " +
		$"endorsed, or otherwise related to CharaChorder. https://www.charachorder.com";

	const ConsoleColor
		DefaultBackground = ConsoleColor.Black,
		DefaultForeground = ConsoleColor.White;

	static readonly string[] CharaChorderDeviceDescriptions = new[]
	{
		"CharaChorder 1", // CC1 v1.1.1
		"TinyUSB CDC", // CCL and CCX v1.1.1
		"CharaChorder One USB Serial", // CC1 v1.1.3
		"CharaChorder Lite USB Serial", // CCL v1.1.3
		"CharaChorder X USB Serial", // CCX v1.1.3
	};

	static Chordmap?[]? ReadAllChords(bool forceMenu)
	{
		var (serialPortName, serialPortDescription) = SelectDevice(forceMenu);
		if (serialPortName == null) return null;
		using var device = new CharaChorder(serialPortName);
		try
		{
			device?.Open();
		}
		catch (UnauthorizedAccessException)
		{
			Console.Error.WriteLine("Unable to open connection to CharaChorder. Is the device open elsewhere?");
			Thread.Sleep(3_000);
			return null;
		}
		if (device?.IsOpen != true)
		{
			Console.Error.WriteLine("Unable to connect to device");
			device?.Dispose();
			return null;
		}

		device.EnableSerialDebugging = false;
		device.EnableSerialLogging = false;
		//var chords = device?.ReadAllFromDevice().Where(x => x is not null).Cast<Chordmap>();
		var chordReadStart = DateTime.Now;
		double GetRemainingSeconds(int total, int current) => (ETAEstimate(chordReadStart, DateTime.Now, 0, total, current) - DateTime.Now).TotalSeconds;
		var chords = device
			?.ReadAllFromDevice(
				(total, current) => Console.Write($"\rReading chords from device ({Math.Round(100d * current / total, 0)}%, {Math.Round(GetRemainingSeconds(total, current), 0)}s) ")
			);
		return chords;
	}

	static void Main(string[] args)
	{
		Console.Title = "Chord Library Checker";
		// display help
		if (ContainsAny(args.Select(a => a.ToLowerInvariant()), "-h", "--help", "/?"))
		{
			Console.WriteLine("EagleBirdman's Chord Tool");
			Console.WriteLine("Not affiliated with CharaChorder");
			Console.WriteLine("Command line arguments:");
			(string, string)[] table = new (string, string)[]
			{
				("--help", "Displays this menu"),
				("--ForceMenu", "Will display the device selection menu, regardless of whether a suitable guess is found"),
			};
			var col1Width = table.Max(entry => entry.Item1.Length);
			foreach (var entry in table)
			{
				Console.WriteLine(entry.Item1.PadRight(col1Width + 3) + entry.Item2);
			}
			return;
		}

		Console.WriteLine(Disclaimer);
		Console.WriteLine();
		selectDevice:
		var chords = ReadAllChords(args.Contains("--ForceMenu"));

		Console.WriteLine();
		var nullChords = chords.Where(c => c is null);
		if (nullChords.Any())
		{
			Console.Error.WriteLine($"{nullChords.Count()}/{chords.Length} chords could not be loaded");
			Console.WriteLine("Press any key to continue...");
			Console.ReadKey(intercept: true);
		}

		Console.Clear();
		Console.WriteLine(Disclaimer);
		Console.WriteLine();
		Console.WriteLine($"Loaded {chords.Length} chords");
		Console.WriteLine("!q to quit");
		Console.WriteLine("!r to reload chords");
		Console.WriteLine();
		startReadLoop:
		Console.Write(">");
		var prompt = Console.ReadLine();
		var trimPrompt = prompt?.Trim();
		var componentChars = trimPrompt?.ToCharArray()?.Distinct().OrderDescending().Select(c => c.ToString());
		var componentCharsString = string.Join(" + ", componentChars?.Select(c => $"{c}").ToArray() ?? Array.Empty<string>());

		// Handle commands
		if (string.IsNullOrWhiteSpace(prompt) || string.IsNullOrWhiteSpace(trimPrompt)) goto startReadLoop;
		else if (trimPrompt == "!q") return; // quit
		else if (trimPrompt == "!r") // reload chords
		{
			goto selectDevice;
		}

		Console.Clear();
		Console.WriteLine($">{prompt}");
		Console.WriteLine();

		// process
		Console.WriteLine("-- Phrases --");
		Console.WriteLine();

		Console.WriteLine("Chords to produce this output:");
		var matchingPhrases = chords.Where(chord => string.Equals(chord?.AsciiPhrase, trimPrompt, StringComparison.CurrentCultureIgnoreCase));
		PrintChords(ConsoleColor.Black, ConsoleColor.White, matchingPhrases);
		Console.WriteLine();

		Console.WriteLine("Chords including this in output:");
		var partialMatchPhrases = chords.Where(chord => chord?.AsciiPhrase?.Contains(trimPrompt, StringComparison.CurrentCultureIgnoreCase) ?? false).Except(matchingPhrases);
		PrintChords(ConsoleColor.White, ConsoleColor.Black, partialMatchPhrases);
		Console.WriteLine();

		Console.WriteLine("-- Chords --");
		Console.WriteLine();

		Console.WriteLine($"Chords using {componentCharsString}");
		var matchingChords = chords.Where(chord => chord?.ChordActions.OrderDescending().SequenceEqual(componentChars, StringComparer.CurrentCultureIgnoreCase) ?? false);
		PrintChords(ConsoleColor.Black, ConsoleColor.White, matchingChords);
		Console.WriteLine();

		Console.WriteLine($"Chords partly using {componentCharsString}");
		var partialMatchChords = chords.Where(chord => chord?.ChordActions.Intersect(componentChars, StringComparer.CurrentCultureIgnoreCase).Count() == componentChars?.Count()).Except(matchingChords);
		PrintChords(ConsoleColor.White, ConsoleColor.Black, partialMatchChords);
		Console.WriteLine();

		Console.WriteLine("-- Hex --");
		Console.WriteLine();

		string chordActions = "Not parsable as chord actions";
		try { chordActions = string.Join(" + ", Chordmap.HexChordToActions(trimPrompt.PadRight(32, '0'))); } catch { }
		Console.WriteLine($"Translation from Chord Hex: {chordActions}");

		string phraseActions = "Not parsable as phrase actions";
		try { phraseActions = string.Join("", Chordmap.HexPhraseToActions(trimPrompt)); } catch { }
		Console.WriteLine($"Translation from Phrase Hex: {phraseActions}");

		string chordHex = "Not parsable as chord hex";
		try { chordHex = Chordmap.ActionsToHexChord(trimPrompt.Distinct().Select(x => x.ToString())); } catch { }
		Console.WriteLine($"Translation to Chord Hex: {chordHex}");

		string phraseHex = "Not parsable as chord hex";
		try { phraseHex = Chordmap.ActionsToHexPhrase(trimPrompt.Select(x => x.ToString())); } catch { }
		Console.WriteLine($"Translation to Phrase Hex: {phraseHex}");

		Console.WriteLine();

		goto startReadLoop;
	}

	static (string? PortName, string? PortDescription) SelectDevice(bool forceDeviceMenu)
	{
		const int MaxChoices = 8;
		int selection = -1;

		while (selection == -1)
		{
			var ports = CharaChorder.GetSerialPorts();
			var recognizedCCDevices = ports
				.Where(port => CharaChorderDeviceDescriptions.Contains(port.Description))
				.OrderBy(port => port.PortName)
				.ToArray();

			if (recognizedCCDevices.Length == 1 && !forceDeviceMenu)
				return recognizedCCDevices.FirstOrDefault();

			var choicesAndActions = ports?
				.Take(MaxChoices)
				.Select<(string? PortName, string? Description), (MenuChoice, Action)>((port, idx) =>
				{
					int oneIndexed = idx + 1;
					ConsoleKey key = GetNumberKey(oneIndexed);
					string display = $"{oneIndexed}) {port.PortName} - {port.Description}";
					return (new MenuChoice(display, key), () => selection = idx);
				})
				.ToArray();

			int optIdx = choicesAndActions.Length;
			var additionalOptions = new (MenuChoice, Action)[]
			{
				(new MenuChoice($"{++optIdx}) Reload", GetNumberKey(optIdx)), () => { }),
				(new MenuChoice($"{++optIdx}) Exit", GetNumberKey(optIdx)), () => Environment.Exit(0)),
			};

			var menuChoices = choicesAndActions.Concat(additionalOptions).ToArray();

			int menuResultIndex = ConsoleMenu.Show("Choose your device:", menuChoices.Select(x => x.Item1).ToArray());
			menuChoices[menuResultIndex].Item2.Invoke();
		}
		return default;

		ConsoleKey GetNumberKey(int number)
		{
			if (number is <= 0 or > 9) throw new ArgumentOutOfRangeException(number.ToString());
			return (ConsoleKey)((int)ConsoleKey.D0 + number);
		}
	}

	static void PrintConsoleColored(object output, ConsoleColor foreground, ConsoleColor background, bool newline = true)
	{
		Console.ForegroundColor = foreground;
		Console.BackgroundColor = background;
		Console.Write(output);
		Console.ForegroundColor = DefaultForeground;
		Console.BackgroundColor = DefaultBackground;
		if (newline) Console.WriteLine();
	}
	static void PrintChords(ConsoleColor foreground, ConsoleColor background, IEnumerable<Chordmap?> chords)
	{
		foreach (var chord in chords)
		{
			if (chord is null) continue;
			PrintConsoleColored(chord.HumanReadable, foreground, background);
		}
	}

	static DateTime ETAEstimate(DateTime startTime, DateTime nowTime, int startValue, int endValue, int nowValue)
	{
		var elapsed = nowTime - startTime;
		var rate = (nowValue - startValue) / elapsed.TotalSeconds;
		var offset = startValue;
		var totalTime = (endValue + offset) / rate;
		if (double.IsInfinity(totalTime)) return DateTime.Now;
		var endTime = startTime + TimeSpan.FromSeconds(totalTime);
		return endTime;
	}

	static bool ContainsAny(IEnumerable<string> collection, params string[] values) => values.Any(collection.Contains);
}

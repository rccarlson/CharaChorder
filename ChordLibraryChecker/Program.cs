﻿namespace ChordLibraryChecker;
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
		"CharaChorder 1",
		"CharaChorder Lite",
		"CharaChorder X",
	};

	static void Main(string[] args)
	{
		// display help
		if(ContainsAny(args.Select(a => a.ToLowerInvariant()), "-h", "--help", "/?"))
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
			foreach(var entry in table)
			{
				Console.WriteLine(entry.Item1.PadRight(col1Width + 3) + entry.Item2);
			}
			return;
		}

		Console.WriteLine(Disclaimer);
		Console.WriteLine();
		selectDevice:
		var (serialPortName, serialPortDescription) = SelectDevice(args.Contains("--ForceMenu"));

		var device = CharaChorder.FromSerial(serialPortName ?? string.Empty);
		try
		{
			device?.Open();
		}catch(UnauthorizedAccessException ex)
		{
			Console.Error.WriteLine("Unable to open connection to CharaChorder. Is the device open elsewhere?");
			Thread.Sleep(5_000);
			Environment.Exit(-1);
		}
		if (device?.IsOpen != true)
		{
			Console.Error.WriteLine("Unable to connect to device");
			goto selectDevice;
		}
		device.EnableSerialDebugging = false;
		device.EnableSerialLogging = false;

		loadChords:
		Action<int, int> fractionalCompletionAction = (total, current) => Console.Write($"\rReading chords from device ({current}/{total})");
		Action<int, int> percentileCompletionAction = (total, current) => Console.Write($"\rReading chords from device ({Math.Round(100d * current / total, 0)}%)");
		var chordReadStart = DateTime.Now;
		double GetRemainingSeconds(int total, int current) => (ETAEstimate(chordReadStart, DateTime.Now, 0, total, current) - DateTime.Now).TotalSeconds;
		var chords = Chordmap.ReadAllFromDevice(device, (total, current) => Console.Write($"\rReading chords from device ({Math.Round(100d * current / total, 0)}%, {Math.Round(GetRemainingSeconds(total, current), 0)}s) "));
		var chordReadEnd = DateTime.Now;
		device.Close();
		device.Dispose();
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
		Console.WriteLine($"Loaded {chords.Length} chords from {serialPortDescription} in {Math.Round((chordReadEnd - chordReadStart).TotalSeconds, 1)} seconds");
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
			//if (device.IsOpen) goto loadChords;
			//else goto selectDevice;
			goto selectDevice;
		}

		Console.Clear();
		Console.WriteLine($">{prompt}");
		Console.WriteLine();

		// process
		Console.WriteLine("-- Phrases --");
		Console.WriteLine();

		Console.WriteLine("Chords to produce this output:");
		var matchingPhrases = chords.Where(chord => chord?.AsciiPhrase == trimPrompt);
		PrintChords(ConsoleColor.Black, ConsoleColor.White, matchingPhrases);
		Console.WriteLine();

		Console.WriteLine("Chords including this in output:");
		var partialMatchPhrases = chords.Where(chord => chord?.AsciiPhrase?.Contains(trimPrompt) ?? false).Except(matchingPhrases);
		PrintChords(ConsoleColor.White, ConsoleColor.Black, partialMatchPhrases);
		Console.WriteLine();

		Console.WriteLine("-- Chords --");
		Console.WriteLine();

		Console.WriteLine($"Chords using {componentCharsString}");
		var matchingChords = chords.Where(chord => chord?.ChordActions.OrderDescending().SequenceEqual(componentChars) ?? false);
		PrintChords(ConsoleColor.Black, ConsoleColor.White, matchingChords);
		Console.WriteLine();

		Console.WriteLine($"Chords partly using {componentCharsString}");
		var partialMatchChords = chords.Where(chord => chord.ChordActions.Intersect(componentChars).Count() == componentChars.Count()).Except(matchingChords);
		PrintChords(ConsoleColor.White, ConsoleColor.Black, partialMatchChords);
		Console.WriteLine();

		goto startReadLoop;
	}

	static (string? PortName, string? PortDescription) SelectDevice(bool forceDeviceMenu)
	{
		var ports = CharaChorder.GetSerialPorts();
		var recognizedCCDevices = ports.Where(port => CharaChorderDeviceDescriptions.Contains(port.Description)).ToArray();

		if (recognizedCCDevices.Length == 1 && !forceDeviceMenu)
		{
			return recognizedCCDevices.FirstOrDefault();
		}
		else
		{
			const int MaxChoices = 8;
			var menuChoices = ports.Take(MaxChoices).Select((port, idx) =>
			{
				int oneIndexed = idx + 1;
				ConsoleKey key = GetNumberKey(oneIndexed);
				string display = $"{oneIndexed}) {port.PortName} - {port.Description}";
				return new MenuChoice(display, key);
			}).ToArray();
			var exitOption = new MenuChoice($"{menuChoices.Length + 1}) Exit", GetNumberKey(menuChoices.Length + 1));
			var selection = ConsoleMenu.Show("Choose your device:", menuChoices.Append(exitOption).ToArray());
			if (selection == menuChoices.Length) Environment.Exit(0);
			return ports[selection];
		}

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
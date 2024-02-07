using System;
using System.Diagnostics;
using System.IO.Ports;
using System.Text;
using System.Text.RegularExpressions;
using System.Text.Unicode;
using CharaChorderInterface.Structs;
using CharaChorderInterface.Utility;

namespace CharaChorderInterface;

public class CharaChorder : IDisposable
{
	static readonly int[] VendorIDs = new int[] { 0x239A, 0x303A };
	private Action<string>? _loggingAction = null;
	public Action<string>? LoggingAction
	{
		get => _loggingAction;
		set
		{
			_loggingAction = value;
			_serialManager.LoggingAction = value;
		}
	}
	public bool UsePropertyCaching { get; set; } = true;

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	public DeviceModel DeviceModel => DeviceID.Device switch
	{
		"ONE" => DeviceModel.One,
		"LITE" => DeviceModel.One,
		"X" => DeviceModel.X,
		"ENGINE" => DeviceModel.Engine,
		_ => throw new NotSupportedException(DeviceID.Device),
	};

	private DeviceID? _deviceId = null;
	public DeviceID DeviceID => _deviceId ??= GetID();

	#region CONSTRUCTION
	private readonly SerialManager _serialManager;
	public bool IsOpen => _serialManager is not null && _serialManager.IsOpen;
	public void Open() => _serialManager?.Open();
	public void Close() => _serialManager?.Close();

	public static (string? PortName, string? Description)[] GetSerialPorts()
		=> Utility.SerialUtility.GetAllCOMPorts()
			.Select(port => (port.name, port.bus_description))
			.ToArray();

	/// <summary>
	/// Opens a connection to a CharaChorder device on the specified serial port
	/// </summary>
	/// <param name="serialPortName"></param>
	public CharaChorder(string serialPortName)
	{
		_serialManager = new SerialManager(serialPortName);
	}
	#endregion CONSTRUCTION

	#region DEVICE INFO
	public DeviceID GetID()
	{
		var result = QueryWithEcho("ID");
		var components = result?.Split(" ");
		return new DeviceID()
		{
			Company = components?[1],
			Device = components?[2],
			Chipset = components?[3],
		};
	}

	public Version GetVersion()
	{
		const int ErrValue = 0;

		var result = QueryWithEcho("VERSION");
		var resultComponents = result?.Split(" ");
		var versionComponents = resultComponents?[1].Split(".");
		var version = new Version(
			major: tryParseInt(versionComponents?[0]),
			minor: tryParseInt(versionComponents?[1]),
			build: tryParseInt(versionComponents?[2])
			);
		return version;

		static int tryParseInt(string? str)
		{
			if (str is null)
				return ErrValue;
			else if (int.TryParse(str, out var value))
				return value;
			else
				return ErrValue;
		}
	}
	#endregion DEVICE INFO

	#region CHORD MANAGEMENT
	public int? GetChordmapCount()
	{
		var response = QueryWithEcho("CML C0");
		var countStr = response?.Split(" ")[2];
		if (!int.TryParse(countStr, out var chordCount))
			throw new InvalidDataException($"Could not parse device response: '{countStr}' to int");
		return chordCount;
	}

	public Chordmap? GetChordmapByIndex(ushort index)
	{
		var response = QueryWithEcho($"CML C1 {index}");
		var split = response?.Split(" ");
		var chord = split?[3];
		var phrase = split?[4];
		var ok = split?[5];
		if (ok != "0") return null;
		if (chord is null || phrase is null) throw new InvalidDataException($"Null chord or phrase");
		return Chordmap.FromHex(chord, phrase);
	}

	public Chordmap? GetChordmapByChord(string hexChord)
	{
		var response = QueryWithEcho($"CML C2 {hexChord}");
		var split = response?.Split(" ");
		var chord = split?[2];
		var phrase = split?[3];
		if (split?.Length != 5) return null; // occurs if chord not found
		var ok = split?[4];
		if (ok != "0") return null;
		if (chord is null || phrase is null) throw new InvalidDataException($"Null chord or phrase");
		return Chordmap.FromHex(chord, phrase);
	}

	public void SetChordmap(Chordmap chord)
	{
		var response = QueryWithEcho($"CML C3 {chord.HexChord} {chord.HexPhrase}");
		var split = response?.Split(" ");
		var ok = split?[4];
		if (ok != "0") throw new InvalidDataException($"Chord creation failed. Code: {ok}");
	}

	public void DeleteChordmap(Chordmap chord) => DeleteChordmap(chord.HexChord);

	public void DeleteChordmap(IEnumerable<string> actions) => DeleteChordmap(Chordmap.ActionsToHexChord(actions));
	internal void DeleteChordmap(string hex)
	{
		var response = QueryWithEcho($"CML C4 {hex}");
		var split = response?.Split(" ");
		var ok = split?[3];
		if (ok != "0") throw new InvalidDataException($"Chord deletion failed. Code: {ok}");
	}

	public Chordmap?[] ReadAllFromDevice(Action<int, int>? callback = null)
	{
		var count = GetChordmapCount() ?? 0;
		var chords = new Chordmap?[count];
		for (int i = 0; i < count; i++)
		{
			chords[i] = GetChordmapByIndex((ushort)i);
			if (i % 3 == 0) callback?.Invoke(count, i);
		}
		return chords;
	}

	/// <summary>
	/// Switches the library of the device.
	/// If an existing library is not provided, the device will be queried for the full list, which will take some time.
	/// </summary>
	/// <param name="delta">
	/// If <see langword="true"/>, will send only commands required to change between libraries.
	/// <para/>
	/// If <see langword="false"/>, will fully clear all chordmaps from the device and load in all new chords.
	/// </param>
	public void LoadLibrary(Chordmap[]? oldChords, Chordmap[] newChords, bool delta = true)
	{
		oldChords ??= ReadAllFromDevice().WhereNotNull().ToArray();
		if (delta)
		{
			// delete only what's necessary, then add only what's necessary
			var (toRemove, toAdd) = GetChordmapDelta(oldChords, newChords);
			foreach (var chord in toRemove)
				DeleteChordmap(chord);
			foreach (var chord in toAdd)
				SetChordmap(chord);
		}
		else
		{
			// delete all, then load all
			foreach (var chord in oldChords)
				DeleteChordmap(chord);
			foreach (var chord in newChords)
				SetChordmap(chord);
		}
	}

	/// <summary>
	/// Get the chords to remove from and add to the device to get the target chordmap
	/// </summary>
	private static (Chordmap[] ToRemove, Chordmap[] ToAdd) GetChordmapDelta(IEnumerable<Chordmap> oldChordmap, IEnumerable<Chordmap> newChordmap)
	{
		var toRemove = oldChordmap.Except(newChordmap).ToArray();
		var toAdd = newChordmap.Except(oldChordmap).ToArray();
		return (toRemove, toAdd);
	}
	#endregion CHORD MANAGEMENT

	#region RESET
	public void Reset(ResetType resetType)
	{
		var subcommand = resetType switch
		{
			ResetType.Restart => "RESTART",
			ResetType.Factory => "FACTORY",
			ResetType.Bootloader => "BOOTLOADER",
			ResetType.Params => "PARAMS",
			ResetType.Keymaps => "KEYMAPS",
			ResetType.Starter => "STARTER",
			ResetType.ClearCml => "CLEARCML",
			ResetType.UpgradeCml => "UPGRADECML",
			ResetType.Func => "FUNC",
			ResetType.Compound => "COMPOUND",
			// Do not implement DUMPPAGE here, as it requires arguments. See DumpPage()
			_ => throw new NotImplementedException(resetType.ToString())
		};
		_ = QueryWithEcho($"RST {subcommand}");
	}

	/// <summary> A debugging function that flushes portions of the device memory. This is not documented or supported. </summary>
	/// <param name="page"> Page number </param>
	public string? DumpPage(int page)
	{
		if (page < 0) throw new ArgumentOutOfRangeException(nameof(page));
		var response = QueryWithEcho($"RST DUMPPAGE {page}");
		return response?.Split(' ')[3];
	}
	#endregion RESET

	#region PARAMETERS
	/// <summary>
	/// Prefixes output with a number code so you can figure out what type of value you read from serial - gekko
	/// </summary>
	[DebuggerBrowsable(DebuggerBrowsableState.Never)] public bool EnableSerialHeader { get => GetBoolParameter("1"); set => SetBoolParameter("1", value); }
	/// <summary>
	/// Dumps information when user interacts with device. Can be used to find what a user is doing when trying to create chords
	/// </summary>
	[DebuggerBrowsable(DebuggerBrowsableState.Never)] public bool EnableSerialLogging { get => GetBoolParameter("2"); set => SetBoolParameter("2", value); }
	/// <summary>
	/// Dumps information when user interacts with device. Can be used to find what a user is doing when trying to create chords
	/// </summary>
	[DebuggerBrowsable(DebuggerBrowsableState.Never)] public bool EnableSerialDebugging { get => GetBoolParameter("3"); set => SetBoolParameter("3", value); }
	[DebuggerBrowsable(DebuggerBrowsableState.Never)] public bool EnableSerialRaw { get => GetBoolParameter("4"); set => SetBoolParameter("4", value); }
	/// <summary> CharaChorder device will write the chord hex into the log every time a chord is successfully performed </summary>
	[DebuggerBrowsable(DebuggerBrowsableState.Never)] public bool EnableSerialChord { get => GetBoolParameter("5"); set => SetBoolParameter("5", value); }
	[DebuggerBrowsable(DebuggerBrowsableState.Never)] public bool EnableSerialKeyboard { get => GetBoolParameter("6"); set => SetBoolParameter("6", value); }
	[DebuggerBrowsable(DebuggerBrowsableState.Never)] public bool EnableSerialMouse { get => GetBoolParameter("7"); set => SetBoolParameter("7", value); }
	[DebuggerBrowsable(DebuggerBrowsableState.Never)] public bool EnableUsbHidKeyboard { get => GetBoolParameter("11"); set => SetBoolParameter("11", value); }
	[DebuggerBrowsable(DebuggerBrowsableState.Never)] public bool EnableCharacterEntry { get => GetBoolParameter("12"); set => SetBoolParameter("12", value); }
	[DebuggerBrowsable(DebuggerBrowsableState.Never)] public bool GUI_CTRLSwapMode { get => GetBoolParameter("13"); set => SetBoolParameter("13", value); }
	[DebuggerBrowsable(DebuggerBrowsableState.Never)] public int KeyScanDuration { get => GetIntParameter("14"); set => SetIntParameter("14", value); }
	[DebuggerBrowsable(DebuggerBrowsableState.Never)] public int KeyDebouncePressDuration { get => GetIntParameter("15"); set => SetIntParameter("15", value); }
	[DebuggerBrowsable(DebuggerBrowsableState.Never)] public int KeyDebounceReleaseDuration { get => GetIntParameter("16"); set => SetIntParameter("16", value); }
	[DebuggerBrowsable(DebuggerBrowsableState.Never)] public int KeyboardOutputCharacterMicrosecondDelays { get => GetIntParameter("17"); set => SetIntParameter("17", value); }
	[DebuggerBrowsable(DebuggerBrowsableState.Never)] public bool EnableUsbHidMouse { get => GetBoolParameter("21"); set => SetBoolParameter("21", value); }
	[DebuggerBrowsable(DebuggerBrowsableState.Never)] public int SlowMouseSpeed { get => GetIntParameter("22"); set => SetIntParameter("22", value); }
	[DebuggerBrowsable(DebuggerBrowsableState.Never)] public int FastMouseSpeed { get => GetIntParameter("23"); set => SetIntParameter("23", value); }
	[DebuggerBrowsable(DebuggerBrowsableState.Never)] public bool EnableActiveMouse { get => GetBoolParameter("24"); set => SetBoolParameter("24", value); }
	[DebuggerBrowsable(DebuggerBrowsableState.Never)] public int MouseScrollSpeed { get => GetIntParameter("25"); set => SetIntParameter("25", value); }
	[DebuggerBrowsable(DebuggerBrowsableState.Never)] public int MousePollDuration { get => GetIntParameter("26"); set => SetIntParameter("26", value); }
	[DebuggerBrowsable(DebuggerBrowsableState.Never)] public bool EnableChording { get => GetBoolParameter("31"); set => SetBoolParameter("31", value); }
	[DebuggerBrowsable(DebuggerBrowsableState.Never)] public bool EnableChordingCharacterCounterTimeout { get => GetBoolParameter("32"); set => SetBoolParameter("32", value); }
	/// <summary> in deciseconds </summary>
	[DebuggerBrowsable(DebuggerBrowsableState.Never)] public int ChordingCharacterCounterTimeoutTimer { get => GetIntParameter("33"); set => SetIntParameter("33", value, 0, 255); }
	[DebuggerBrowsable(DebuggerBrowsableState.Never)] public int ChordDetectionPressTolerance { get => GetIntParameter("34"); set => SetIntParameter("34", value, 1, 50); }
	[DebuggerBrowsable(DebuggerBrowsableState.Never)] public int ChordDetectionReleaseTolerance { get => GetIntParameter("35"); set => SetIntParameter("35", value, 1, 50); }
	[DebuggerBrowsable(DebuggerBrowsableState.Never)] public bool EnableSpurring { get => GetBoolParameter("41"); set => SetBoolParameter("41", value); }
	[DebuggerBrowsable(DebuggerBrowsableState.Never)] public bool EnableSpurringCharacterCounterTimeout { get => GetBoolParameter("42"); set => SetBoolParameter("42", value); }
	[DebuggerBrowsable(DebuggerBrowsableState.Never)] public int SpurringCharacterCounterTimeoutTimer { get => GetIntParameter("43"); set => SetIntParameter("43", value, 0, 255); }
	[DebuggerBrowsable(DebuggerBrowsableState.Never)] public bool EnableArpeggiates { get => GetBoolParameter("51"); set => SetBoolParameter("51", value); }
	[DebuggerBrowsable(DebuggerBrowsableState.Never)] public int ArpeggiateTolerance { get => GetIntParameter("54"); set => SetIntParameter("54", value); }
	//[DebuggerBrowsable(DebuggerBrowsableState.Never)] public bool EnableCompoundChording { get => GetBoolParameter("61"); set => SetBoolParameter("61", value); }
	[DebuggerBrowsable(DebuggerBrowsableState.Never)] public int CompoundTolerance { get => GetIntParameter("64"); set => SetIntParameter("64", value); }
	[DebuggerBrowsable(DebuggerBrowsableState.Never)] public int LEDBrightness { get => GetIntParameter("81"); set => SetIntParameter("81", value, 0, 50); }
	[DebuggerBrowsable(DebuggerBrowsableState.Never)] public int LEDColorCode { get => throw new NotImplementedException("82"); set => throw new NotImplementedException("82"); }
	[DebuggerBrowsable(DebuggerBrowsableState.Never)] public bool EnableLEDKeyHighlight { get => GetBoolParameter("83"); set => SetBoolParameter("83", value); }
	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	public OperatingSystem OperatingSystem
	{
		get
		{
			var osStr = GetParameter("91");
			if (!int.TryParse(osStr, out int osInt)) return OperatingSystem.Unknown;
			return (OperatingSystem)osInt;
		}
		set => SetParameter("91", ((int)value).ToString());
	}
	[DebuggerBrowsable(DebuggerBrowsableState.Never)] public bool EnableRealtimeFeedback { get => GetBoolParameter("92"); set => SetBoolParameter("92", value); }
	[DebuggerBrowsable(DebuggerBrowsableState.Never)] public bool EnableCharaChorderReadyOnStartup { get => GetBoolParameter("93"); set => SetBoolParameter("93", value); }


	private int GetIntParameter(string parameterName) => int.Parse(GetParameter(parameterName));
	private void SetIntParameter(string parameterName, int value) => SetParameter(parameterName, value.ToString());
	private void SetIntParameter(string parameterName, int value, int min, int max) => SetParameter(parameterName, value.Clamp(min, max).ToString());
	private bool GetBoolParameter(string parameterName)
	{
		var response = GetParameter(parameterName);
		return response switch
		{
			"0" => false,
			"1" => true,
			_ => throw new NotImplementedException(response),
		};
	}
	private void SetBoolParameter(string parameterName, bool value)
	{
		var valueStr = value switch
		{
			true => "1",
			false => "0",
		};
		SetParameter(parameterName, valueStr);
	}

	private string GetParameter(string parameterCode)
	{
		if (UsePropertyCaching && _propertyCache.TryGetValue(parameterCode, out var cachedValue) && cachedValue is not null) return cachedValue;
		var response = QueryWithEcho($"VAR B1 {parameterCode}");
		var split = response?.Split(" ");
		var dataOut = _propertyCache[parameterCode] = split?[3];
		var ok = split?[4];
		if (ok != "0") throw new InvalidDataException($"Failed to query parameter {parameterCode}. Exception code: {ok}");
		return dataOut ?? string.Empty;
	}
	private void SetParameter(string parameterCode, string dataIn)
	{
		var response = QueryWithEcho($"VAR B2 {parameterCode} {dataIn}");
		var split = response?.Split(" ");
		var dataOut = _propertyCache[parameterCode] = split?[3];
		var ok = split?[4];
		if (ok != "0") throw new InvalidDataException($"Failed to query parameter {parameterCode}. Exception code: {ok}");
	}

	Dictionary<string, string?> _propertyCache = new();
	public void ResetPropertyCaches() => _propertyCache.Clear();

	public void Commit()
	{
		var response = QueryWithEcho("VAR B0");
		var split = response?.Split(" ");
		var ok = split?[2];
		if (ok != "0") throw new InvalidDataException("Commit failed");
	}
	#endregion PARAMETERS	

	#region KEYMAP
	/// <summary>
	/// Gets the key binding at <paramref name="index"/>.
	/// <para>CC1: 0-89</para>
	/// <para>CCL: 0-66</para>
	/// </summary>
	public string? GetKeymap(KeymapLayer keymap, int index)
	{
		var keymapCode = keymap switch
		{
			KeymapLayer.Primary => "A1",
			KeymapLayer.Num => "A2",
			KeymapLayer.Function => "A3",
			_ => throw new NotImplementedException(),
		};
		var response = QueryWithEcho($"VAR B3 {keymapCode} {index}");
		var split = response?.Split(" ");
		var actionIdStr = split?[4];
		var ok = split?[5];
		if (ok != "0") throw new InvalidDataException($"Query failed. Error code: {ok}");
		var actionID = int.Parse(actionIdStr ?? string.Empty);
		var action = Maps.ActionMap[actionID];
		return action;
	}
	#endregion KEYMAP

	#region RAM
	/// <summary>
	/// Returns the current number of bytes available in SRAM. This is useful for debugging when there is a suspected heap or stack issue.
	/// </summary>
	public int? GetRam()
	{
		var response = QueryWithEcho("RAM");
		var split = response?.Split(" ");
		var bytesAvailable = split?[1];
		if (int.TryParse(bytesAvailable, out var result)) return result;
		else return null;
	}
	#endregion RAM

	/// <summary>
	/// Provides a way to inject a chord or key states to be processed by the device. This is primarily used for debugging.
	/// Returns the output phrase
	/// </summary>
	/// <exception cref="ArgumentOutOfRangeException"></exception>
	public string? SimChord(string chordHex)
	{
		if (chordHex?.Length != 32) throw new ArgumentOutOfRangeException(nameof(chordHex), $"Length was {chordHex?.Length}. Expected 32");
		var response = QueryWithEcho($"SIM CHORD {chordHex}");
		var split = response?.Split(" ");
		var generatedPhrase = split?[3];
		return generatedPhrase;
	}


	#region SERIAL
	public void Send(string query) => _serialManager.Send(query);

	private static readonly Regex queryResponseHeaderRegex = new(@"(?:(\d{2}) )?([A-Z]+.*)", RegexOptions.Compiled | RegexOptions.NonBacktracking);
	public string? QueryWithEcho(string query)
	{
		var response = _serialManager.Query(query, @$"(?:([0-9+]+) )?({query}.*)", true);
		if (response is null) return null;
		var headerMatch = queryResponseHeaderRegex.Match(response);
		var parsedHeader = headerMatch?.Groups?[1]?.Value;
		var parsedHesponse = headerMatch?.Groups?[2]?.Value;
		var responseType = GetResponseType(parsedHeader);
		return parsedHesponse;
	}

	private static SerialResponseType? GetResponseType(string? header) => header switch
	{
		null or "" => null,
		"01" => SerialResponseType.QueryResponse,
		"30" => SerialResponseType.Chord,
		"60" => SerialResponseType.Logging,
		_ => throw new NotImplementedException(header),
	};
	#endregion SERIAL

	public void Dispose()
	{
		if (_serialManager is not null)
		{
			_serialManager.Dispose();
		}
		GC.SuppressFinalize(this); // CA1816
	}
}
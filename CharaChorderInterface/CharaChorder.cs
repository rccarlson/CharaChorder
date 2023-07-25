using System.IO.Ports;
using System.Text;
using System.Text.Unicode;
using CharaChorderInterface.Structs;

namespace CharaChorderInterface;

public class CharaChorder : IDisposable
{
	const int BaudRate = 115_200;
	static readonly int[] VendorIDs = new int[] { 0x239A, 0x303A };
	public Action<string> Log = Console.WriteLine;

	#region CONSTRUCTION
	private SerialPort? _port = null;
	public bool IsOpen => _port is not null && _port.IsOpen;

	public static (string? PortName, string? Description)[] GetSerialPorts()
		=> Utility.SerialUtility.GetAllCOMPorts()
			.Select(port => (port.name, port.bus_description))
			.ToArray();

	private CharaChorder() { }

	/// <summary>
	/// Build a <see cref="CharaChorder"/> from a serial port
	/// </summary>
	/// <param name="serialPortName"></param>
	/// <returns></returns>
	public static CharaChorder? FromSerial(string serialPortName)
	{
		var cc = new CharaChorder()
		{
			_port = new SerialPort(serialPortName, baudRate: BaudRate)
			{
				DtrEnable = true,
				ReadTimeout = 5_000,
			}
		};

		//cc._port.DataReceived += (object sender, SerialDataReceivedEventArgs e) =>
		//{
		//	Console.WriteLine("Data received");
		//};
		cc._port.PinChanged += (object sender, SerialPinChangedEventArgs e) =>
		{
			Console.WriteLine("Pin changed");
		};

		try { cc._port.Open(); }
		catch (UnauthorizedAccessException ex) { ThrowUnauthorized(ex); }
		if (!cc._port.IsOpen) ThrowUnauthorized();
		return cc;

		void ThrowUnauthorized(UnauthorizedAccessException? ex = null)
		{
			cc?.Dispose();
			throw new UnauthorizedAccessException($"Unable to open port {serialPortName}. Is it open elsewhere?", ex);
		}
	}
	#endregion CONSTRUCTION

	#region DEVICE INFO
	public DeviceID GetID()
	{
		var result = Query("ID");
		var components = result?.Split(" ");
		var command = components?[0];
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

		var result = Query("VERSION");
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
		var response = Query("CML C0");
		var countStr = response?.Split(" ")[2];
		if (!int.TryParse(countStr, out var chordCount))
			throw new InvalidDataException($"Could not parse device response: '{countStr}' to int");
		return chordCount;
	}

	public Chordmap? GetChordmapByIndex(ushort index)
	{
		var response = Query($"CML C1 {index}");
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
		var response = Query($"CML C2 {hexChord}");
		var split = response?.Split(" ");
		var chord = split?[2];
		var phrase = split?[3];
		var ok = split?[4];
		if (ok != "0") return null;
		if (chord is null || phrase is null) throw new InvalidDataException($"Null chord or phrase");
		return Chordmap.FromHex(chord, phrase);
	}

	#endregion CHORD MANAGEMENT

	private string? Query(string query)
	{
		if (_port is null || !_port.IsOpen)
			throw new InvalidOperationException("Cannot execute query: No connection established");
		Log($"Sending: '{query}'");
		var bytes = UTF8Encoding.UTF8.GetBytes(query + "\r\n");
		_port?.Write(bytes, 0, bytes.Length);
		var result = _port?.ReadTo("\r\n");
		return result;
	}


	public void Dispose()
	{
		if (_port is not null)
		{
			if (_port.IsOpen) _port.Close();
			_port.Dispose();
		}
		GC.SuppressFinalize(this); // CA1816
	}
}
using System.IO.Ports;
using System.Text;
using System.Text.Unicode;
using CharaChorder.Structs;

namespace CharaChorder;

public class CharaChorder : IDisposable
{
	const int BaudRate = 115_200;
	static readonly int[] VendorIDs = new int[] { 0x239A, 0x303A };
	public Action<string> Log = Console.WriteLine;

	private SerialPort? _port = null;
	private bool PortIsOpen => _port is not null && _port.IsOpen;

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
			_port = new SerialPort(serialPortName, baudRate: BaudRate) { 
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

	public int? GetChordCount()
	{
		const string ChordCountCommand = "CML C0";
		var response = Query(ChordCountCommand);
		var responsePortions = response?.Split(" ");
		var countStr = responsePortions?[2];
		if (!int.TryParse(countStr, out var chordCount))
			throw new InvalidDataException($"Could not parse device response: '{countStr}' to int");
		return chordCount;
	}

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

	private string? Query(string query)
	{
		Log($"Sending: '{query}'");
		var bytes = UTF8Encoding.UTF8.GetBytes(query + "\r\n");
		_port?.Write(bytes, 0, bytes.Length);
		return _port?.ReadTo("\r\n");
	}

	public void Dispose()
	{
		if(_port is not null)
		{
			if(_port.IsOpen) _port.Close();
			_port.Dispose();
		}
		GC.SuppressFinalize(this); // CA1816
	}
}
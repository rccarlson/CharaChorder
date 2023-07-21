using System.IO.Ports;

namespace CharaChorder;

public class CharaChorder : IDisposable
{
	const int BaudRate = 115_200;
	const int VendorID = 0x239a;

	private SerialPort? _port = null;

	public static (string? PortName, string? Description)[] GetSerialPorts()
		=> Utility.SerialUtility.GetAllCOMPorts()
			.Select(port => (port.name, port.bus_description))
			.ToArray();

	private CharaChorder() { }

	/// <summary>
	/// Build a <see cref="CharaChorder"/> from 
	/// </summary>
	/// <param name="serialPortName"></param>
	/// <returns></returns>
	public static CharaChorder? FromSerial(string serialPortName)
	{
		var cc = new CharaChorder()
		{
			_port = new SerialPort(serialPortName, baudRate: BaudRate)
		};
		try { cc._port.Open(); }
		catch (UnauthorizedAccessException ex) { ThrowUnauthorized(ex); }
		if (!cc._port.IsOpen) ThrowUnauthorized();
		return cc;

		void ThrowUnauthorized(UnauthorizedAccessException? ex = null)
		{
			cc.Dispose();
			throw new UnauthorizedAccessException($"Unable to open port {serialPortName}. Is it open elsewhere?", ex);
		}
	}

	public void Dispose()
	{
		if(_port is not null)
		{
			if(_port.IsOpen) _port.Close();
			_port.Dispose();
		}
	}
}
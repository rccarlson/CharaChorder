using System.IO.Ports;
using System.Management;

namespace CharaChorder;

public class CharaChorder
{
	const int BaudRate = 115200;

	public static CharaChorder FromSerial()
	{
		var all = SerialUtility.GetAllCOMPorts();
		var portNames = SerialPort.GetPortNames();
		
		foreach(var portName in portNames)
		{
			var port = new SerialPort(portName, baudRate: BaudRate);
		}

		return default;
	}
}
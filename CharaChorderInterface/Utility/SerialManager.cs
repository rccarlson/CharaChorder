using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.IO.Ports;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CharaChorderInterface.Utility;

//public record SerialListener(Predicate<string> IsValidTarget, Action<SerialPort> ReadAction);

public record SerialEventArgs(string Message);
public delegate void SerialLogMessageHandler(object sender, SerialEventArgs e);
public record KeyPressEventArgs(string Key, bool IsPressed);
public delegate void KeyPressEventHandler(object sender, KeyPressEventArgs e);

public class SerialManager : IDisposable
{
	const int BaudRate = 115_200;
	const string NewLine = "\r\n";

	public SerialManager(string portName)
	{
		Port = new SerialPort(portName)
		{
			DtrEnable = true,
			ReceivedBytesThreshold = 1, // do not try to hold onto a lot of data before flushing it
			ReadTimeout = 5_000,
			BaudRate = BaudRate,
		};
		Port.DataReceived += OnDataReceived;
		OnUnhandledSerialMessage += (sender, args) =>
		{
			var match = Regex.Match(args.Message, @"(?:(\d{2}) )?Actions_::trigger\((\d+),(\d+)\)");
			if (match.Success)
			{
				var key = match.Groups[2].Value;
				var isPressed = match.Groups[3].Value switch
				{
					"1" => true,
					"0" => false,
					_ => throw new InvalidDataException(match.Groups[3].Value)
				};
				var keyPressArgs = new KeyPressEventArgs(key, isPressed);
				OnKeyPressChange?.Invoke(this, keyPressArgs);
			}
		};

		OnKeyPressChange += (sender, args) =>
		{
			Console.WriteLine($"{args.Key} is pressed: {args.IsPressed}");
		};
	}
	public void Dispose()
	{
		if (Port is not null)
		{
			Port.DataReceived -= OnDataReceived;
			Port.Dispose();
		}
	}

	private object _serialLock = new object();
	internal SerialPort Port { get; }

	/// <summary>
	/// Amount of time (in milliseconds) to wait for the response for a query.
	/// Note that this can be very slow if the CharaChorder is outputting a lot of debug information
	/// </summary>
	public int QueryTimeoutMs { get; set; } = 3_000;
	public int QueryIdleTimeoutMs { get; set; } = 100;

	public void Open() => Port?.Open();
	public void Close() => Port?.Close();
	public bool IsOpen => Port.IsOpen;

	private void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
	{
		// todo: remove string concatenation/substrings
		//lastReceivedCommandTime = DateTime.Now;
		var incoming = Port.ReadExisting();
		partialIncomingLine += incoming;
		var built = partialIncomingLine.ToString();
		while (built.Contains(NewLine))
		{
			var idx = built.IndexOf(NewLine);
			var str = built[..idx];
			built = built[(idx + NewLine.Length)..];
			//Console.WriteLine($"Completed line: {str}");
			if (IsProcessingCommand)
			{
				_incomingSerialQueue.Enqueue(str);
			}
			else
				OnUnhandledSerialMessage.Invoke(this, new SerialEventArgs(str));
		}
		partialIncomingLine = built;
	}
	private string partialIncomingLine = string.Empty;
	//private DateTime lastReceivedCommandTime = DateTime.MinValue;
	private Queue<string> _incomingSerialQueue = new();
	private bool IsProcessingCommand = false;

	public void Send(string query)
	{
		lock (_serialLock)
		{
			Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss:fff")} Sending: '{query}'... ");

			var bytes = Encoding.UTF8.GetBytes(query + NewLine);
			Port?.Write(bytes, 0, bytes.Length);
		}
	}

	public string? Query(string query, [StringSyntax(StringSyntaxAttribute.Regex)] string resultRegex, bool awaitReply)
	{
		// TODO: this is jank, but it works
		const int RetryDelayMs = 5;
		lock (_serialLock)
		{
			string? response = null;
			try
			{
				IsProcessingCommand = true;

				var bytes = Encoding.UTF8.GetBytes(query + "\r\n");
				Port?.Write(bytes, 0, bytes.Length);
				if (!awaitReply) return null;

				var start = DateTime.Now;
				start:
				var elapsed = (DateTime.Now - start).TotalMilliseconds;
				//var idleTime = (DateTime.Now - lastReceivedCommandTime).TotalMilliseconds;
				if (elapsed > QueryTimeoutMs)
					return null; // timeout. give up

				while (_incomingSerialQueue.Any())
				{
					var incoming = _incomingSerialQueue.Dequeue();
					if (Regex.IsMatch(incoming, resultRegex))
					{
						return response = incoming;
					}
					else
					{
						OnUnhandledSerialMessage.Invoke(this, new SerialEventArgs(incoming));
					}
				}
				Thread.Sleep(RetryDelayMs);
				goto start;
			}
			finally
			{
				IsProcessingCommand = false;
				Console.WriteLine($"'{query}' -> '{response}'");
			}
		}
	}

	public event SerialLogMessageHandler OnUnhandledSerialMessage;
	public event KeyPressEventHandler OnKeyPressChange;
}

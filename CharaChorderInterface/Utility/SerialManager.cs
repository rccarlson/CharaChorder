using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
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

public record ChordEnteredEventArgs(string ChordHex);
public delegate void ChordEnteredHandler(object sender, ChordEnteredEventArgs e);

public class SerialManager : IDisposable
{
	const int BaudRate = 115_200;
	const string NewLine = "\r\n";

	public Action<string>? LoggingAction { get; set; }

	public SerialManager(string portName)
	{
		Port = new SerialPort(portName)
		{
			DtrEnable = true,
			ReceivedBytesThreshold = 1, // do not try to hold onto data before flushing it
			ReadTimeout = 5_000,
			BaudRate = BaudRate,
		};
		Port.DataReceived += OnDataReceived;
		OnUnhandledSerialMessage += (sender, args) => LoggingAction?.Invoke($"log: {args.Message}");
		// configure OnKeyPressChange listener
		OnUnhandledSerialMessage += (sender, args) =>
		{
			var match = Regex.Match(args.Message, @"(?:(\d{2}) )?Actions_::trigger\((\d+),(\d+)\)"); // looks for Actions_::trigger(), possibly preceeded by a header
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
		// configure OnChordEntered listener
		OnUnhandledSerialMessage += (sender, args) =>
		{
			var match = Regex.Match(args.Message, @"^(?:(\d{2}) )?([A-F0-9]{32})$"); // looks for a chord, possibly preceeded by a header
			if(match.Success)
			{
				var chord = match.Groups[2].Value;
				var eventArgs = new ChordEnteredEventArgs(chord);
				OnChordEntered?.Invoke(this, eventArgs);
			}
		};

		OnKeyPressChange += (sender, args) =>
		{
			var state = args.IsPressed ? "pressed" : "released";
			LoggingAction?.Invoke($"{args.Key} is {state}");
		};
		OnChordEntered += (sender, args) =>
		{
			var chord = args.ChordHex;
			var phraseActions = Chordmap.HexChordToActions(args.ChordHex);
			var phrase = string.Join("", phraseActions);
			LoggingAction?.Invoke($"Chord entered: {chord} -> {phrase}");
		};
	}
	public void Dispose()
	{
		if (Port is not null)
		{
			Port.DataReceived -= OnDataReceived;
			Port.Dispose();
		}
		GC.SuppressFinalize(this);
	}

	private object _serialLock = new object();
	internal SerialPort Port { get; }

	/// <summary> Amount of time (in milliseconds) to wait for the response for a query </summary>
	public int QueryTimeoutMs { get; set; } = 3_000;
	/// <summary> Time to wait for CharaChorder to respond while it sits idle </summary>
	/// <remarks>
	/// For example, if the CharaChorder is outputting large amounts of debug information, it may
	/// take some time to reply to your query. Queries should not consider the request dead unless
	/// the CharaChorder has left the line idle for <see cref="QueryIdleTimeoutMs"/> milliseconds.
	/// </remarks>
	public int QueryIdleTimeoutMs { get; set; } = 100;

	/// <summary> Opens a serial connection </summary>
	public void Open() => Port?.Open();
	/// <summary> Closes the active serial connection </summary>
	public void Close() => Port?.Close();
	public bool IsOpen => Port?.IsOpen ?? false;

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
			if (Debugger.IsAttached) Debugger.Log(0, "Serial", $"{DateTime.Now.ToString("HH:mm:ss:fff")} Received: {str}{Environment.NewLine}");
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

	/// <summary>
	/// Fires and forgets a command to the CharaChorder
	/// </summary>
	public void Send(string query)
	{
		lock (_serialLock)
		{
			if (Debugger.IsAttached) Debugger.Log(0, "Serial", $"{DateTime.Now.ToString("HH:mm:ss:fff")} Sending: {query}{Environment.NewLine}");
			LoggingAction?.Invoke($"{DateTime.Now.ToString("HH:mm:ss:fff")} Sending: '{query}'... ");

			var bytes = Encoding.UTF8.GetBytes(query + NewLine);
			Port?.Write(bytes, 0, bytes.Length);
		}
	}

	/// <summary>
	/// Sends the <paramref name="query"/> to the CharaChorder and awaits a reply in the format defined by <paramref name="resultRegex"/> if <paramref name="awaitReply"/> is true.
	/// </summary>
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

				if (Debugger.IsAttached) Debugger.Log(0, "Serial", $"{DateTime.Now.ToString("HH:mm:ss:fff")} Sending: {query}{Environment.NewLine}");

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
				LoggingAction?.Invoke($"'{query}' -> '{response}'");
			}
		}
	}

	/// <summary>
	/// Triggered upon receiving a message that is not in response to an API request.
	/// </summary>
	public event SerialLogMessageHandler OnUnhandledSerialMessage;

	/// <summary>
	/// Requires <see cref="CharaChorder.EnableSerialDebugging"/> or <see cref="CharaChorder.EnableSerialLogging"/> to be <see langword="true"/>.
	/// Otherwise, no events will be triggered.
	/// </summary>
	public event KeyPressEventHandler OnKeyPressChange;

	/// <summary>
	/// Requires <see cref="CharaChorder.EnableSerialChord"/> to be <see langword="true"/>.
	/// Otherwise, no events will be triggered.
	/// </summary>
	public event ChordEnteredHandler OnChordEntered;
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CharaChorderInterface;

[DebuggerDisplay("{Layer,nq} {Index,nq} {Action,nq}")]
public class KeymapEntry
{
	public KeymapEntry(KeymapLayer layer, int index, string actionCode)
	{
		this.Layer = layer;
		this.Index = index;
		this.Action = actionCode;
	}
	public KeymapEntry(KeymapLayer layer, int index, CharaChorder cc)
	{
		this.Layer = layer;
		this.Index = index;
		this.Action = cc.GetKeymap(layer, index) ?? string.Empty;
	}

	public KeymapLayer Layer { get; }
	public int Index { get; }
	public string Action { get; }
}


public class Keymap
{
	private static int GetMaxIndex(CharaChorder device) => device.DeviceModel switch
	{
		DeviceModel.One => 89,
		DeviceModel.Lite => 66,
		_ => throw new NotImplementedException(device.DeviceModel.ToString())
	};
	private static KeymapLayer[] GetLayersForDevice(CharaChorder device)
		=> device.DeviceModel switch
		{
			DeviceModel.One => new[] { KeymapLayer.Primary, KeymapLayer.Num, KeymapLayer.Function },
			DeviceModel.Lite => new[] { KeymapLayer.Primary, KeymapLayer.Num },
			_ => throw new NotImplementedException(device.DeviceModel.ToString()),
		};

	public KeymapEntry[] Entries;

	public static Keymap ReadFromDevice(CharaChorder charaChorder)
	{
		return new Keymap()
		{
			Entries = ReadKeymap(charaChorder).ToArray()
		};
	}
	public static Keymap ReadFromCSV(string filepath)
	{
		throw new NotImplementedException();
	}

	private static IEnumerable<KeymapEntry> ReadKeymap(CharaChorder cc)
	{
		var maxIndex = GetMaxIndex(cc);
		var indexes = Enumerable.Range(0, maxIndex);
		var layers = GetLayersForDevice(cc);

		foreach (var layer in layers)
		{
			foreach (var index in indexes)
			{
				yield return new KeymapEntry(layer, index, cc);
			}
		}
	}
}
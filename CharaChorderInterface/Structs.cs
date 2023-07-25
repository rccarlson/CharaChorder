using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CharaChorderInterface.Structs;

public readonly record struct DeviceID(string? Company, string? Device, string? Chipset);

public readonly record struct Chordmap(int Index, long Chord, CCActionCodes[] ActionCodes);

public readonly record struct CCActionCodes(long ActionCode);
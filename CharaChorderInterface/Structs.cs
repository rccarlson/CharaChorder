using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CharaChorder.Structs;

public record struct DeviceID(string? Company, string? Device, string? Chipset);

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CharaChorderInterface.Utility;

namespace CharaChorderInterfaceTests;

public class HexBinDecTests
{
	[TestCase("0018C620000000000000000000000000", ExpectedResult = "00000000000110001100011000100000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000")]
	[TestCase("00194620000000000000000000000000", ExpectedResult = "00000000000110010100011000100000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000")]
	[TestCase("001BC6C19C6100000000000000000000", ExpectedResult = "00000000000110111100011011000001100111000110000100000000000000000000000000000000000000000000000000000000000000000000000000000000")]
	[TestCase("001C8691A06700000000000000000000", ExpectedResult = "00000000000111001000011010010001101000000110011100000000000000000000000000000000000000000000000000000000000000000000000000000000")]
	public string HexToBin(string hex) => HexUtility.HexToBin(hex);

	[TestCase("7B", ExpectedResult = "123")]
	[TestCase("1C8", ExpectedResult = "456")]
	[TestCase("EA1016", ExpectedResult = "15339542")]
	public string HexToDec(string hex) => HexUtility.HexToDec(hex);
}

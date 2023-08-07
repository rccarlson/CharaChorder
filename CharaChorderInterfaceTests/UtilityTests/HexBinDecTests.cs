using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CharaChorderInterface.Utility;

namespace CharaChorderInterfaceTests.UtilityTests;

public class HexBinDecTests
{
	[TestCase("0018C620000000000000000000000000", ExpectedResult = "00000000000110001100011000100000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000")]
	[TestCase("00194620000000000000000000000000", ExpectedResult = "00000000000110010100011000100000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000")]
	[TestCase("001BC6C19C6100000000000000000000", ExpectedResult = "00000000000110111100011011000001100111000110000100000000000000000000000000000000000000000000000000000000000000000000000000000000")]
	[TestCase("001C8691A06700000000000000000000", ExpectedResult = "00000000000111001000011010010001101000000110011100000000000000000000000000000000000000000000000000000000000000000000000000000000")]
	public string HexToBin(string hex) => NumberUtility.HexToBin(hex);

	[TestCase("7B", ExpectedResult = "123")]
	[TestCase("1C8", ExpectedResult = "456")]
	[TestCase("EA1016", ExpectedResult = "15339542")]
	public string HexToDec(string hex) => NumberUtility.HexToDec(hex);

	[TestCase(99, "1100011")]
	[TestCase(123, "1111011")]
	[TestCase(420, "110100100")]
	[TestCase(123456789, "111010110111100110100010101")]
	[TestCase(2147483647, "01111111111111111111111111111111")]
	public void IntDecToBin(int dec, string expected)
	{
		var actual = NumberUtility.DecToBin(dec);
		Assert.That(() => actual.EndsWith(expected), Is.True);
		var leftPadding = actual[..^expected.Length];
		Assert.That(() => leftPadding, Is.All.EqualTo('0'));
	}

	[TestCase("010101", ExpectedResult = "15")]
	[TestCase("000100100011", ExpectedResult = "123")]
	[TestCase("01100101011010100101", ExpectedResult = "656A5")]
	[TestCase("110100010101100111100", ExpectedResult = "1A2B3C")]
	[TestCase("00000000000110001100011000100001100001000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000", ExpectedResult = "0018C621840000000000000000000000")]
	public string BinToHex(string bin) => NumberUtility.BinToHex(bin);

	[TestCase("01111011", ExpectedResult = "123")]
	[TestCase("00010110", ExpectedResult = "22")]
	[TestCase("110001100011000100001100001", ExpectedResult = "103909473")]
	public string BinToDec(string bin) => NumberUtility.BinToDec(bin);
}

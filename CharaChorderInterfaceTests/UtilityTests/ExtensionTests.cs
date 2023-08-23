using CharaChorder.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CharaChorderInterfaceTests.UtilityTests;

public class ExtensionTests
{
	[Test]
	public void Clamp()
	{
		Assert.Multiple(() =>
		{
			Assert.That((-1).Clamp(0, 1), Is.EqualTo(0));
			Assert.That(0.Clamp(0, 1), Is.EqualTo(0));
			Assert.That(1.Clamp(0, 1), Is.EqualTo(1));
			Assert.That(2.Clamp(0, 1), Is.EqualTo(1));
		});

		Assert.Multiple(() =>
		{
			Assert.That((-1f).Clamp(0f, 1f), Is.EqualTo(0f));
			Assert.That(0f.Clamp(0f, 1f), Is.EqualTo(0f));
			Assert.That(1f.Clamp(0f, 1f), Is.EqualTo(1f));
			Assert.That(2f.Clamp(0f, 1f), Is.EqualTo(1f));
		});

		Assert.Multiple(() =>
		{
			Assert.That("a".Clamp("b", "d"), Is.EqualTo("b"));
			Assert.That("b".Clamp("b", "d"), Is.EqualTo("b"));
			Assert.That("c".Clamp("b", "d"), Is.EqualTo("c"));
			Assert.That("d".Clamp("b", "d"), Is.EqualTo("d"));
			Assert.That("e".Clamp("b", "d"), Is.EqualTo("d"));
		});
	}

	[TestCase("abcdef", "//", ExpectedResult = "abcdef")]
	[TestCase("abc//def", "//", ExpectedResult = "abc")]
	[TestCase("abc//", "//", ExpectedResult = "abc")]
	[TestCase("//def", "//", ExpectedResult = "")]
	public string ReadTo(string value, string splitter) => value.SubStringBefore(splitter);

	[TestCase("abc", "//", ExpectedResult = "abc")]
	[TestCase("abc//def", "//", ExpectedResult = "abc")]
	[TestCase("abc//", "//", ExpectedResult = "abc")]
	[TestCase("//def", "//", ExpectedResult = "")]
	[TestCase("abc////def", "//", ExpectedResult = "abc//")]
	[TestCase("////def", "//", ExpectedResult = "//")]
	public string ReadToLast(string value, string splitter) => value.ReadToLastInstanceOf(splitter);

	[Test]
	public void WhereNotNull()
	{
		Assert.That(new object?[] { "abc", 123, null, new object(), 123d, 123f }.WhereNotNull(), Is.Not.Contains(null));
		Assert.That(new object?[] { null, null, null }.WhereNotNull(), Is.Not.Contains(null));
	}
}

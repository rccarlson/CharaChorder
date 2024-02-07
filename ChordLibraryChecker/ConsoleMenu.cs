using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChordLibraryChecker;

public record MenuChoice(string Content, ConsoleKey SelectionKey);

public static class ConsoleMenu
{
	public static int Show(string prompt, MenuChoice[] choices)
	{
		Console.Clear();
		Console.WriteLine(prompt);
		foreach (MenuChoice choice in choices) Console.WriteLine(choice.Content);
		read:
		var selection = Console.ReadKey(intercept: true);
		var selectionIdx = IndexOf(choices, choice => choice.SelectionKey == selection.Key);
		if (selectionIdx == -1) goto read;
		else return selectionIdx;
	}

	private static int IndexOf<T>(IList<T> values, Predicate<T> predicate)
	{
		for(int i = 0; i < values.Count; i++)
		{
			if (predicate(values[i])) return i;
		}
		return -1;
	}
}
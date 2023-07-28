using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CharaChorderInterface;

public enum ResetType
{
	/// <summary> Restarts the microcontroller </summary>
	Restart,
	/// <summary> Performs a factory reset of the flash and emulated eeprom. During the process, the flash chip is erased. </summary>
	Factory,
	/// <summary> Restarts the device into a bootloader mode. </summary>
	Bootloader,
	/// <summary> Resets the parameters to factory defaults and commits. </summary>
	Params,
	/// <summary> Resets the keymaps to the factory defaults and commits. </summary>
	Keymaps,
	/// <summary> Adds starter chordmaps. This does not clear the chordmap library, but adds to it, replacing those that have the same chord. </summary>
	Starter,
	/// <summary> Permanently deletes all the chordmaps stored in the device memory. </summary>
	ClearCml,
	/// <summary> Attempts to upgrade chordmaps that the system detects are older. </summary>
	UpgradeCml,
	/// <summary> Adds back in functional chords such as CAPSLOCKS and Backspace-X chords. </summary>
	Func,
}

public enum OperatingSystem
{
	Windows = 0,
	Mac = 1,
	Linux = 2,
	iOS = 3,
	Android = 4,
	Unknown = 255,
}

public enum Keymap { 
	Primary,
	Num,
	Function
}
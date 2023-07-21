// This file is based on https://stackoverflow.com/a/36616242/11069086 with some modifications

namespace CharaChorder.Utility;

using System;
using System.Text;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using static CharaChorderInterface.Utility.DllImports;

internal class SerialUtility
{
	const int BUFFER_SIZE = 1024;


	const int utf16terminatorSize_bytes = 2;

	public struct DeviceInfo
	{
		public string name;
		public string description;
		public string bus_description;
	}

	static DEVPROPKEY DEVPKEY_Device_BusReportedDeviceDesc = new DEVPROPKEY()
	{
		fmtid = new Guid(0x540b947e, 0x8b40, 0x45bc, 0xa8, 0xa2, 0x6a, 0x0b, 0x89, 0x4c, 0xbd, 0xa2),
		pid = 4
	};

	public static List<DeviceInfo> GetAllCOMPorts()
	{
		Guid[] guids = GetClassGUIDs("Ports");
		List<DeviceInfo> devices = new();
		for (int index = 0; index < guids.Length; index++)
		{
			IntPtr hDeviceInfoSet = SetupDiGetClassDevs(ref guids[index], 0, 0, DiGetClassFlags.DIGCF_PRESENT);
			if (hDeviceInfoSet == IntPtr.Zero) continue;

			try
			{
				UInt32 iMemberIndex = 0;
				while (true)
				{
					SP_DEVINFO_DATA deviceInfoData = new SP_DEVINFO_DATA();
					deviceInfoData.cbSize = (uint)Marshal.SizeOf(typeof(SP_DEVINFO_DATA));
					bool success = SetupDiEnumDeviceInfo(hDeviceInfoSet, iMemberIndex, ref deviceInfoData);
					if (!success)
					{
						// No more devices in the device information set
						break;
					}

					DeviceInfo deviceInfo = new DeviceInfo()
					{
						name = GetDeviceName(hDeviceInfoSet, deviceInfoData),
						description = GetDeviceDescription(hDeviceInfoSet, deviceInfoData),
						bus_description = GetDeviceBusDescription(hDeviceInfoSet, deviceInfoData),
					};
					devices.Add(deviceInfo);

					iMemberIndex++;
				}
			}
			finally
			{
				SetupDiDestroyDeviceInfoList(hDeviceInfoSet);
			}
		}
		return devices;
	}

	private static string GetDeviceName(IntPtr pDevInfoSet, SP_DEVINFO_DATA deviceInfoData)
	{
		IntPtr hDeviceRegistryKey = SetupDiOpenDevRegKey(pDevInfoSet, ref deviceInfoData,
				DICS_FLAG_GLOBAL, 0, DIREG_DEV, KEY_QUERY_VALUE);
		if (hDeviceRegistryKey == IntPtr.Zero)
		{
			throw new Exception("Failed to open a registry key for device-specific configuration information");
		}

		byte[] ptrBuf = new byte[BUFFER_SIZE];
		uint length = (uint)ptrBuf.Length;
		try
		{
			uint lpRegKeyType;
			int result = RegQueryValueEx(hDeviceRegistryKey, "PortName", 0, out lpRegKeyType, ptrBuf, ref length);
			if (result != 0)
			{
				throw new Exception("Can not read registry value PortName for device " + deviceInfoData.ClassGuid);
			}
		}
		finally
		{
			RegCloseKey(hDeviceRegistryKey);
		}

		return Encoding.Unicode.GetString(ptrBuf, 0, (int)length - utf16terminatorSize_bytes);
	}

	private static string GetDeviceDescription(IntPtr hDeviceInfoSet, SP_DEVINFO_DATA deviceInfoData)
	{
		byte[] ptrBuf = new byte[BUFFER_SIZE];
		uint propRegDataType;
		uint RequiredSize;
		bool success = SetupDiGetDeviceRegistryProperty(hDeviceInfoSet, ref deviceInfoData, SPDRP.SPDRP_DEVICEDESC,
				out propRegDataType, ptrBuf, BUFFER_SIZE, out RequiredSize);
		if (!success)
		{
			throw new Exception("Can not read registry value PortName for device " + deviceInfoData.ClassGuid);
		}
		return Encoding.Unicode.GetString(ptrBuf, 0, (int)RequiredSize - utf16terminatorSize_bytes);
	}

	private static string GetDeviceBusDescription(IntPtr hDeviceInfoSet, SP_DEVINFO_DATA deviceInfoData)
	{
		byte[] ptrBuf = new byte[BUFFER_SIZE];
		uint propRegDataType;
		uint RequiredSize;
		bool success = SetupDiGetDevicePropertyW(hDeviceInfoSet, ref deviceInfoData, ref DEVPKEY_Device_BusReportedDeviceDesc,
				out propRegDataType, ptrBuf, BUFFER_SIZE, out RequiredSize, 0);
		if (!success)
		{
			return null;
			//throw new Exception("Can not read Bus provided device description device " + deviceInfoData.ClassGuid);
		}
		return System.Text.UnicodeEncoding.Unicode.GetString(ptrBuf, 0, (int)RequiredSize - utf16terminatorSize_bytes);
	}

	private static Guid[] GetClassGUIDs(string className)
	{
		UInt32 requiredSize = 0;
		Guid[] guidArray = new Guid[1];

		bool status = SetupDiClassGuidsFromName(className, ref guidArray[0], 1, out requiredSize);
		if (true == status)
		{
			if (1 < requiredSize)
			{
				guidArray = new Guid[requiredSize];
				SetupDiClassGuidsFromName(className, ref guidArray[0], requiredSize, out requiredSize);
			}
		}
		else
			throw new System.ComponentModel.Win32Exception();

		return guidArray;
	}
}
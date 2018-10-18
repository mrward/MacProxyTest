using System;
using System.Runtime.InteropServices;

namespace ProxyTest
{
	internal static class Platform
	{
		private static bool checkedOS;

		private static bool isMacOS;

		private static bool isFreeBSD;

		public static bool IsMacOS {
			get {
				if (!checkedOS) {
					CheckOS ();
				}
				return isMacOS;
			}
		}

		public static bool IsFreeBSD {
			get {
				if (!checkedOS) {
					CheckOS ();
				}
				return isFreeBSD;
			}
		}

		[DllImport ("libc")]
		private static extern int uname (IntPtr buf);

		private static void CheckOS ()
		{
			if (Environment.OSVersion.Platform != PlatformID.Unix) {
				checkedOS = true;
			} else {
				IntPtr intPtr = Marshal.AllocHGlobal (8192);
				if (uname (intPtr) == 0) {
					string a = Marshal.PtrToStringAnsi (intPtr);
					if (!(a == "Darwin")) {
						if (a == "FreeBSD") {
							isFreeBSD = true;
						}
					} else {
						isMacOS = true;
					}
				}
				Marshal.FreeHGlobal (intPtr);
				checkedOS = true;
			}
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace BLETest
{
    internal class KeyControl
    {
        public static void SingleKeyOperation(int code, uint dwFlags)
        {
            Win32Interops.INPUT[] inputs = new Win32Interops.INPUT[1];
            inputs[0].type = 1; // INPUT_KEYBOARD
            inputs[0].input =new Win32Interops.InputUnion
            {
                ki = new Win32Interops.KEYBDINPUT
                {
                    wVk = (ushort)code,
                    wScan = 0,
                    dwFlags = dwFlags,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            };
            Win32Interops.SendInput(1, inputs, Marshal.SizeOf(typeof(Win32Interops.INPUT)));
        }

        public static void KeyDown(int code)
        {
            SingleKeyOperation(code, 0);
        }
        public static void KeyUp(int code)
        {
            SingleKeyOperation(code, 2); // KEYEVENTF_KEYUP
        }
    }
}

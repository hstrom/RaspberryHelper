using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace RaspberryHelper
{
    public static class Delay
    {
        [DllImport("kernel32.dll")]
        public static extern uint GetTickCount();

        /// <summary>
        /// 非程序假死的延迟
        /// </summary>
        /// <param name="ms">毫秒</param>
        public static void DelyNotSleep(uint ms)
        {
            uint start = GetTickCount();
            while (GetTickCount() - start < ms)
            {
                System.Threading.Thread.Sleep(500);
                Application.DoEvents();
            }
        }

    }
}

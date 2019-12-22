using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace swtab
{
    class GameAgent
    {
        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wp, IntPtr lp);
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetForegroundWindow(IntPtr hWnd);


        [DllImport("swhook.dll")]
        private static extern IntPtr newGame(string fileName, string workDirectory, IntPtr hWnd);
        [DllImport("swhook.dll")]
        private static extern bool readId(IntPtr gi, [Out] char[] lpBuffer);
        [DllImport("swhook.dll")]
        private static extern IntPtr readHwnd(IntPtr gi);
        [DllImport("swhook.dll")]
        private static extern void killGame(IntPtr gi);
        [DllImport("swhook.dll")]
        private static extern IntPtr getProcess(IntPtr gi);


        private IntPtr gi;
        private IntPtr _hWnd;
        public GameAgent(string fileName, string workDirectory, IntPtr hWnd)
        {
            gi = newGame(fileName, workDirectory, hWnd);
        }

        public IntPtr hWnd
        {
            get
            {
                if (_hWnd == IntPtr.Zero)
                {
                    _hWnd = readHwnd(gi);
                }
                return _hWnd;
            }
        }

        public void close()
        {
            if (hWnd != IntPtr.Zero)
            {
                SendMessage(hWnd, 0x0010, IntPtr.Zero, IntPtr.Zero);
            }
        }

        public void active()
        {
            if (hWnd != IntPtr.Zero)
            {
                SetForegroundWindow(hWnd);
            }
        }

        public void kill()
        {
            killGame(gi);
        }

        public string id
        {
            get
            {
                var buffer = new char[20];
                int len = 0;
                if (!readId(gi, buffer))
                {
                    return null;
                }

                while (buffer[len] > 0) len++;
                return new string(buffer, 0, len);
            }
        }

        public IntPtr hProcess
        {
            get
            {
                return getProcess(gi);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public static class MemoryReader
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool ReadProcessMemory(
          IntPtr hProcess,
          IntPtr lpBaseAddress,
          byte[] lpBuffer,
          int dwSize,
          out int lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenProcess(
        uint processAccess,
        bool bInheritHandle,
        int processId);

        private const uint PROCESS_WM_READ = 0x0010;

        public static int ReadMemoryInt(Process process, int address)
        {
            IntPtr processHandle = process.Handle;
            byte[] buffer = new byte[4]; // Buffer for a 4-byte integer
            bool success = ReadProcessMemory(processHandle, new IntPtr(address), buffer, buffer.Length, out int bytesRead);

            // Logging and error checking
            if (!success)
            {
                int error = Marshal.GetLastWin32Error();
                MessageBox.Show($"ReadProcessMemory failed with error code {error}.");
            }

            //MessageBox.Show($"Bytes read: {bytesRead}. Data: {BitConverter.ToString(buffer)}");

            if (bytesRead == 4)
            {
                return BitConverter.ToInt32(buffer, 0);
            }
            else
            {
                throw new InvalidOperationException("Failed to read memory from process.");
            }
        }


        public static Process GetProcessByName(string processName)
        {
            Process[] processes = Process.GetProcessesByName(processName);
            if (processes.Length == 0)
            {
                throw new InvalidOperationException($"Process '{processName}' not found.");
            }

            return processes[0];
        }

        public static string ReadMemoryString(Process process, int address, int maxLength = 256)
        {
            IntPtr processHandle = process.Handle;
            byte[] buffer = new byte[maxLength];
            bool success = ReadProcessMemory(processHandle, new IntPtr(address), buffer, buffer.Length, out int bytesRead);

            if (!success)
            {
                throw new InvalidOperationException("Failed to read memory from process.");
            }

            int nullTerminatorPos = Array.IndexOf(buffer, (byte)0);
            if (nullTerminatorPos == -1)
            {
                nullTerminatorPos = maxLength;
            }
            return System.Text.Encoding.ASCII.GetString(buffer, 0, nullTerminatorPos);
        }
    }
}

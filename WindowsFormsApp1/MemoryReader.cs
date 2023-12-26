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
using System.Globalization;

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

        public static int ReadMemoryInt(Process process, string offsetHexString)
        {
            IntPtr baseAddress = process.MainModule.BaseAddress;
            int offset = Convert.ToInt32(offsetHexString, 16); // Convert the hex string to an integer
            IntPtr address = IntPtr.Add(baseAddress, offset); // Add the offset to the base address

            byte[] buffer = new byte[4]; // Buffer for a 4-byte integer
            bool success = ReadProcessMemory(process.Handle, address, buffer, buffer.Length, out int bytesRead);

            // Logging and error checking
            if (!success)
            {
                int error = Marshal.GetLastWin32Error();
                //MessageBox.Show($"ReadProcessMemory failed with error code {error}.");
                return 0;
            }

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
                return null;
            }

            return processes[0];
        }

        public static string ReadMemoryString(Process process, string offsetHexString, int length)
        {
            IntPtr baseAddress = process.MainModule.BaseAddress;
            int offset = Convert.ToInt32(offsetHexString, 16); // Convert the hex string to an integer
            IntPtr address = IntPtr.Add(baseAddress, offset); // Add the offset to the base address

            byte[] buffer = new byte[length]; // Buffer for the string
            bool success = ReadProcessMemory(process.Handle, address, buffer, buffer.Length, out int bytesRead);

            // Logging and error checking
            if (!success)
            {
                int error = Marshal.GetLastWin32Error();
                // MessageBox.Show($"ReadProcessMemory failed with error code {error}.");
                return string.Empty;
            }

            // Find the null terminator index
            int nullIndex = Array.IndexOf(buffer, (byte)0);
            if (nullIndex >= 0)
            {
                // Resize buffer to actual string length
                Array.Resize(ref buffer, nullIndex);
            }

            // Decode the byte array to a string
            // Assuming the string is ASCII encoded. Change the encoding if necessary.
            return System.Text.Encoding.ASCII.GetString(buffer);
        }


    }
}

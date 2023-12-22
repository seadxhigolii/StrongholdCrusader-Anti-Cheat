using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public class User
    {
        const uint PROCESS_ALL_ACCESS = 0x1F0FFF;
        private int userID = 0;
        public event Action UserIdSet;

        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint dwSize, out int lpNumberOfBytesRead);

        public int GetUserId()
        {
            return this.userID;
        }
        public int ReadGameRangerUserId(Button button, TextBox textBox)
        {
            var processes = Process.GetProcessesByName("GameRanger");
            if (!processes.Any())
            {
                //MessageBox.Show("GameRanger is not running.");
                return 0;
            }

            int offset = 0x2EA898;
            byte[] buffer = new byte[4];
            int bytesRead;

            foreach (var gameRangerProcess in processes)
            {
                IntPtr processHandle = OpenProcess(PROCESS_ALL_ACCESS, false, gameRangerProcess.Id);
                IntPtr addressToRead = new IntPtr(gameRangerProcess.MainModule.BaseAddress.ToInt32() + offset);
                bool result = ReadProcessMemory(processHandle, addressToRead, buffer, (uint)buffer.Length, out bytesRead);
                if (!result)
                {
                    MessageBox.Show($"Failed to read memory at address {addressToRead.ToString("X")} for process {gameRangerProcess.Id}");
                    continue;
                }

                this.userID = BitConverter.ToInt32(buffer, 0);
                if (userID != 0)
                {
                    button.Text = "Connecting...";
                    button.BackColor = Color.Gray;
                    button.Enabled = false;
                    UserIdSet?.Invoke();
                    return userID;
                }
            }
            return 0;
        }
    }
}

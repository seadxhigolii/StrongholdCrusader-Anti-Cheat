using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

public class MouseMonitor : IDisposable
{
    private const int WH_MOUSE_LL = 14;
    private const int WM_LBUTTONDOWN = 0x0201;
    private static IntPtr _hookID;
    private static LowLevelMouseProc _proc;
    private static List<DateTime> clickTimestamps = new List<DateTime>();
    private static Queue<DateTime> clicksLast10Seconds = new Queue<DateTime>();
    private int maxCPSLast10Seconds = 0;
    private DateTime firstClickTimestamp;
    private DateTime lastClearingTime;
    private TimeSpan clearingInterval = TimeSpan.FromSeconds(10); // Interval for clearing the data

    public event Action HighClickRateDetected;

    public double HighestCPSLast10Seconds
    {
        get { return maxCPSLast10Seconds; }
    }

    public double AverageCPSLast10Seconds
    {
        get
        {
            double totalDuration = (DateTime.Now - firstClickTimestamp).TotalSeconds;
            totalDuration = totalDuration > 10 ? 10 : totalDuration;
            double average = totalDuration == 0 ? 0 : (double)clicksLast10Seconds.Count / totalDuration;
            return Math.Round(average, 1);
        }
    }

    public MouseMonitor()
    {
        _proc = HookCallback;
        _hookID = SetHook(_proc);
        lastClearingTime = DateTime.Now; // Initialize last clearing time
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        try
        {
            DateTime now = DateTime.Now;

            // Time-based clearing logic
            if (now - lastClearingTime > clearingInterval)
            {
                clickTimestamps.Clear();
                clicksLast10Seconds.Clear();
                maxCPSLast10Seconds = 0;
                lastClearingTime = now;
            }

            if (nCode >= 0 && wParam == (IntPtr)WM_LBUTTONDOWN)
            {
                clickTimestamps.Add(now);
                clickTimestamps.RemoveAll(ts => ts < now.AddSeconds(-1));

                if (clicksLast10Seconds.Count == 0)
                {
                    firstClickTimestamp = now;
                }

                clicksLast10Seconds.Enqueue(now);
                while (clicksLast10Seconds.Count > 0 && clicksLast10Seconds.Peek() < now.AddSeconds(-10))
                {
                    clicksLast10Seconds.Dequeue();
                    if (clicksLast10Seconds.Count > 0)
                    {
                        firstClickTimestamp = clicksLast10Seconds.Peek();
                    }
                }

                int currentCPS = clickTimestamps.Count;
                if (currentCPS > maxCPSLast10Seconds)
                    maxCPSLast10Seconds = currentCPS;
            }

            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }
        catch (Exception ex)
        {
            throw ex;
        }
    }

    public void Clear()
    {
        clicksLast10Seconds.Clear();
        clickTimestamps.Clear();
        maxCPSLast10Seconds = 0;
    }

    private static IntPtr SetHook(LowLevelMouseProc proc)
    {
        using (Process curProcess = Process.GetCurrentProcess())
        using (ProcessModule curModule = curProcess.MainModule)
        {
            return SetWindowsHookEx(WH_MOUSE_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
        }
    }

    public void Dispose()
    {
        UnhookWindowsHookEx(_hookID);
    }

    #region P/Invoke Declarations

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

    #endregion
}

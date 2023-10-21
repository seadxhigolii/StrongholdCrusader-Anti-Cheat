using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

public class MouseMonitor
{
    private const int WH_MOUSE_LL = 14;
    private const int WM_LBUTTONDOWN = 0x0201;
    private const int MAX_CPS = 10;
    private const int SLIDING_WINDOW_SECONDS = 1;
    private static IntPtr _hookID;
    private static LowLevelMouseProc _proc;
    private static List<DateTime> clickTimestamps = new List<DateTime>();

    // Event to notify when high click rate is detected
    public event Action HighClickRateDetected;

    public MouseMonitor()
    {
        //_proc = HookCallback;
        _hookID = SetHook(_proc);
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        Debug.WriteLine(DateTime.Now.Millisecond);
        try
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_LBUTTONDOWN)
            {
                clickTimestamps.Add(DateTime.Now);
                clickTimestamps.RemoveAll(ts => ts < DateTime.Now.AddSeconds(-SLIDING_WINDOW_SECONDS));

                if (clickTimestamps.Count > MAX_CPS)
                {
                    HighClickRateDetected?.Invoke();
                    clickTimestamps.Clear();
                }
            }

            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }
        catch (Exception)
        {
            throw;
        }
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

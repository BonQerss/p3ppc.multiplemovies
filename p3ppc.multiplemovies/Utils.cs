﻿using p3ppc.multiplemovies.Configuration;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using Reloaded.Mod.Interfaces;
using System.Diagnostics;
using System.Text;

namespace p3ppc.expShare.NuGet.templates.defaultPlus
{
    internal class Utils
    {
        private static ILogger _logger;
        private static Config _config;
        private static IStartupScanner _startupScanner;
        internal static nint BaseAddress { get; private set; }

        internal static bool Initialise(ILogger logger, Config config, IModLoader modLoader)
        {
            _logger = logger;
            _config = config;
            using var thisProcess = Process.GetCurrentProcess();
            BaseAddress = thisProcess.MainModule!.BaseAddress;

            var startupScannerController = modLoader.GetController<IStartupScanner>();
            if (startupScannerController == null || !startupScannerController.TryGetTarget(out _startupScanner))
            {
                LogError($"Unable to get controller for Reloaded SigScan Library, stuff won't work :(");
                return false;
            }

            return true;
        }

        internal static void Log(string message)
        {
            _logger.WriteLine($"[Exp Share] {message}");
        }

        internal static void LogError(string message, Exception e)
        {
            _logger.WriteLine($"[Exp Share] {message}: {e.Message}", System.Drawing.Color.Red);
        }

        internal static void LogError(string message)
        {
            _logger.WriteLine($"[Exp Share] {message}", System.Drawing.Color.Red);
        }

        internal static void SigScan(string pattern, string name, Action<nint> action)
        {
            _startupScanner.AddMainModuleScan(pattern, result =>
            {
                if (!result.Found)
                {
                    LogError($"Unable to find {name}, stuff won't work :(");
                    return;
                }

                action(result.Offset + BaseAddress);
            });
        }

        public static string PushXmm(int xmmNum)
        {
            return // Save an xmm register 
                $"sub rsp, 16\n" + // allocate space on stack
                $"movdqu dqword [rsp], xmm{xmmNum}\n";
        }

        public static string PushXmm()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < 16; i++)
            {
                sb.Append(PushXmm(i));
            }
            return sb.ToString();
        }

        public static string PopXmm(int xmmNum)
        {
            return //Pop back the value from stack to xmm
                $"movdqu xmm{xmmNum}, dqword [rsp]\n" +
                $"add rsp, 16\n"; // re-align the stack
        }

        public static string PopXmm()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 7; i >= 0; i--)
            {
                sb.Append(PopXmm(i));
            }
            return sb.ToString();
        }

        internal static unsafe nuint GetGlobalAddress(nint ptrAddress)
        {
            return (nuint)((*(int*)ptrAddress) + ptrAddress + 4);
        }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace HpToolsLauncher
{
    public static class ProcessExtensions
    {
        #region Win32 Constants

        private const int CREATE_UNICODE_ENVIRONMENT = 0x00000400;
        private const int CREATE_NO_WINDOW = 0x08000000;

        private const int CREATE_NEW_CONSOLE = 0x00000010;

        private const uint INVALID_SESSION_ID = 0xFFFFFFFF;
        private static readonly IntPtr WTS_CURRENT_SERVER_HANDLE = IntPtr.Zero;
        
        private const UInt32 INFINITE = 0xFFFFFFFF;

        const int STD_OUTPUT_HANDLE = -11;
        const int STD_ERROR_HANDLE = -12;
        const Int32 STARTF_USESTDHANDLES = 0x00000100;

        #endregion

        #region DllImports

        [DllImport("advapi32.dll", EntryPoint = "CreateProcessAsUser", SetLastError = true, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern bool CreateProcessAsUser(
            IntPtr hToken,
            String lpApplicationName,
            String lpCommandLine,
            IntPtr lpProcessAttributes,
            IntPtr lpThreadAttributes,
            bool bInheritHandle,
            uint dwCreationFlags,
            IntPtr lpEnvironment,
            String lpCurrentDirectory,
            ref STARTUPINFO lpStartupInfo,
            out PROCESS_INFORMATION lpProcessInformation);

        [DllImport("advapi32.dll", EntryPoint = "DuplicateTokenEx", SetLastError = true)]
        private static extern bool DuplicateTokenEx(
            IntPtr ExistingTokenHandle,
            uint dwDesiredAccess,
            IntPtr lpThreadAttributes,
            int TokenType,
            int ImpersonationLevel,
            ref IntPtr DuplicateTokenHandle);

        [DllImport("userenv.dll", SetLastError = true)]
        private static extern bool CreateEnvironmentBlock(ref IntPtr lpEnvironment, IntPtr hToken, bool bInherit);

        [DllImport("userenv.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DestroyEnvironmentBlock(IntPtr lpEnvironment);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetExitCodeProcess(IntPtr hProcess, out uint ExitCode);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hSnapshot);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint WTSGetActiveConsoleSessionId();

        [DllImport("Wtsapi32.dll", SetLastError = true)]
        private static extern uint WTSQueryUserToken(uint SessionId, ref IntPtr phToken);

        [DllImport("wtsapi32.dll", SetLastError = true)]
        private static extern int WTSEnumerateSessions(
            IntPtr hServer,
            int Reserved,
            int Version,
            ref IntPtr ppSessionInfo,
            ref int pCount);

        [DllImport("Kernel32.dll", SetLastError = true)]
        private static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);
        #endregion

        #region Win32 Structs

        private enum SW
        {
            SW_HIDE = 0,
            SW_SHOWNORMAL = 1,
            SW_NORMAL = 1,
            SW_SHOWMINIMIZED = 2,
            SW_SHOWMAXIMIZED = 3,
            SW_MAXIMIZE = 3,
            SW_SHOWNOACTIVATE = 4,
            SW_SHOW = 5,
            SW_MINIMIZE = 6,
            SW_SHOWMINNOACTIVE = 7,
            SW_SHOWNA = 8,
            SW_RESTORE = 9,
            SW_SHOWDEFAULT = 10,
            SW_MAX = 10
        }

        private enum WTS_CONNECTSTATE_CLASS
        {
            WTSActive,
            WTSConnected,
            WTSConnectQuery,
            WTSShadow,
            WTSDisconnected,
            WTSIdle,
            WTSListen,
            WTSReset,
            WTSDown,
            WTSInit
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public uint dwProcessId;
            public uint dwThreadId;
        }

        private enum SECURITY_IMPERSONATION_LEVEL
        {
            SecurityAnonymous = 0,
            SecurityIdentification = 1,
            SecurityImpersonation = 2,
            SecurityDelegation = 3,
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct STARTUPINFO
        {
            public int cb;
            public String lpReserved;
            public String lpDesktop;
            public String lpTitle;
            public uint dwX;
            public uint dwY;
            public uint dwXSize;
            public uint dwYSize;
            public uint dwXCountChars;
            public uint dwYCountChars;
            public uint dwFillAttribute;
            public uint dwFlags;
            public short wShowWindow;
            public short cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        private enum TOKEN_TYPE
        {
            TokenPrimary = 1,
            TokenImpersonation = 2
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct WTS_SESSION_INFO
        {
            public readonly UInt32 SessionID;

            [MarshalAs(UnmanagedType.LPStr)]
            public readonly String pWinStationName;

            public readonly WTS_CONNECTSTATE_CLASS State;
        }

        #endregion

        /// <summary>
        /// Get the parent process for a given process handle.
        /// </summary>
        /// <param name="hProcess">the process handle</param>
        /// <returns>The parent process</returns>
        private static Process GetParentProcess(IntPtr hProcess)
        {
            NativeProcess.PROCESS_BASIC_INFORMATION pbi = new NativeProcess.PROCESS_BASIC_INFORMATION();
            int pbiLength = Marshal.SizeOf(pbi);
            int returnLength = 0;

            int status = NativeProcess.NtQueryInformationProcess(hProcess, NativeProcess.PROCESSINFOCLASS.ProcessBasicInformation,
                ref pbi, pbiLength, out returnLength);

            if (status != 0)
            {
                throw new Win32Exception(status);
            }

            try
            {
                return Process.GetProcessById(pbi.InheritedFromUniqueProcessId.ToInt32());
            }
            catch (ArgumentException)
            { // Not found
                return null;
            }
        }
        /// <summary>
        /// Returns the parent process of a given process
        /// </summary>
        /// <param name="process">the process for which to find the parent</param>
        /// <returns>the parent process</returns>
        public static Process Parent(this Process process)
        {
            return GetParentProcess(process.Handle);
        }

        /// <summary>
        /// Gets the user token from the currently active session
        /// </summary>
        /// <param name="phUserToken">A handle to the primary token that represents a user</param>
        /// <returns>boolean result</returns>
        private static bool GetSessionUserToken(ref IntPtr phUserToken)
        {
            var bResult = false;
            var hImpersonationToken = IntPtr.Zero;
            var activeSessionId = INVALID_SESSION_ID;
            var pSessionInfo = IntPtr.Zero;
            var sessionCount = 0;

            // Get a handle to the user access token for the current active session.
            if (WTSEnumerateSessions(WTS_CURRENT_SERVER_HANDLE, 0, 1, ref pSessionInfo, ref sessionCount) != 0)
            {
                var arrayElementSize = Marshal.SizeOf(typeof(WTS_SESSION_INFO));
                var current = pSessionInfo;

                for (var i = 0; i < sessionCount; i++)
                {
                    var si = (WTS_SESSION_INFO)Marshal.PtrToStructure((IntPtr)current, typeof(WTS_SESSION_INFO));
                    current += arrayElementSize;

                    if (si.State == WTS_CONNECTSTATE_CLASS.WTSActive)
                    {
                        activeSessionId = si.SessionID;
                        Console.WriteLine("Debug: session id is found: {0}", activeSessionId);
                        break;
                    }
                }
            }

            // If enumerating did not work, fall back to the old method
            if (activeSessionId == INVALID_SESSION_ID)
            {
                activeSessionId = WTSGetActiveConsoleSessionId();
                Console.WriteLine("Debug: fallback to old session method and session id is: {0}", activeSessionId);
            }

            if (WTSQueryUserToken(activeSessionId, ref hImpersonationToken) != 0)
            {
                Console.WriteLine("Debug: user token is retrieved from the session id: {0}", activeSessionId);
                // Convert the impersonation token to a primary token
                bResult = DuplicateTokenEx(hImpersonationToken, 0, IntPtr.Zero,
                    (int)SECURITY_IMPERSONATION_LEVEL.SecurityImpersonation, (int)TOKEN_TYPE.TokenPrimary,
                    ref phUserToken);

                if (!bResult)
                {
                    int errorCode = Marshal.GetLastWin32Error();
                    Console.Error.WriteLine("Warning: failed to duplicate the user token of the session id {0}. Error code: {1}", activeSessionId, errorCode);
                }
                else
                {
                    Console.WriteLine("Debug: the user token is successfully duplicated for creating new process in session: {0}", activeSessionId);
                }

                CloseHandle(hImpersonationToken);
            }
            else
            {
                // can't query user token
                int errorCode = Marshal.GetLastWin32Error();
                Console.Error.WriteLine("Warning: failed to query user token from the session id {2}. Error code: {1}", activeSessionId, errorCode);
            }

            return bResult;
        }

        /// <summary>
        /// Start a process from active user session
        /// </summary>
        /// <param name="appPath">The path of the module to be executed.</param>
        /// <param name="cmdLine">The command line to be executed.</param>
        /// <param name="workDir">The full path to the current directory for the process.</param>
        /// <param name="visible">Show window or not.</param>
        /// <returns>boolean result</returns>
        public static int StartProcessFromUserSession(string appPath, string cmdLine = null, string workDir = null, bool visible = true)
        {
            Console.WriteLine("Debug: starting process from user session. Program={0}; args={1}; working directory={2}",
                appPath ?? string.Empty, cmdLine ?? string.Empty, workDir ?? string.Empty);

            var hUserToken = IntPtr.Zero;
            var startInfo = new STARTUPINFO();
            var procInfo = new PROCESS_INFORMATION();
            var pEnv = IntPtr.Zero;
            int iResultOfCreateProcessAsUser;
            uint iExitCode = 1;

            startInfo.cb = Marshal.SizeOf(typeof(STARTUPINFO));

            try
            {
                if (!GetSessionUserToken(ref hUserToken))
                {
                    throw new Exception("StartProcessAsCurrentUser: GetSessionUserToken failed.");
                }

                uint dwCreationFlags = CREATE_UNICODE_ENVIRONMENT | (uint)(visible ? CREATE_NEW_CONSOLE : CREATE_NO_WINDOW);
                startInfo.wShowWindow = (short)(visible ? SW.SW_SHOW : SW.SW_HIDE);
                startInfo.lpDesktop = "winsta0\\default";
                startInfo.hStdOutput = GetStdHandle(STD_OUTPUT_HANDLE);
                startInfo.hStdError = GetStdHandle(STD_ERROR_HANDLE);
                startInfo.dwFlags |= STARTF_USESTDHANDLES;
                if (!CreateEnvironmentBlock(ref pEnv, hUserToken, false))
                {
                    throw new Exception("StartProcessAsCurrentUser: CreateEnvironmentBlock failed.");
                }

                if (!CreateProcessAsUser(hUserToken,
                    appPath, // Application Name
                    cmdLine, // Command Line
                    IntPtr.Zero,
                    IntPtr.Zero,
                    false,
                    dwCreationFlags,
                    pEnv,
                    workDir, // Working directory
                    ref startInfo,
                    out procInfo))
                {
                    iResultOfCreateProcessAsUser = Marshal.GetLastWin32Error();
                    throw new Exception("StartProcessAsCurrentUser: CreateProcessAsUser failed.  Error Code -" + iResultOfCreateProcessAsUser);
                }

                Console.WriteLine("Debug: the new launcher process is started. PID: {0}", procInfo.dwProcessId);
                Console.WriteLine("The following output comes from the new process:");
                Console.WriteLine("#######################################################");
                WaitForSingleObject(procInfo.hProcess, INFINITE);
                Console.WriteLine("#######################################################");
                iResultOfCreateProcessAsUser = Marshal.GetLastWin32Error();
                GetExitCodeProcess(procInfo.hProcess, out iExitCode);                
            }
            finally
            {
                CloseHandle(hUserToken);
                if (pEnv != IntPtr.Zero)
                {
                    DestroyEnvironmentBlock(pEnv);
                }
                CloseHandle(procInfo.hThread);
                CloseHandle(procInfo.hProcess);
            }
            return (int)iExitCode;
        }

        /// <summary>
        /// Start a process from current session
        /// </summary>
        /// <param name="appPath">The path of the module to be executed.</param>
        /// <param name="cmdLine">The command line to be executed.</param>
        /// <param name="workDir">The full path to the current directory for the process.</param>
        /// <param name="visible">Show window or not.</param>
        public static int StartProcessFromCurrentSession(string appPath, string cmdLine = null, string workDir = null, bool visible = true)
        {
            Process process = new Process();
            process.StartInfo.FileName = appPath;
            process.StartInfo.Arguments = cmdLine;
            process.StartInfo.WorkingDirectory = workDir;
            process.StartInfo.CreateNoWindow = (visible ? false : true);
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.OutputDataReceived += (sender, args) => Console.WriteLine(args.Data);
            process.ErrorDataReceived += (sender, args) => Console.WriteLine(args.Data);
            process.Start();
            Console.WriteLine("The following output comes from the new process:");
            Console.WriteLine("#######################################################");
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();// Waits here for the process to exit.
            Console.WriteLine("#######################################################");
            return process.ExitCode;
        }
    }
}

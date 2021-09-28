using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;

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
        
        private const int INFINITE = -1;

        const int STD_OUTPUT_HANDLE = -11;
        const int STD_ERROR_HANDLE = -12;
        const Int32 STARTF_USESTDHANDLES = 0x00000100;

        // ERROR 1008 - An attempt was made to reference a token that does not exist
        const int ERROR_NO_TOKEN = 1008;
        // ERROR 1314 - A required privilege is not held by the client.
        const int ERROR_PRIVILEGE_NOT_HELD = 1314;

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

        [DllImport("wtsapi32.dll", SetLastError = true)]
        private static extern void WTSFreeMemory(IntPtr pMemory);

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
            Active,
            Connected,
            ConnectQuery,
            Shadow,
            Disconnected,
            Idle,
            Listen,
            Reset,
            Down,
            Init
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PROCESS_INFORMATION
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
        private static bool GetSessionUserToken(ref IntPtr phUserToken, ref uint activeSessionId)
        {
            activeSessionId = INVALID_SESSION_ID;
            var bResult = false;
            var hImpersonationToken = IntPtr.Zero;
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

                    ConsoleWriter.WriteVerboseLine(string.Format("Session: ID={0}, State={1}, Name={2}", si.SessionID, si.State, si.pWinStationName));

                    if (activeSessionId == INVALID_SESSION_ID && si.State == WTS_CONNECTSTATE_CLASS.Active)
                    {
                        activeSessionId = si.SessionID;
                    }
                }
                WTSFreeMemory(pSessionInfo);
            }

            // If enumerating did not work, fall back to the old method
            if (activeSessionId == INVALID_SESSION_ID)
            {
                activeSessionId = WTSGetActiveConsoleSessionId();
                Console.Error.WriteLine("Warning: Couldn't find any active user session.");
                ConsoleWriter.WriteVerboseLine(string.Format("Fallback to active console session of which id is: {0}", activeSessionId));
            }
            else
            {
                ConsoleWriter.WriteVerboseLine(string.Format("The active user session is found. Session id: {0}", activeSessionId));
            }

            if (WTSQueryUserToken(activeSessionId, ref hImpersonationToken) != 0)
            {
                ConsoleWriter.WriteVerboseLine(string.Format("The user token is retrieved from the session id: {0}", activeSessionId));
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
                    ConsoleWriter.WriteVerboseLine(string.Format("The user token is successfully duplicated for creating new process in session: {0}", activeSessionId));
                }

                CloseHandle(hImpersonationToken);
            }
            else
            {
                // can't query user token
                int errorCode = Marshal.GetLastWin32Error();
                if (errorCode == ERROR_NO_TOKEN)
                {
                    // token doesn't exit
                    ConsoleWriter.WriteVerboseLine(string.Format("Token does not exist for session: {0}", activeSessionId));
                }
                else if (errorCode == ERROR_PRIVILEGE_NOT_HELD)
                {
                    // the required privilege is typically held only by code running as Local System
                    Console.Error.WriteLine("Error: Insufficient privilege. Please run as \"Local System\" account. For Windows service, configure \"Log on as\" with \"Local System account\".");
                }
                else
                {
                    Console.Error.WriteLine("Error: Failed to retrieve token from the session id: {0}. Error code: {1}", activeSessionId, errorCode);
                }
            }

            return bResult;
        }

        /// <summary>
        /// Start a process in active user session
        /// </summary>
        /// <param name="appPath">The path of the module to be executed.</param>
        /// <param name="cmdLine">The command line to be executed.</param>
        /// <param name="workDir">The full path to the current directory for the process.</param>
        /// <param name="visible">Show window or not.</param>
        /// <returns>boolean result</returns>
        public static UserSessionProcInfo StartProcessInUserSession(string appPath, string cmdLine = null, string workDir = null, bool visible = false)
        {
            uint activeSessionId = 0;
            var hUserToken = IntPtr.Zero;
            var startInfo = new STARTUPINFO();
            var pEnv = IntPtr.Zero;
            PROCESS_INFORMATION procInfo;

            startInfo.cb = Marshal.SizeOf(typeof(STARTUPINFO));

            if (!GetSessionUserToken(ref hUserToken, ref activeSessionId))
            {
                // can't retrieve user token, return null to indicate no process is created
                return null;
            }

            uint dwCreationFlags = CREATE_UNICODE_ENVIRONMENT | (uint)(visible ? CREATE_NEW_CONSOLE : CREATE_NO_WINDOW);

            // the new process uses the environment of the calling process.
            //if (!CreateEnvironmentBlock(ref pEnv, hUserToken, false))
            //{
            //    Console.Error.WriteLine("Warning: Failed to retrieve the environment variables for the active user session. Skipped.");
            //}

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
                int err = Marshal.GetLastWin32Error();
                Console.Error.WriteLine("Error: Failed to create process in active user session.  Error Code: " + err);
                return null;
            }

            return new UserSessionProcInfo(activeSessionId, hUserToken, pEnv, procInfo);
        }

        /// <summary>
        /// Start a process in current session
        /// </summary>
        /// <param name="appPath">The path of the module to be executed.</param>
        /// <param name="cmdLine">The command line to be executed.</param>
        /// <param name="workDir">The full path to the current directory for the process.</param>
        /// <param name="visible">Show window or not.</param>
        public static CurrentSessionProcInfo StartProcessInCurrentSession(string appPath, string cmdLine = null, string workDir = null, bool visible = false)
        {
            Process process = new Process();
            process.StartInfo.FileName = appPath;
            process.StartInfo.Arguments = cmdLine;
            process.StartInfo.WorkingDirectory = workDir;
            process.StartInfo.CreateNoWindow = !visible;
            process.StartInfo.UseShellExecute = false;
            process.Start();

            return new CurrentSessionProcInfo(process);
        }

        public interface IProcessInfo
        {
            int PID { get; }
            int WaitForExit(int milliseconds);
            void Release();
        }

        public class UserSessionProcInfo : IProcessInfo
        {
            public UserSessionProcInfo(uint activeSessionID, IntPtr userToken, IntPtr envBlock, PROCESS_INFORMATION procInfo)
            {
                ActiveSessionID = activeSessionID;
                UserTokenHandle = userToken;
                EnvBlockHandle = envBlock;
                ProcInfo = procInfo;
            }

            public uint ActiveSessionID { get; private set; }
            public IntPtr UserTokenHandle { get; private set; }
            public IntPtr EnvBlockHandle { get; private set; }
            public PROCESS_INFORMATION ProcInfo { get; private set; }

            public int PID
            {
                get { return unchecked((int)ProcInfo.dwProcessId); }
            }

            public int WaitForExit(int milliseconds = INFINITE)
            {
                if (ProcInfo.hProcess != IntPtr.Zero)
                {
                    WaitForSingleObject(ProcInfo.hProcess, unchecked((uint)milliseconds));
                }

                uint code = GetExitCode();
                return unchecked((int)code);
            }

            public uint GetExitCode()
            {
                uint exitCode = 1;
                if (ProcInfo.hProcess != IntPtr.Zero)
                {
                    GetExitCodeProcess(ProcInfo.hProcess, out exitCode);
                }
                return exitCode;
            }

            public void Release()
            {
                CloseHandle(UserTokenHandle);
                UserTokenHandle = IntPtr.Zero;

                if (EnvBlockHandle != IntPtr.Zero)
                {
                    DestroyEnvironmentBlock(EnvBlockHandle);
                    EnvBlockHandle = IntPtr.Zero;
                }

                CloseHandle(ProcInfo.hThread);
                CloseHandle(ProcInfo.hProcess);
                ProcInfo = new PROCESS_INFORMATION();
            }
        }

        public class CurrentSessionProcInfo : IProcessInfo
        {
            public CurrentSessionProcInfo(Process proc)
            {
                Proc = proc;
            }

            public Process Proc { get; private set; }

            public int PID
            {
                get
                {
                    if (Proc != null)
                    {
                        return Proc.Id;
                    }

                    return 0;
                }
            }

            public void Release()
            {
                if (Proc != null)
                {
                    Proc.Dispose();
                    Proc = null;
                }
            }

            public int WaitForExit(int milliseconds)
            {
                if (Proc != null)
                {
                    Proc.WaitForExit(milliseconds);
                    return Proc.ExitCode;
                }

                return -1;
            }
        }
    }
}

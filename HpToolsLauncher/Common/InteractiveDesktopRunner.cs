using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading;

namespace HpToolsLauncher.Common
{
    /// <summary>
    /// Runs a process on the active desktop of the signed-in user when the launcher runs in Session 0 (e.g. over WinRM).
    /// The process is started by a temporary interactive-token scheduled task; its stdout/stderr are streamed back
    /// through a named pipe and it is terminated if the launcher exits.
    /// </summary>
    internal static class InteractiveDesktopRunner
    {
        private const string TaskFolderName = "FTToolsLauncher";
        private const string TaskPrefix = "Api-";
        private const string BACK_SLASH = @"\";
        private const int TaskRegistrationCreate = 2;
        private const int TaskLogonInteractiveToken = 3;
        private const int TaskRunUseSessionId = 4;
        private const int TaskStateRunning = 4;
        private const int TaskActionExec = 0;
        private const int TaskRunLevelLeastPrivilege = 0;
        private const int TaskInstancesIgnoreNew = 2;
        private const int TaskStopFlags = 0;
        private const int TaskDeleteFlags = 0;
        private const int TaskEnumerateHidden = 1;
        private const string TaskExecutionTimeLimit = "PT0S";
        private const int WtsActive = 0;
        private const int WtsUserName = 5;
        private const int WtsDomainName = 7;
        private const int WtsReserved = 0;
        private const int WtsVersion = 1;
        private const int SessionZero = 0;
        private const int PipeMaxInstances = 1;
        private const int DefaultPipeBufferSize = 0;
        private const int ProcessTreeWaitTimeoutMs = 10000;
        private const int ReaderShutdownTimeoutMs = 1000;
        internal const int PollingTimeMs = 500;
        private const string PowerShellArgs = "-NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -WindowStyle Hidden -EncodedCommand";
        private const string TaskKillExecutable = "taskkill.exe";
        private const string TaskKillArguments = "/PID {0} /T /F";
        private const string ScriptResourceName = "HpToolsLauncher.Common.Scripts.RunInDesktop.ps1";
        private const string ExitMarker = "\u0001FTL_EXIT:";
        private const string ErrorMarker = "\u0001FTL_ERR:";
        private const string PidMarker = "\u0001FTL_PID:";

        private static readonly string PowerShellFullPathExe = Path.Combine(Environment.SystemDirectory, @"WindowsPowerShell\v1.0\powershell.exe");
        private static readonly TimeSpan StartTimeout = TimeSpan.FromMinutes(1);
        private static readonly TimeSpan StaleTaskAge = TimeSpan.FromDays(1);

        [StructLayout(LayoutKind.Sequential)]
        private struct SessionInfo
        {
            public int SessionId;
            public IntPtr StationName;
            public int State;
        }

        private sealed class RunState
        {
            public readonly object SyncRoot = new();
            public dynamic Service;
            public dynamic Folder;
            public dynamic Task;
            public int? ExitCode;
            public int ExecutorPid;
            public string ScriptError;
            public bool Done;
            public string Stage = "connecting to Task Scheduler";
        }

        [DllImport("wtsapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool WTSEnumerateSessions(IntPtr server, int reserved, int version, out IntPtr sessions, out int count);

        [DllImport("wtsapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool WTSQuerySessionInformation(IntPtr server, int sessionId, int infoClass, out IntPtr buffer, out int bytes);

        [DllImport("wtsapi32.dll")]
        private static extern void WTSFreeMemory(IntPtr memory);

        public static int Run(string fileName, string arguments, string workingDirectory, RunAsUser runAsUser, Func<bool> runCancelled)
        {
            if (runCancelled())
                throw new OperationCanceledException("Process was stopped since job has timed out!");
            (SecurityIdentifier user, int sessionId) = GetTargetSession(runAsUser);

            int ftlPid = Process.GetCurrentProcess().Id;
            string runId = $"{ftlPid}-{DateTime.UtcNow.Ticks}";
            string taskName = $"{TaskPrefix}{runId}";
            string pipeName = $"{TaskFolderName}-{runId}";
            RunState state = new();
            try
            {
                return RunTask(fileName, arguments, workingDirectory, user, sessionId, ftlPid, taskName, pipeName, runCancelled, state);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                string details = e is COMException comException
                    ? $"HRESULT 0x{comException.ErrorCode:X8}: {comException.Message}"
                    : e.ToString();
                throw new InvalidOperationException($"Error while {state.Stage}, in session #{sessionId}: {details}", e);
            }
            finally
            {
                Cleanup(state, taskName);
            }
        }

        private static (SecurityIdentifier User, int SessionId) GetTargetSession(RunAsUser runAsUser)
        {
            SecurityIdentifier user;
            using (WindowsIdentity caller = WindowsIdentity.GetCurrent())
                user = runAsUser != null ? GetUserSid(runAsUser.Username) : caller.User;

            int sessionId = FindActiveSession(user);
            ConsoleWriter.WriteLine($"Using active desktop session #{sessionId} owned by user \"{Translate(user)}\".");
            return (user, sessionId);
        }

        private static int RunTask(string fileName, string arguments, string workingDirectory, SecurityIdentifier user,
            int sessionId, int ftlPid, string taskName, string pipeName, Func<bool> runCancelled, RunState state)
        {
            state.Service = Activator.CreateInstance(Type.GetTypeFromProgID("Schedule.Service", true));
            state.Service.Connect();
            state.Stage = "opening or creating the FTToolsLauncher task folder";
            state.Folder = GetOrCreateFolder(state.Service);
            state.Stage = "cleaning stale API tasks";
            DeleteStaleTasks(state.Folder);

            return CreateAndRunTask(fileName, arguments, workingDirectory, user, sessionId, ftlPid, taskName,
                pipeName, runCancelled, state);
        }

        private static int CreateAndRunTask(string fileName, string arguments, string workingDirectory,
            SecurityIdentifier user, int sessionId, int ftlPid, string taskName, string pipeName,
            Func<bool> runCancelled, RunState state)
        {
            state.Stage = "creating the output named pipe";
            using NamedPipeServerStream pipe = CreatePipe(pipeName, user);
            string script = BuildScript(fileName, arguments, workingDirectory, pipeName, ftlPid);
            state.Stage = "registering the temporary PowerShell task";
            state.Task = RegisterTask(state.Service, state.Folder, taskName, user, script, workingDirectory);
            ConsoleWriter.WriteLine($"API executor: task registered as \\{TaskFolderName}\\{taskName}.");

            Thread reader = StartOutputReader(pipe, state);
            try
            {
                state.Stage = "starting the scheduled task";
                state.Task.RunEx(null, TaskRunUseSessionId, sessionId, null);
                return WaitForTask(pipe, reader, sessionId, runCancelled, state);
            }
            finally
            {
                StopOutputReader(pipe, reader, state);
            }
        }

        private static void StopOutputReader(NamedPipeServerStream pipe, Thread reader, RunState state)
        {
            lock (state.SyncRoot)
                Volatile.Write(ref state.Done, true);

            pipe.Dispose();
            // State is frozen; a blocked console write must not delay cleanup indefinitely.
            reader.Join(ReaderShutdownTimeoutMs);
        }

        private static Thread StartOutputReader(NamedPipeServerStream pipe, RunState state)
        {
            Thread reader = new(() =>
            {
                try
                {
                    pipe.WaitForConnectionAsync().GetAwaiter().GetResult();
                    using StreamReader sr = new(pipe, new UTF8Encoding(false));
                    string line;
                    while ((line = sr.ReadLineAsync().GetAwaiter().GetResult()) != null)
                    {
                        lock (state.SyncRoot)
                        {
                            if (state.Done)
                                return;
                            if (line.StartsWithIgnoreCase(PidMarker))
                            {
                                state.ExecutorPid = int.Parse(line.Substring(PidMarker.Length));
                                continue;
                            }
                            if (line.StartsWithIgnoreCase(ExitMarker))
                            {
                                state.ExitCode = int.Parse(line.Substring(ExitMarker.Length));
                                continue;
                            }
                            if (line.StartsWithIgnoreCase(ErrorMarker))
                            {
                                state.ScriptError = line.Substring(ErrorMarker.Length);
                                continue;
                            }
                        }
                        if (line.Length > 0)
                            ConsoleWriter.WriteLine(line);
                    }
                }
                catch (Exception) when (Volatile.Read(ref state.Done))
                {
                    // pipe closed during shutdown
                }
                catch (Exception e)
                {
                    ConsoleWriter.WriteErrLine($"API executor output stream failed: {e.Message}");
                }
            })
            { IsBackground = true, Name = "API executor output" };
            reader.Start();
            return reader;
        }

        private static int WaitForTask(NamedPipeServerStream pipe, Thread reader, int sessionId, Func<bool> runCancelled,
            RunState state)
        {
            state.Stage = "waiting for the API executor";
            Stopwatch elapsed = Stopwatch.StartNew();
            while (!reader.Join(PollingTimeMs))
            {
                if (runCancelled())
                    throw new OperationCanceledException("Process was stopped since job has timed out!");
                if (!pipe.IsConnected && elapsed.Elapsed >= StartTimeout)
                {
                    int startResult = state.Task.LastTaskResult;
                    throw new TimeoutException($"Failed to connect from desktop session #{sessionId} within {StartTimeout.TotalSeconds:F0} seconds (Task Scheduler result 0x{startResult:X8}).");
                }
            }

            if (state.ScriptError != null)
                throw new InvalidOperationException($"API executor could not be started: {state.ScriptError}");
            if (state.ExitCode.HasValue)
            {
                ConsoleWriter.WriteLine($"API executor exited with code {state.ExitCode.Value} after {elapsed.Elapsed.TotalSeconds:F0} seconds.");
                return state.ExitCode.Value;
            }
            int taskResult = state.Task.LastTaskResult;
            throw new InvalidOperationException($"API executor task ended without reporting an exit code (Task Scheduler result 0x{taskResult:X8}).");
        }

        private static void Cleanup(RunState state, string taskName)
        {
            int? exitCode;
            int executorPid;
            lock (state.SyncRoot)
            {
                exitCode = state.ExitCode;
                executorPid = state.ExecutorPid;
            }
            if (!exitCode.HasValue)
                KillTree(executorPid);
            if (state.Task != null)
            {
                try
                {
                    if (state.Task.State == TaskStateRunning)
                        state.Task.Stop(TaskStopFlags);
                }
                catch (Exception e)
                {
                    ConsoleWriter.WriteErrLine($"Unable to stop API executor task: {e.Message}");
                }
                try
                {
                    state.Folder.DeleteTask(taskName, TaskDeleteFlags);
                }
                catch (Exception e)
                {
                    ConsoleWriter.WriteErrLine($"Unable to remove API executor task \\{TaskFolderName}\\{taskName}: {e.Message}");
                }
            }
            Release(state.Task);
            Release(state.Folder);
            Release(state.Service);
        }

        private static NamedPipeServerStream CreatePipe(string pipeName, SecurityIdentifier user)
        {
            PipeSecurity security = new();
            security.AddAccessRule(new PipeAccessRule(user, PipeAccessRights.ReadWrite, AccessControlType.Allow));
            using (WindowsIdentity self = WindowsIdentity.GetCurrent())
                security.AddAccessRule(new PipeAccessRule(self.User, PipeAccessRights.FullControl, AccessControlType.Allow));
            return new NamedPipeServerStream(pipeName, PipeDirection.In, PipeMaxInstances, PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous, DefaultPipeBufferSize, DefaultPipeBufferSize, security);
        }

        private static dynamic GetOrCreateFolder(dynamic service)
        {
            dynamic root = service.GetFolder(BACK_SLASH);
            try
            {
                return service.GetFolder($@"\{TaskFolderName}");
            }
            catch (FileNotFoundException)
            {
                return root.CreateFolder(TaskFolderName);
            }
            finally
            {
                Release(root);
            }
        }

        private static void DeleteStaleTasks(dynamic folder)
        {
            dynamic tasks = folder.GetTasks(TaskEnumerateHidden);
            try
            {
                List<string> stale = [];
                foreach (dynamic t in tasks)
                {
                    try
                    {
                        string name = t.Name;
                        DateTime registered = DateTime.Parse((string)t.Definition.RegistrationInfo.Date);
                        if (name.StartsWithIgnoreCase(TaskPrefix) && DateTime.Now - registered > StaleTaskAge)
                            stale.Add(name);
                    }
                    catch (Exception)
                    {
                        // ignore tasks we cannot inspect
                    }
                    finally
                    {
                        Release(t);
                    }
                }
                foreach (string name in stale)
                {
                    try { folder.DeleteTask(name, TaskDeleteFlags); }
                    catch (Exception) { /* owned by another user */ }
                }
            }
            finally
            {
                Release(tasks);
            }
        }

        private static dynamic RegisterTask(dynamic service, dynamic folder, string taskName, SecurityIdentifier user,
            string script, string workDir)
        {
            dynamic def = service.NewTask(0);
            try
            {
                def.RegistrationInfo.Date = DateTime.Now.ToString("s");
                def.RegistrationInfo.Description = "Temporary FTToolsLauncher API test run.";
                def.Principal.UserId = user.Value;
                def.Principal.LogonType = TaskLogonInteractiveToken;
                def.Principal.RunLevel = TaskRunLevelLeastPrivilege;
                def.Settings.Enabled = true;
                def.Settings.Hidden = true;
                def.Settings.AllowDemandStart = true;
                def.Settings.AllowHardTerminate = true;
                def.Settings.DisallowStartIfOnBatteries = false;
                def.Settings.StopIfGoingOnBatteries = false;
                def.Settings.MultipleInstances = TaskInstancesIgnoreNew;
                def.Settings.ExecutionTimeLimit = TaskExecutionTimeLimit;

                dynamic action = def.Actions.Create(TaskActionExec);
                action.Path = PowerShellFullPathExe;
                action.Arguments = $"{PowerShellArgs} {Encoding.Unicode.GetBytes(script).ToBase64String()}";
                action.WorkingDirectory = workDir;
                Release(action);

                return folder.RegisterTaskDefinition(taskName, def, TaskRegistrationCreate, user.Value, null, TaskLogonInteractiveToken, null);
            }
            finally
            {
                Release(def);
            }
        }

        private static string BuildScript(string fileName, string arguments, string workingDirectory, string pipeName, int launcherPid)
        {
            using Stream stream = typeof(InteractiveDesktopRunner).Assembly.GetManifestResourceStream(ScriptResourceName)
                ?? throw new InvalidOperationException($"Embedded PowerShell resource '{ScriptResourceName}' was not found.");
            using StreamReader reader = new(stream, Encoding.UTF8);
            string script = reader.ReadToEnd();
            static string Quote(string value) => "'" + value.Replace("'", "''") + "'";

            return $"{script}{Environment.NewLine}Invoke-FTLDesktopProcess -FileName {Quote(fileName)} -Arguments {Quote(arguments)} -WorkingDirectory {Quote(workingDirectory)} -PipeName {Quote(pipeName)} -LauncherPid {launcherPid}";
        }

        private static void KillTree(int pid)
        {
            if (pid <= 0)
                return;
            try
            {
                using Process p = Process.Start(new ProcessStartInfo(TaskKillExecutable, string.Format(TaskKillArguments, pid))
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                });
                p?.WaitForExit(ProcessTreeWaitTimeoutMs);
            }
            catch (Exception e)
            {
                ConsoleWriter.WriteErrLine($"Unable to stop API executor process tree {pid}: {e.Message}");
            }
        }

        private static int FindActiveSession(SecurityIdentifier user)
        {
            if (!WTSEnumerateSessions(IntPtr.Zero, WtsReserved, WtsVersion, out IntPtr sessions, out int count))
                throw new Win32Exception(Marshal.GetLastWin32Error(), "WTSEnumerateSessions failed.");

            List<int> matches = [];
            try
            {
                int size = Marshal.SizeOf(typeof(SessionInfo));
                for (int i = 0; i < count; i++)
                {
                    SessionInfo s = Marshal.PtrToStructure<SessionInfo>(IntPtr.Add(sessions, i * size));
                    if (s.SessionId == SessionZero || s.State != WtsActive)
                        continue;
                    SecurityIdentifier sid = GetSessionUserSid(s.SessionId);
                    if (sid != null && sid.Equals(user))
                        matches.Add(s.SessionId);
                }
            }
            finally
            {
                WTSFreeMemory(sessions);
            }

            if (matches.Count == 0)
                throw new InvalidOperationException($"No active desktop session found for user {Translate(user)}. Sign in to the VM (console or RDP) with this account.");
            if (matches.Count > 1)
                throw new InvalidOperationException($"User {Translate(user)} has multiple active desktop sessions ({string.Join(", ", matches)}); keep only one.");
            return matches[0];
        }

        private static SecurityIdentifier GetSessionUserSid(int sessionId)
        {
            string user = QuerySessionString(sessionId, WtsUserName);
            if (string.IsNullOrEmpty(user))
                return null;
            string domain = QuerySessionString(sessionId, WtsDomainName);
            NTAccount account = string.IsNullOrEmpty(domain) ? new NTAccount(user) : new NTAccount(domain, user);
            return (SecurityIdentifier)account.Translate(typeof(SecurityIdentifier));
        }

        private static string QuerySessionString(int sessionId, int infoClass)
        {
            if (!WTSQuerySessionInformation(IntPtr.Zero, sessionId, infoClass, out IntPtr buffer, out _))
                throw new Win32Exception(Marshal.GetLastWin32Error(), $"WTSQuerySessionInformation failed for session {sessionId}.");
            try
            {
                return buffer == IntPtr.Zero ? string.Empty : Marshal.PtrToStringUni(buffer);
            }
            finally
            {
                WTSFreeMemory(buffer);
            }
        }

        private static SecurityIdentifier GetUserSid(string username)
        {
            if (username.StartsWith(@".\", StringComparison.Ordinal))
                username = Environment.MachineName + username.Substring(1);
            return (SecurityIdentifier)new NTAccount(username).Translate(typeof(SecurityIdentifier));
        }

        private static string Translate(SecurityIdentifier sid)
        {
            try { return sid.Translate(typeof(NTAccount)).Value; }
            catch (IdentityNotMappedException) { return sid.Value; }
        }

        private static void Release(object instance)
        {
            if (instance != null && Marshal.IsComObject(instance))
                Marshal.ReleaseComObject(instance);
        }
    }
}

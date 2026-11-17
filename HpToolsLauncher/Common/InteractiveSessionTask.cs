using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading;

namespace HpToolsLauncher.Common
{
    internal static class InteractiveSessionTask
    {
        private const int TaskCreate = 2;
        private const int TaskLogonInteractiveToken = 3;
        private const int TaskRunUseSessionId = 4;
        private const int TaskStateQueued = 2;
        private const int TaskStateRunning = 4;
        private const int TaskHasNotRun = 0x41303;

        public static int Run(string fileName, string arguments, string workingDirectory, int sessionId,
            SecurityIdentifier user, Func<bool> runCancelled, int pollingTimeMs)
        {
            if (sessionId <= 0)
                throw new ArgumentOutOfRangeException(nameof(sessionId), "An interactive desktop session is required.");
            if (pollingTimeMs <= 0)
                throw new ArgumentOutOfRangeException(nameof(pollingTimeMs));
            if (runCancelled())
                throw new OperationCanceledException("Process was stopped since job has timed out!");

            string taskName = "FTToolsLauncher-Api-" + Guid.NewGuid().ToString("N");
            dynamic service = null;
            dynamic folder = null;
            dynamic definition = null;
            dynamic principal = null;
            dynamic settings = null;
            dynamic actions = null;
            dynamic action = null;
            dynamic task = null;
            dynamic runningTask = null;
            bool runRequested = false;
            bool completed = false;
            try
            {
                service = Activator.CreateInstance(Type.GetTypeFromProgID("Schedule.Service", true));
                service.Connect();
                folder = service.GetFolder(@"\");
                definition = service.NewTask(0);
                principal = definition.Principal;
                principal.UserId = user.Value;
                principal.LogonType = TaskLogonInteractiveToken;
                principal.RunLevel = 0;

                settings = definition.Settings;
                settings.Enabled = true;
                settings.AllowDemandStart = true;
                settings.AllowHardTerminate = true;
                settings.DisallowStartIfOnBatteries = false;
                settings.StopIfGoingOnBatteries = false;
                settings.ExecutionTimeLimit = "PT0S";

                actions = definition.Actions;
                action = actions.Create(0); // TASK_ACTION_EXEC: launch the executor directly, not another launcher.
                action.Path = fileName;
                action.Arguments = arguments;
                action.WorkingDirectory = workingDirectory;
                task = folder.RegisterTaskDefinition(taskName, definition, TaskCreate,
                    user.Value, null, TaskLogonInteractiveToken, null);

                ConsoleWriter.WriteLine($"API executor task: \\{taskName}; target session {sessionId}; user SID {user.Value}; interactive-token logon (no password stored).");
                if (runCancelled())
                    throw new OperationCanceledException("Process was stopped since job has timed out!");
                DateTime previousRun = task.LastRunTime;
                runRequested = true;
                runningTask = task.RunEx(null, TaskRunUseSessionId, sessionId, null);
                if (runningTask == null)
                    throw new InvalidOperationException($"Task Scheduler did not return an instance for API executor task {taskName}.");

                Stopwatch elapsed = Stopwatch.StartNew();
                TimeSpan nextStatus = TimeSpan.FromSeconds(30);
                bool started = false;
                while (true)
                {
                    if (runCancelled())
                        throw new OperationCanceledException("Process was stopped since job has timed out!");

                    // The task is unique and has no triggers, so its result belongs only to this run.
                    int state = task.State;
                    int result = task.LastTaskResult;
                    DateTime lastRun = task.LastRunTime;
                    started |= state == TaskStateRunning || lastRun != previousRun;
                    if (state != TaskStateQueued && state != TaskStateRunning && (started || result != TaskHasNotRun))
                    {
                        completed = true;
                        ConsoleWriter.WriteLine($"API executor task {taskName} completed with result {result} (0x{result:X8}) after {elapsed.Elapsed.TotalSeconds:F0} seconds.");
                        return result;
                    }
                    if (!started && elapsed.Elapsed >= TimeSpan.FromMinutes(1))
                        throw new TimeoutException($"API executor task {taskName} did not start in desktop session {sessionId} within 60 seconds. Check Task Scheduler history and that the user is still signed in.");
                    if (elapsed.Elapsed >= nextStatus)
                    {
                        ConsoleWriter.WriteLine($"API executor task {taskName}, target session {sessionId}, is {(started ? "running" : "waiting to start")} after {elapsed.Elapsed.TotalSeconds:F0} seconds; live test output is not forwarded on this launch path.");
                        nextStatus = elapsed.Elapsed + TimeSpan.FromSeconds(30);
                    }
                    Thread.Sleep(pollingTimeMs);
                }
            }
            catch (COMException e)
            {
                throw new InvalidOperationException($"Task Scheduler could not run the API executor in desktop session {sessionId} (HRESULT 0x{e.ErrorCode:X8}): {e.Message} Ensure the Task Scheduler service is running and the service account can register and run an interactive-token task for the selected user.", e);
            }
            finally
            {
                bool canDelete = true;
                if (task != null && runRequested && !completed)
                {
                    try
                    {
                        task.Stop(0);
                    }
                    catch (Exception e)
                    {
                        canDelete = false;
                        ConsoleWriter.WriteErrLine($"Unable to stop API executor task \\{taskName}: {e.Message}. The task was retained for manual inspection and cleanup.");
                    }
                }
                if (task != null && canDelete)
                {
                    try
                    {
                        folder.DeleteTask(taskName, 0);
                    }
                    catch (Exception e)
                    {
                        ConsoleWriter.WriteErrLine($"Unable to remove temporary API executor task \\{taskName}: {e.Message}");
                    }
                }
                Release((object)runningTask);
                Release((object)task);
                Release((object)action);
                Release((object)actions);
                Release((object)settings);
                Release((object)principal);
                Release((object)definition);
                Release((object)folder);
                Release((object)service);
            }
        }

        private static void Release(object instance)
        {
            if (instance != null && Marshal.IsComObject(instance))
                Marshal.ReleaseComObject(instance);
        }
    }
}

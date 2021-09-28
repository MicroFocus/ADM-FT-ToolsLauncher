using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HpToolsLauncher.Properties;

namespace HpToolsLauncher
{
    public enum TestStorageType
    {
        Alm,
        AlmLabManagement,
        FileSystem,
        LoadRunner,
        MBT,
        Unknown
    }

    static class Program
    {
        private static readonly Dictionary<string, string> argsDictionary = new Dictionary<string, string>();

        //[MTAThread]
        static void Main(string[] args)
        {
            ConsoleQuickEdit.Disable();
            ConsoleWriter.Initialize();
            if (!args.Any() || args.Contains("/?") || args.Contains("-h") || args.Contains("-help") || args.Contains("/h"))
            {
                ShowHelp();
                return;
            }
            // show version?
            if (args[0] == "-v" || args[0] == "-version" || args[0] == "/v")
            {
                Console.WriteLine(Assembly.GetEntryAssembly().GetName().Version.ToString());
                Environment.Exit(0);
                return;
            }

            for (int i = 0; i < args.Length; i += 2)
            {
                string arg = args[i].Trim();
                if (arg.StartsWith("--"))
                {
                    // --<flag> means it is a flag without value
                    string key = arg.Substring(2);
                    argsDictionary[key] = string.Empty;
                    i -= 1;
                }
                else
                {
                    string key = arg.StartsWith("-") ? arg.Substring(1) : arg;
                    string val = i + 1 < args.Length ? args[i + 1].Trim() : string.Empty;
                    argsDictionary[key] = val;
                }
            }

            // verbose output mode?
            if (argsDictionary.ContainsKey("verbose"))
            {
                ConsoleWriter.EnableVerboseOutput = true;
            }

            // redirect output if required
            string redirectOutPipeName;
            if (argsDictionary.TryGetValue("redirect-out-pipe", out redirectOutPipeName))
            {
                // need to redirect stdout to the specified pipe
                var outPipe = new NamedPipeClientStream(".", redirectOutPipeName, PipeDirection.Out);
                outPipe.Connect();
                // create stream write for stdout
                var sw = new StreamWriter(outPipe)
                {
                    AutoFlush = true
                };
                Console.SetOut(sw);

                ConsoleWriter.WriteVerboseLine("The stdout is redirected via the named pipe: " + redirectOutPipeName);
            }

            // redirect error if required
            string redirectErrPipeName;
            if (argsDictionary.TryGetValue("redirect-err-pipe", out redirectErrPipeName))
            {
                // need to redirect stderr to the specified pipe
                var errPipe = new NamedPipeClientStream(".", redirectErrPipeName, PipeDirection.Out);
                errPipe.Connect();
                // create stream write for stderr
                var sw = new StreamWriter(errPipe)
                {
                    AutoFlush = true
                };
                Console.SetError(sw);

                ConsoleWriter.WriteVerboseLine("The stderr is redirected via the named pipe: " + redirectErrPipeName);
            }

            string paramFileName, runtype;
            string failOnTestFailed = "N";
            argsDictionary.TryGetValue("runtype", out runtype);
            argsDictionary.TryGetValue("paramfile", out paramFileName);
            TestStorageType enmRuntype = TestStorageType.Unknown;

            if (!Enum.TryParse<TestStorageType>(runtype, true, out enmRuntype))
                enmRuntype = TestStorageType.Unknown;

            if (string.IsNullOrEmpty(paramFileName))
            {
                ShowHelp();
                return;
            }

            ShowTitle();
            ConsoleWriter.WriteLine(Resources.GeneralStarted);

            try
            {
                if (StartNewLauncherProcess(args))
                {
                    // the new launcher process is launched and everthing shall already be handled in the StartNewLauncherProcess
                    // so here returns, that is, this process shall exit
                    return;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Warning: Error occurred when creating the new process in the user session:");
                Console.Error.WriteLine("-------------------------------");
                Console.Error.WriteLine(ex.Message);                
                ConsoleWriter.WriteVerboseLine(ex.ToString());
                Console.Error.WriteLine("-------------------------------");
                Console.Error.WriteLine("Warning: Test(s) will be run in non-user session, however, the test(s) might fail.");
            }

            var launcher = new Launcher(failOnTestFailed, paramFileName, enmRuntype);
            if (launcher.IsParamFileEncodingNotSupported)
            {
                Console.WriteLine(Properties.Resources.JavaPropertyFileBOMNotSupported);
                Environment.Exit((int)Launcher.ExitCodeEnum.Failed);
                return;
            }

            launcher.Run();
        }

        private static string GetProgramTitle()
        {
            Assembly assembly = Assembly.GetEntryAssembly();
            return ((AssemblyTitleAttribute)Attribute.GetCustomAttribute(assembly, typeof(AssemblyTitleAttribute), false)).Title;
        }

        private static void ShowTitle()
        {
            Assembly assembly = Assembly.GetEntryAssembly();
            Console.WriteLine("Micro Focus Automation Tools - {0} {1} ", GetProgramTitle(), assembly.GetName().Version.ToString());
            Console.WriteLine();
        }

        private static void ShowHelp()
        {
            string programName = Assembly.GetEntryAssembly().GetName().Name;

            ShowTitle();
            Console.WriteLine("Usage: {0} -v|-version", programName);
            Console.WriteLine("  Show program version");
            Console.WriteLine();
            Console.Write("Usage: {0} -paramfile ", programName);
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("<a file in key=value format> ");
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine("  Execute tests with parameters");
            Console.WriteLine();
            Console.WriteLine("  -paramfile is required and the parameters file may contain the following mandatory fields:");
            Console.WriteLine("\t# Basic parameters");
            Console.WriteLine("\trunType=FileSystem|Alm");
            Console.WriteLine("\tresultsFilename=<result-file>");
            Console.WriteLine();
            Console.WriteLine("\t# ALM parameters");
            Console.WriteLine("\talmServerUrl=http(s)://<server>:<port>/qcbin");
            Console.WriteLine("\talmUsername=<username>");
            Console.WriteLine("\talmPasswordBasicAuth=<base64-password>");
            Console.WriteLine("\talmDomain=<domain>");
            Console.WriteLine("\talmProject=<project>");
            Console.WriteLine("\talmRunMode=RUN_LOCAL|RUN_REMOTE|RUN_PLANNED_HOST");
            Console.WriteLine("\tTestSet<i:1-to-n>=<test-set-path>|<Alm-folder>");
            Console.WriteLine();
            Console.WriteLine("\t# File System parameters");
            Console.WriteLine("\tTest<i:1-to-n>=<test-folder>|<path-to-test-folders>|<.lrs file>|<.mtb file>|<.mtbx file>");
            Console.WriteLine();
            Console.WriteLine("\t# Mobile Center parameters");
            Console.WriteLine("\tMobileHostAddress=http(s)://<server>:<port>");
            Console.WriteLine("\tMobileUserName=<username>");
            Console.WriteLine("\tMobilePasswordBasicAuth=<base64-password>");
            Console.WriteLine("\tMobileTenantId=<mc-tenant-id>");
            Console.WriteLine();
            Console.WriteLine("\t# Parallel Runner parameters");
            Console.WriteLine("\tparallelRunnerMode=true|false");
            Console.WriteLine("\tParallelTest<i>Env<j>=<key>:<value>[,...]");
            Console.WriteLine();
            Console.WriteLine("* For the details of the entire parameter list, see the online README on GitHub.");
            Environment.Exit((int)Launcher.ExitCodeEnum.Failed);
        }

        private static bool StartNewLauncherProcess(string[] args)
        {
            ConsoleWriter.WriteVerboseLine("Attempt to start a new launcher process, in an available user session ...");

            // run in current session?
            string tmp;
            bool is_current_session = argsDictionary.TryGetValue("current-session", out tmp);
            if (is_current_session && !string.IsNullOrWhiteSpace(tmp) && tmp.ToLower() == "false")
            {
                is_current_session = false;
            }

            if (is_current_session)
            {
                // no need to create new session, let main process continue to run in the current session
                // return false to indicate the new process is not launched
                ConsoleWriter.WriteVerboseLine("The --current-session flag is set, continue to run tests in the current program.");
                return false;
            }

            // try to get launcher tool location from the command line or environment variable
            // this path will be used to launcher the new process
            bool useCurrentProcessPath = false;
            string launcherToolPath;
            if (!argsDictionary.TryGetValue("tool-path", out launcherToolPath))
            {
                ConsoleWriter.WriteVerboseLine("The -tool-path flag is not set, try the environment variable 'FTTOOLSLAUNCHER_TOOL_PATH'.");

                // not given in command line, try to find in env
                launcherToolPath = Environment.GetEnvironmentVariable("FTTOOLSLAUNCHER_TOOL_PATH");
            }

            if (string.IsNullOrWhiteSpace(launcherToolPath))
            {
                ConsoleWriter.WriteVerboseLine("The launcher tool path is not specified in command line nor in environment variable, use current program's path.");

                // neither from command line nor from env is found, use current one
                launcherToolPath = Assembly.GetExecutingAssembly().Location;
                useCurrentProcessPath = true;
            }

            launcherToolPath = Path.GetFullPath(launcherToolPath);
            if (!File.Exists(launcherToolPath))
            {
                Console.Error.WriteLine("Error: Can't find the launcher tool: {0}. Use current launcher tool path.", launcherToolPath);
                return false;
            }

            // if the current session is a user interactive session and the launcherToolPath is same as the current running program
            // it means it is not necessary to create a new process in the current session again
            // the current running program shall be good to go
            if (Environment.UserInteractive && useCurrentProcessPath)
            {
                ConsoleWriter.WriteVerboseLine("The current session is a user-interactive session, and the launcher tool path is same as the current program's, continue to run tests in the current program.");
                return false;
            }

            // now, we can ensure that a new process need to be launched

            // prepare out/err redirection for the new process
            int pid = Process.GetCurrentProcess().Id;
            string redirOutPipeName = string.Format("toolslauncher_pid_{0}_out", pid);
            string redirErrPipeName = string.Format("toolslauncher_pid_{0}_err", pid);

            // create pipe server(s) to receive the redirected out/err from the new process
            var redirOutPipeSever = new NamedPipeServerStream(redirOutPipeName, PipeDirection.In);
            var redirErrPipeSever = new NamedPipeServerStream(redirErrPipeName, PipeDirection.In);

            ConsoleWriter.WriteVerboseLine("The stdout redirection named pipe server is created: " + redirOutPipeName);
            ConsoleWriter.WriteVerboseLine("The stderr redirection named pipe server is created: " + redirErrPipeName);

            // build command line for the new process
            string cmdLine = string.Empty;
            foreach (string arg in args) 
            {
                if (arg.Contains(" "))
                {
                    cmdLine += string.Format(" \"{0}\" ", arg);
                }
                else
                {
                    cmdLine += string.Format(" {0} ", arg);
                }
            }
            // tell the newly created process that the new process shall always run in that session
            // use "--<flag>" to indicate this is a boolean flag without argument followed by
            cmdLine += " --current-session";
            // tell the newly created process that the new process shall redirect its stdout and stderr via the pipes
            cmdLine += string.Format(" -redirect-out-pipe \"{0}\"", redirOutPipeName);
            cmdLine += string.Format(" -redirect-err-pipe \"{0}\"", redirErrPipeName);

            ConsoleWriter.WriteVerboseLine(string.Format("The command line to start the new launcher process is: {0} {1}", launcherToolPath, cmdLine));

            // start process
            string workingDir = Directory.GetCurrentDirectory();
            ProcessExtensions.IProcessInfo procInfo = null;
            if (!Environment.UserInteractive)
            {
                Console.WriteLine("Running a new launcher process in available user session. Launcher tool path: {0}", launcherToolPath);
                var userSessionProcInfo = ProcessExtensions.StartProcessInUserSession(launcherToolPath, cmdLine, workingDir);
                if (userSessionProcInfo == null)
                {
                    // process is not started
                    Console.Error.WriteLine("Warning: Process is not started in the active user session.");
                    Console.Error.WriteLine("Warning: Test(s) will be run in current (non-user) session, however, some test(s) might fail. To avoid failure, log on an user or keep the connected remote desktop active.");
                    return false;
                }
                Console.WriteLine("The new launcher process is started in session: {0}. PID: {1}", userSessionProcInfo.ActiveSessionID, userSessionProcInfo.PID);
                procInfo = userSessionProcInfo;
            }
            else
            {
                Console.WriteLine("Runnnig a new launcher process in current session. Launcher tool path: {0}", launcherToolPath);
                procInfo = ProcessExtensions.StartProcessInCurrentSession(launcherToolPath, cmdLine, workingDir, false);
                Console.WriteLine("The new launcher process is started in current session. PID: {0}", procInfo.PID);
            }

            Console.WriteLine("The following output comes from the new launcher process.");
            Console.WriteLine("##################### Start - Output from PID {0} #####################", procInfo.PID);
            Console.WriteLine();

            Task.Run(() =>
            {
                redirOutPipeSever.WaitForConnection();
                using (StreamReader sr = new StreamReader(redirOutPipeSever))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        Console.Out.WriteLine(line);
                    }
                }
            });
            Task.Run(() =>
            {
                redirErrPipeSever.WaitForConnection();
                using (StreamReader sr = new StreamReader(redirErrPipeSever))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        Console.Error.WriteLine(line);
                    }
                }
            });

            // wait for newly launched process exit
            int exitCode = procInfo.WaitForExit(-1);

            Console.WriteLine();
            Console.WriteLine("##################### End - Output from PID {0} #####################", procInfo.PID);

            Console.WriteLine("The new launcher process (PID={0}) exited with error code: {1}.", procInfo.PID, exitCode);

            procInfo.Release();
            Environment.Exit(exitCode);

            return true;
        }
    }
}
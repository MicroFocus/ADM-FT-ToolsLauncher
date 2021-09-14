using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Reflection;
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
                    return;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Warning: Error occurred when start new session process:");
                Console.Error.WriteLine(ex.Message);
                Console.Error.WriteLine("The above error is ignored and continue to run the tests.");
            }

            var apiRunner = new Launcher(failOnTestFailed, paramFileName, enmRuntype);
            if (apiRunner.IsParamFileEncodingNotSupported)
            {
                Console.WriteLine(Properties.Resources.JavaPropertyFileBOMNotSupported);
                Environment.Exit((int)Launcher.ExitCodeEnum.Failed);
                return;
            }

            apiRunner.Run();
        }

        private static void ShowTitle()
        {
            AssemblyName assembly = Assembly.GetEntryAssembly().GetName();
            Console.WriteLine("Micro Focus Automation Tools - {0} {1} ", assembly.Name, assembly.Version.ToString());
            Console.WriteLine();
        }

        private static void ShowHelp()
        {
            AssemblyName assembly = Assembly.GetEntryAssembly().GetName();

            ShowTitle();
            Console.WriteLine("Usage: {0} -v|-version", assembly.Name);
            Console.WriteLine("  Show program version");
            Console.WriteLine();
            Console.Write("Usage: {0} -paramfile ", assembly.Name);
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
            string tmp;
            if (argsDictionary.TryGetValue("no-new-session", out tmp))
            {
                // no need to create new session, let main process continue
                // We are in second instance
                var stdPipe = new NamedPipeServerStream(ProcessExtensions.ToolsLauncherStdPipeName, PipeDirection.Out);
                stdPipe.WaitForConnection();

                var stdStream = new StreamWriter(stdPipe);
                stdStream.AutoFlush = true;

                Console.SetError(stdStream);
                Console.SetOut(stdStream);				
                return false;
            }

            // try to get launcher tool location from the command line
            bool useCurrentProcessPath = false;
            string launcherToolPath = string.Empty;
            if (!argsDictionary.TryGetValue("session-tool-path", out launcherToolPath))
            {
                // not given in command line, try to find in env
                launcherToolPath = Environment.GetEnvironmentVariable("FTTOOLSLAUNCHER_SESSION_TOOL_PATH");
            }

            if (string.IsNullOrWhiteSpace(launcherToolPath))
            {
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
            cmdLine += " --no-new-session"; // tell the newly created process that it must NOT start another process

            string workingDir = System.IO.Directory.GetCurrentDirectory();

            if (!Environment.UserInteractive)
            {
                Console.WriteLine("Run a new launcher process in user session. Launcher tool path: {0}", launcherToolPath);

                int exitCode = ProcessExtensions.StartProcessFromUserSession(launcherToolPath, cmdLine, workingDir, false);
                Environment.Exit(exitCode);
            }
            else if (!useCurrentProcessPath)
            {
                Console.WriteLine("Run a new launcher process in current session. Launcher tool path: {0}", launcherToolPath);

                int exitCode = ProcessExtensions.StartProcessFromCurrentSession(launcherToolPath, cmdLine, workingDir, false);
                Environment.Exit(exitCode);
            }
            else
            {
                // no session is created, and the tools launcher path is same as the current exe, no need to start a new process
                // just use this process to run test
                Console.WriteLine("The launcher tool is running in user interactive mode.");
                return false;
            }

            return true;
        }
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ReportConverter
{
    class ArgHelper
    {
        private const string ShortFormArgIndicator = "-";
        private const string LongFormArgIndicator = "--";
        private const string AlternativeArgIndicator = "/";

        private static readonly ArgHelper _instance = new ArgHelper();

        private List<OptArgInfo> _optArgs;
        private List<PosArgInfo> _posArgs;

        public static ArgHelper Instance { get { return _instance; } }

        static ArgHelper() { }

        private ArgHelper()
        {
            _optArgs = new List<OptArgInfo>();
            _posArgs = new List<PosArgInfo>();

            Init();
        }

        private void Init()
        {
            Type t = typeof(CommandArguments);
            PropertyInfo[] props = t.GetProperties();
            foreach (PropertyInfo pi in props)
            {
                ArgDescriptionAttribute argDesc = pi.GetCustomAttribute<ArgDescriptionAttribute>();
                OptionalArgAttribute optArg = pi.GetCustomAttribute<OptionalArgAttribute>();
                if (optArg != null)
                {
                    _optArgs.Add(new OptArgInfo(pi, optArg)
                    {
                        Value = pi.GetCustomAttribute<OptionalArgValueAttribute>(),
                        Description = argDesc
                    });
                    continue;
                }

                PositionalArgAttribute posArg = pi.GetCustomAttribute<PositionalArgAttribute>();
                if (posArg != null)
                {
                    _posArgs.Add(new PosArgInfo(pi, posArg)
                    {
                        Description = argDesc
                    });
                    continue;
                }
            }

            _posArgs.Sort((x, y) =>
            {
                if (x == null && y == null) return 0;
                else if (x == null) return -1;
                else if (y == null) return 1;
                else return x.Argument.Position - y.Argument.Position;
            });
        }

        private bool IsOptionalArgument(string arg)
        {
            if (string.IsNullOrWhiteSpace(arg) || arg.Length < 2)
            {
                return false;
            }

            if (arg.StartsWith(ShortFormArgIndicator) ||
                arg.StartsWith(LongFormArgIndicator) ||
                arg.StartsWith(AlternativeArgIndicator))
            {
                return true;
            }

            return false;
        }

        private bool IsOptionalArgument(string arg, IEnumerable<string> argNames)
        {
            foreach (string name in argNames)
            {
                if (name.Length == 1)
                {
                    string n1 = ShortFormArgIndicator + name;
                    string n2 = AlternativeArgIndicator + name;
                    if (n1 == arg || n2 == arg)
                    {
                        return true;
                    }
                }
                else if (name.Length > 1)
                {
                    string n1 = LongFormArgIndicator + name;
                    string n2 = AlternativeArgIndicator + name;
                    if (n1 == arg || n2 == arg)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public CommandArguments ParseCommandArguments(string[] args)
        {
            CommandArguments cmdArgs = new CommandArguments();

            int pos = -1;
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                if (IsOptionalArgument(arg))
                {
                    // optional argument
                    foreach (OptArgInfo optArg in _optArgs)
                    {
                        if (IsOptionalArgument(arg, optArg.Argument.Names))
                        {
                            Type propertyType = optArg.PropertyInfo.PropertyType;
                            if (propertyType == typeof(bool))
                            {
                                optArg.PropertyInfo.SetValue(cmdArgs, true);
                                break;
                            }
                            else if (propertyType == typeof(string))
                            {
                                i++;
                                if (i >= args.Length)
                                {
                                    OutputWriter.WriteLine("The value argument is not specified for {0}", optArg.Argument.FirstName);
                                }
                                else
                                {
                                    optArg.PropertyInfo.SetValue(cmdArgs, args[i]);
                                }
                                break;
                            }
                            else
                            {
                                // not supported property type
                                OutputWriter.WriteLine("Warning: Cannot parse '{0}' to type {1}", optArg.Argument.FirstName, propertyType.FullName);
                            }
                        }
                    }
                }
                else
                {
                    // positional argument
                    pos++;
                    PosArgInfo posArg = _posArgs[pos];
                    posArg.PropertyInfo.SetValue(cmdArgs, arg);
                }
            }

            return cmdArgs;
        }

        public void WriteUsage(TextWriter w, string programName = "")
        {
            if (w == null)
            {
                return;
            }

            // one-line usage
            if (string.IsNullOrWhiteSpace(programName))
            {
                programName = Assembly.GetEntryAssembly().GetName().Name;
            }

            string argsUsageLine = string.Empty;
            if (_optArgs.Count > 0)
            {
                argsUsageLine += " [<options>]";
            }
            foreach (PosArgInfo posArg in _posArgs)
            {
                argsUsageLine += " " + posArg.Argument.PlaceholderText;
            }

            w.WriteLine(Properties.Resources.Prog_Usage_OneLine, programName + argsUsageLine);
            w.WriteLine();

            const string indentLv1 = "  ";
            const string indentLv2 = "    ";

            // positional argument details
            if (_posArgs.Count > 0)
            {
                foreach (PosArgInfo posArg in _posArgs)
                {
                    if (posArg.Description != null && posArg.Description.Lines.Count() > 0)
                    {
                        w.WriteLine(posArg.Argument.PlaceholderText);
                        foreach (string line in posArg.Description.Lines)
                        {
                            w.WriteLine(indentLv1 + line);
                        }
                        w.WriteLine();
                    }
                }
                w.WriteLine();
            }

            // optional argument details
            if (_optArgs.Count > 0)
            {
                w.WriteLine(Properties.Resources.Prog_Usage_OptArgsTitle);
                w.WriteLine();

                foreach (OptArgInfo optArg in _optArgs)
                {
                    // arg name(s)
                    List<string> shortFormList = new List<string>();
                    List<string> longFormList = new List<string>();
                    foreach (string name in optArg.Argument.Names)
                    {
                        if (name.Length == 1)
                        {
                            shortFormList.Add(ShortFormArgIndicator + name);
                        }
                        else
                        {
                            longFormList.Add(LongFormArgIndicator + name);
                        }
                    }
                    w.Write(indentLv1);
                    if (shortFormList.Count > 0)
                    {
                        w.Write(string.Join<string>(", ", shortFormList));
                    }
                    if (longFormList.Count > 0)
                    {
                        if (shortFormList.Count > 0)
                        {
                            w.Write(", ");
                        }
                        w.Write(string.Join<string>(", ", longFormList));
                    }

                    // arg value
                    if (optArg.Value != null)
                    {
                        w.Write(" ");
                        w.Write(optArg.Value.PlaceholderText);
                    }

                    // required arg?
                    if (optArg.Argument.Required)
                    {
                        w.Write("  [Mandatory]");
                    }
                    w.WriteLine();

                    // arg description
                    if (optArg.Description != null)
                    {
                        foreach (string line in optArg.Description.Lines)
                        {
                            w.WriteLine(indentLv2 + line);
                        }
                        w.WriteLine();
                    }
                }
            }
        }
    }

    class ArgInfo
    {
        public ArgInfo(PropertyInfo propertyInfo)
        {
            PropertyInfo = propertyInfo;
        }

        public PropertyInfo PropertyInfo { get; private set; }
        public ArgDescriptionAttribute Description { get; set; }
    }

    class OptArgInfo : ArgInfo
    {
        public OptArgInfo(PropertyInfo propertyInfo, OptionalArgAttribute arg) : base(propertyInfo)
        {
            Argument = arg;
        }

        public OptionalArgAttribute Argument { get; private set; }
        public OptionalArgValueAttribute Value { get; set; }
    }

    class PosArgInfo : ArgInfo
    {
        public PosArgInfo(PropertyInfo propertyInfo, PositionalArgAttribute arg) : base(propertyInfo)
        {
            Argument = arg;
        }

        public PositionalArgAttribute Argument { get; set; }
    }

}

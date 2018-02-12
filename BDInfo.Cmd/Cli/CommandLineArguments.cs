using CommandLineParser.Arguments;

namespace BDInfo.Cli
{
    internal class CommandLineArguments
    {
        public static CommandLineArguments ParseArguments(string[] args)
        {
            var parser = new CommandLineParser.CommandLineParser();
            var result = new CommandLineArguments();

            parser.ExtractArgumentAttributes(result);
            parser.ParseCommandLine(args);

            return result;
        }

        [ValueArgument(typeof(string), "input", ValueOptional = true)]
        public string InputPath;

        [ValueArgument(typeof(string), "output", ValueOptional = true)]
        public string OutputPath;

        [SwitchArgument("quickscan",  false, Optional = true)]
        public bool QuickScan;

        [SwitchArgument("fullscan", false, Optional = true)]
        public bool ScanBitrates;
    }
}

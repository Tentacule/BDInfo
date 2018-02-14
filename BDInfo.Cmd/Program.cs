using System;
using BDInfo.Cli;

namespace BDInfo.Cmd
{
    class Program
    {
        static void Main(string[] args)
        {
            var arguments = CommandLineArguments.ParseArguments(args);

            Console.CancelKeyPress += CancelKeyPressHandler;

            if (arguments.QuickScan || arguments.ScanBitrates)
            {
                CommandLineScanner.CommandLineScan(arguments);
            }
        }

        protected static void CancelKeyPressHandler(object sender, ConsoleCancelEventArgs args)
        {
            CommandLineScanner.Scanner?.CancelAsync();
            Console.WriteLine("\nScan aborted.");
            CommandLineScanner.Done = true;
        }
    }
}

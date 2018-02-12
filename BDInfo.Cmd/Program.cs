using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using BDInfo;
using BDInfo.Cli;

namespace BDInfo.Cmd
{
    class Program
    {
        static void Main(string[] args)
        {
            var arguments = CommandLineArguments.ParseArguments(args);

            if (arguments.QuickScan || arguments.ScanBitrates)
            {
                CommandLineScanner.CommandLineScan(arguments);
            }
        }
    }
}

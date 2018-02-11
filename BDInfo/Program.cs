//============================================================================
// BDInfo - Blu-ray Video and Audio Analysis Tool
// Copyright © 2010 Cinema Squid
//
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 2.1 of the License, or (at your option) any later version.
//
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
//=============================================================================

using System;
using System.Threading;
using System.Windows.Forms;
using BDInfo.Cli;
using BDInfo.Scanner;

namespace BDInfo
{
    static class Program
    {

        public static string BDInfoVersion = Application.ProductVersion;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            var arguments = CommandLineArguments.ParseArguments(args);

            if (arguments.QuickScan || arguments.ScanBitrates)
            {
                CommandLineScan(arguments);
            }
            else
            {
                string[] formArguments = { arguments.InputPath };
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new FormMain(formArguments));
            }
        }

        private static void CommandLineScan(CommandLineArguments arguments)
        {
            var scanner = new BdRomIsoScanner(arguments.InputPath);
            scanner.Scan();

            while (scanner.worker.IsBusy)
            {
                Thread.Sleep(50);
                Application.DoEvents();
            }
        }
    }
}

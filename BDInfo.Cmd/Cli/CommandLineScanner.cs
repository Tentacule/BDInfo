using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using BDInfo.Scanner;
using BDInfo.Utilities;

namespace BDInfo.Cli
{
    public class CommandLineScanner
    {
        private static bool _done;
        private static ProgressBar _progress;
        private static string _outputPath;

        internal static void CommandLineScan(CommandLineArguments arguments)
        {
            _progress = new ProgressBar();
            _outputPath = arguments.OutputPath;

            var scanner = new BdRomIsoScanner(arguments.InputPath);
            scanner.ScanStreamClipFileError += OnStreamClipFileScanError;
            scanner.ScanPlaylistFileError += OnPlaylistFileScanError;
            scanner.ScanStreamFileError += OnStreamFileScanError;
            scanner.ScanBitratesProgress += ScanBitratesProgress;
            scanner.ScanBitratesCompleted += ScanBitratesOnScanCompleted;
            scanner.ScanCompleted += ScannerOnScanCompleted;

            scanner.ScanBitrates(null);

            while (!_done)
            {
                Thread.Sleep(50);
            }
       
            _progress.Dispose();
        }

        private static void OnPlaylistFileScanError(object sender, ScannerErrorEventArgs e)
        {
            Console.WriteLine("BDInfo Scan Error");
            Console.WriteLine($"An error occurred while scanning the playlist file {e.PlaylistFile.Name}.\n\nThe disc may be copy-protected or damaged.\n\nDo you want to continue scanning the playlist files?");

            var key = Console.ReadKey();
            e.ContinueScan = (key.Key == ConsoleKey.Y);
        }

        private static void OnStreamFileScanError(object sender, ScannerErrorEventArgs e)
        {
            Console.WriteLine("BDInfo Scan Error");
            Console.WriteLine($"An error occurred while scanning the stream file {e.StreamFile.Name}.\n\nThe disc may be copy-protected or damaged.\n\nDo you want to continue scanning the stream files?");

            var key = Console.ReadKey();
            e.ContinueScan = (key.Key == ConsoleKey.Y);
        }

        private static void OnStreamClipFileScanError(object sender, ScannerErrorEventArgs e)
        {
            Console.WriteLine("BDInfo Scan Error");
            Console.WriteLine($"An error occurred while scanning the stream clip file {e.StreamClipFile.Name}.\n\nThe disc may be copy-protected or damaged.\n\nDo you want to continue scanning the stream clip files?");

            var key = Console.ReadKey();
            e.ContinueScan = (key.Key == ConsoleKey.Y);
        }

        private static void ScannerOnScanCompleted(object sender, ScannerEventArgs e)
        {
            var bdRomIso = e.BdRomIso;

            if (e.Exception != null)
            {
                Console.WriteLine("BDInfo Error");
                Console.WriteLine($"{e.Exception.Message}");

                return;
            }

            var message = $"Detected BDMV Folder: {bdRomIso.DirectoryBDMV.FullName} ({bdRomIso.VolumeLabel}) {Environment.NewLine}";

            var features = new List<string>();
            if (bdRomIso.Is50Hz)
            {
                features.Add("50Hz Content");
            }
            if (bdRomIso.IsBDPlus)
            {
                features.Add("BD+ Copy Protection");
            }
            if (bdRomIso.IsBDJava)
            {
                features.Add("BD-Java");
            }
            if (bdRomIso.Is3D)
            {
                features.Add("Blu-ray 3D");
            }
            if (bdRomIso.IsDBOX)
            {
                features.Add("D-BOX Motion Code");
            }
            if (bdRomIso.IsPSP)
            {
                features.Add("PSP Digital Copy");
            }
            if (features.Count > 0)
            {
                message += "Detected Features: " + string.Join(", ", features.ToArray()) + Environment.NewLine;
            }

            message += $"Disc Size: {bdRomIso.Size:N0} bytes{Environment.NewLine}";

            Console.WriteLine();
            Console.WriteLine(message);
        }

        private static void ScanBitratesProgress(object sender, ScannerEventArgs e)
        {
            ScanBDROMState scanState = e.ScanState;

            try
            {
                string message = "";

                if (scanState.StreamFile != null)
                {
                    message = string.Format(
                       "Scanning {0}...\r\n",
                       scanState.StreamFile.DisplayName);
                }

                long finishedBytes = scanState.FinishedBytes;
                if (scanState.StreamFile != null)
                {
                    finishedBytes += scanState.StreamFile.Size;
                }

                double progress = ((double)finishedBytes / scanState.TotalBytes);
                int progressValue = (int)Math.Round(progress * 100);
                if (progressValue < 0) progressValue = 0;
                if (progressValue > 100) progressValue = 100;


                double progressPercent = (double)progressValue / 100;

                _progress.Report(progressPercent);

                TimeSpan elapsedTime = DateTime.Now.Subtract(scanState.TimeStarted);
                TimeSpan remainingTime;
                if (progress > 0 && progress < 1)
                {
                    remainingTime = new TimeSpan(
                        (long)((double)elapsedTime.Ticks / progress) - elapsedTime.Ticks);
                }
                else
                {
                    remainingTime = new TimeSpan(0);
                }

                _progress.UpdateText(message + " - " + remainingTime);
            }
            catch { }
        }

        private static void ScanBitratesOnScanCompleted(object sender, ScannerEventArgs e)
        {
            if (_outputPath != string.Empty)
            {
                File.WriteAllText(_outputPath, ReportUtilities.CreateReport(e.BdRomIso, e.ScanResult));
            }

            _progress.Report(1);

            Console.WriteLine("Scan complete.");
            Console.WriteLine();

            if (e.ScanResult.ScanException != null)
            {
                Console.WriteLine("BDInfo Error:");
                Console.WriteLine($"{e.ScanResult.ScanException.Message}");
            }
            else
            {
                if (e.ScanResult.FileExceptions.Count > 0)
                {
                    Console.WriteLine("BDInfo Scan");
                    Console.WriteLine("Scan completed with errors (see report).");
                }
                else
                {
                    Console.WriteLine("BDInfo Scan");
                    Console.WriteLine("Scan completed successfully.");
                }
            }
            _done = true;
        }

    }
}

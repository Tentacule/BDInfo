using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using BDInfo.BDROM;

namespace BDInfo.Scanner
{
    public class BdRomIsoScanner
    {

        private CancellationTokenSource _cancellationSource;

        private readonly string _inputFilename;
        private ScanBDROMResult _scanResult;

        internal BdRomIso _bdRomIso = null;
        public BackgroundWorker worker = null;

        public delegate void ScanEventHandler(object sender, ScannerEventArgs e);
        public delegate void ScanErrorEventHandler(object sender, ScannerErrorEventArgs e);

        public event ScanEventHandler ScanProgress;
        public event ScanEventHandler ScanCompleted;
        public event ScanEventHandler ScanBitratesProgress;
        public event ScanEventHandler ScanBitratesCompleted;
        public event ScanErrorEventHandler ScanPlaylistFileError;
        public event ScanErrorEventHandler ScanStreamFileError;
        public event ScanErrorEventHandler ScanStreamClipFileError;

        public BdRomIsoScanner(string inputFilename)
        {
            _inputFilename = inputFilename;
        }

        public bool IsBusy => worker != null && worker.IsBusy;

        public void Scan()
        {
            worker = new BackgroundWorker
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true
            };

            worker.DoWork += DoWorkQuickScan;
            worker.ProgressChanged += OnScanProgress;
            worker.RunWorkerCompleted += OnScanCompleted;
            worker.RunWorkerAsync(_inputFilename);
        }

        public void ScanBitrates(List<TSStreamFile> streamFiles)
        {
            worker = new BackgroundWorker
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true
            };

            worker.DoWork += DoWorkScanBitrates;
            worker.ProgressChanged += OnScanBitratesProgress;
            worker.RunWorkerCompleted += OnScanBitratesCompleted;
            worker.RunWorkerAsync(streamFiles);
        }

        public void CancelAsync()
        {
            worker?.CancelAsync();
            _cancellationSource.Cancel();
        }

        private BdRomIso CreateBdRomIso()
        {
            BdRomIso bdRomIso = new BdRomIso(_inputFilename);
            bdRomIso.StreamClipFileScanError += new BdRomIso.OnStreamClipFileScanError(OnStreamClipFileScanError);
            bdRomIso.StreamFileScanError += new BdRomIso.OnStreamFileScanError(OnStreamFileScanError);
            bdRomIso.PlaylistFileScanError += new BdRomIso.OnPlaylistFileScanError(OnPlaylistFileScanError);
            bdRomIso.StreamClipFileScanError += new BdRomIso.OnStreamClipFileScanError(OnStreamClipFileScanError);
            bdRomIso.StreamFileScanError += new BdRomIso.OnStreamFileScanError(OnStreamFileScanError);
            bdRomIso.PlaylistFileScanError += new BdRomIso.OnPlaylistFileScanError(OnPlaylistFileScanError);
            bdRomIso.ScanBitratesProgress += OnScanBitratesProgress;

            return bdRomIso;
        }

        private void DoWorkQuickScan(object sender, DoWorkEventArgs e)
        {
            _cancellationSource = new CancellationTokenSource();
            try
            {
                _bdRomIso = CreateBdRomIso();
                _bdRomIso.CancellationToken = _cancellationSource.Token;
                _bdRomIso.Scan();
                e.Result = null;
            }
            catch (Exception ex)
            {
                e.Result = ex;
            }
        }

        private void DoWorkScanBitrates(object sender, DoWorkEventArgs e)
        {
            _scanResult = new ScanBDROMResult();
            _cancellationSource = new CancellationTokenSource();

            try
            {
                _bdRomIso.CancellationToken = _cancellationSource.Token;
                _scanResult = _bdRomIso.ScanBitrates(streamFiles: (List<TSStreamFile>)e.Argument);

                e.Result = null;
            }
            catch (Exception ex)
            {
                e.Result = ex;
            }
        }

        private void OnScanBitratesProgress(object sender, ScanBitratesEventArgs e)
        {
            worker.ReportProgress(0, e);
        }

        protected virtual bool OnPlaylistFileScanError(TSPlaylistFile playlistFile, Exception ex)
        {
            ScannerErrorEventArgs arguments = new ScannerErrorEventArgs
            {
                PlaylistFile = playlistFile,
                Exception = ex
            };

            ScanPlaylistFileError?.Invoke(this, arguments);

            return arguments.ContinueScan;
        }

        protected virtual bool OnStreamFileScanError(TSStreamFile streamFile, Exception ex)
        {
            ScannerErrorEventArgs arguments = new ScannerErrorEventArgs
            {
                StreamFile = streamFile,
                Exception = ex
            };

            ScanStreamFileError?.Invoke(this, arguments);

            return arguments.ContinueScan;
        }

        protected virtual bool OnStreamClipFileScanError(TSStreamClipFile streamClipFile, Exception ex)
        {
            ScannerErrorEventArgs arguments = new ScannerErrorEventArgs
            {
                StreamClipFile = streamClipFile,
                Exception = ex
            };

            ScanStreamClipFileError?.Invoke(this, arguments);

            return arguments.ContinueScan;
        }

        protected virtual void OnScanProgress(object sender, ProgressChangedEventArgs e)
        {
            try
            {
                if (worker.IsBusy &&
                    !worker.CancellationPending)
                {
                    ScanBitratesEventArgs arg = (ScanBitratesEventArgs)e.UserState;
                    ScanProgress?.Invoke(this, new ScannerEventArgs(_bdRomIso, null, null, arg.ScanState));
                }
            }
            catch { }
        }

        protected virtual void OnScanBitratesProgress(object sender, ProgressChangedEventArgs e)
        {
            try
            {
                if (worker.IsBusy &&
                    !worker.CancellationPending)
                {
                    ScanBitratesEventArgs arg = (ScanBitratesEventArgs)e.UserState;
                    ScanBitratesProgress?.Invoke(this, new ScannerEventArgs(_bdRomIso, null, null, arg.ScanState));
                }
            }
            catch { }
        }
        
        protected virtual void OnScanCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            ScanCompleted?.Invoke(this, new ScannerEventArgs(_bdRomIso, (Exception)e.Result, _scanResult, null));
        }

        protected virtual void OnScanBitratesCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            ScanBitratesCompleted?.Invoke(this, new ScannerEventArgs(_bdRomIso, (Exception)e.Result, _scanResult, null));
        }
    }
}

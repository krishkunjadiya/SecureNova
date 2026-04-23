using System;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Windows;

namespace SecureNova.GUI.Services
{
    public class SecurityScanner
    {
        private readonly string _scriptPath;
        private PowerShell? _currentPowerShell;
        private bool _isCancelled;
        private const int SCAN_TIMEOUT_SECONDS = 300; // 5 minutes timeout

        public event EventHandler<FindingEventArgs>? OnFindingDetected;
        public event EventHandler<FileChangeEventArgs>? OnFileChanged;
        public event EventHandler<ProcessChangeEventArgs>? OnProcessChanged;
        public event EventHandler<string>? OnProgressUpdate;

        public SecurityScanner()
        {
            _scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Scripts", "SecureNova.ps1");
        }

        public async Task PerformScan()
        {
            try
            {
                if (!File.Exists(_scriptPath))
                {
                    throw new FileNotFoundException("PowerShell script not found", _scriptPath);
                }

                _isCancelled = false;
                using (var runspace = RunspaceFactory.CreateRunspace())
                {
                    runspace.Open();
                    
                    using (_currentPowerShell = PowerShell.Create())
                    {
                        _currentPowerShell.Runspace = runspace;

                        // Create a new pipeline for running the script
                        _currentPowerShell.AddScript(File.ReadAllText(_scriptPath));
                        _currentPowerShell.AddParameter("SingleScan", true);

                        // Set up streaming output handling
                        var outputCollection = new PSDataCollection<PSObject>();
                        outputCollection.DataAdded += OutputCollection_DataAdded;

                        // Set up information stream for progress
                        _currentPowerShell.Streams.Information.DataAdded += (sender, e) =>
                        {
                            if (sender is PSDataCollection<InformationRecord> infoCollection)
                            {
                                var info = infoCollection[e.Index];
                                OnProgressUpdate?.Invoke(this, info.MessageData.ToString() ?? "");
                            }
                        };

                        // Run the script asynchronously with output streaming
                        var asyncResult = _currentPowerShell.BeginInvoke<PSObject, PSObject>(null, outputCollection);

                        // Wait for completion or timeout
                        var completed = await Task.Run(() =>
                        {
                            return asyncResult.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(SCAN_TIMEOUT_SECONDS));
                        });

                        if (!completed)
                        {
                            _isCancelled = true;
                            _currentPowerShell.Stop();
                            throw new TimeoutException($"Security scan timed out after {SCAN_TIMEOUT_SECONDS} seconds");
                        }

                        if (_currentPowerShell.HadErrors)
                        {
                            var errors = string.Join("\n", _currentPowerShell.Streams.Error.ReadAll());
                            throw new Exception($"PowerShell script errors:\n{errors}");
                        }

                        _currentPowerShell.EndInvoke(asyncResult);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Security scan failed: {ex.Message}", ex);
            }
            finally
            {
                _currentPowerShell = null;
            }
        }

        private void OutputCollection_DataAdded(object? sender, DataAddedEventArgs e)
        {
            if (sender is PSDataCollection<PSObject> collection)
            {
                var result = collection[e.Index];
                try
                {
                    var resultJson = result.BaseObject.ToString();
                    if (string.IsNullOrEmpty(resultJson)) return;

                    var resultObj = JsonConvert.DeserializeObject<dynamic>(resultJson);
                    if (resultObj == null) return;
                    
                    string? type = resultObj?.Type?.ToString();
                    string? data = resultObj?.Data?.ToString();

                    if (string.IsNullOrEmpty(type) || string.IsNullOrEmpty(data))
                        return;

                    switch (type)
                    {
                        case "Finding":
                            var finding = JsonConvert.DeserializeObject<Finding>(data);
                            if (finding != null)
                            {
                                Application.Current.Dispatcher.Invoke(() =>
                                    OnFindingDetected?.Invoke(this, new FindingEventArgs(finding)));
                            }
                            break;

                        case "FileChange":
                            var fileChange = JsonConvert.DeserializeObject<FileChange>(data);
                            if (fileChange != null)
                            {
                                Application.Current.Dispatcher.Invoke(() =>
                                    OnFileChanged?.Invoke(this, new FileChangeEventArgs(fileChange)));
                            }
                            break;

                        case "ProcessChange":
                            var processChange = JsonConvert.DeserializeObject<ProcessChange>(data);
                            if (processChange != null)
                            {
                                Application.Current.Dispatcher.Invoke(() =>
                                    OnProcessChanged?.Invoke(this, new ProcessChangeEventArgs(processChange)));
                            }
                            break;
                    }
                }
                catch
                {
                    // Ignore parsing errors for non-JSON output
                }
            }
        }

        public void StopScan()
        {
            _isCancelled = true;
            _currentPowerShell?.Stop();
        }
    }

    public class FindingEventArgs : EventArgs
    {
        public Finding Finding { get; }
        public FindingEventArgs(Finding finding) => Finding = finding;
    }

    public class FileChangeEventArgs : EventArgs
    {
        public string FilePath { get; }
        public WatcherChangeTypes ChangeType { get; }
        public FileChangeEventArgs(FileChange change)
        {
            FilePath = change.Path;
            ChangeType = change.ChangeType;
        }
    }

    public class ProcessChangeEventArgs : EventArgs
    {
        public string ProcessName { get; }
        public int ProcessId { get; }
        public string RiskLevel { get; }
        public string ProcessPath { get; }
        public string SignatureStatus { get; }

        public ProcessChangeEventArgs(ProcessChange change)
        {
            ProcessName = change.Name;
            ProcessId = change.Id;
            RiskLevel = change.RiskLevel;
            ProcessPath = change.Path;
            SignatureStatus = change.SignatureStatus;
        }
    }

    public class Finding
    {
        public required string Type { get; set; }
        public required string Severity { get; set; }
        public required string Details { get; set; }
    }

    public class FileChange
    {
        public required string Path { get; set; }
        public WatcherChangeTypes ChangeType { get; set; }
    }

    public class ProcessChange
    {
        public required string Name { get; set; }
        public int Id { get; set; }
        public required string RiskLevel { get; set; }
        public required string Path { get; set; }
        public required string SignatureStatus { get; set; }
    }
} 
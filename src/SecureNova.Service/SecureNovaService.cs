using System;
using System.ServiceProcess;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;

namespace SecureNova.Service
{
    public class SecureNovaService : ServiceBase
    {
        private Runspace? _runspace;
        private PowerShell? _powershell;
        private bool _isRunning;
        private readonly string _scriptPath;
        private readonly EventLog _eventLog;

        public SecureNovaService()
        {
            ServiceName = "SecureNovaService";
            _scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Scripts", "SecureNova.ps1");

            _eventLog = new EventLog();
            if (!EventLog.SourceExists("SecureNova"))
            {
                EventLog.CreateEventSource("SecureNova", "Application");
            }
            _eventLog.Source = "SecureNova";
            _eventLog.Log = "Application";
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                _eventLog.WriteEntry("SecureNova Service is starting...", EventLogEntryType.Information);
                InitializePowerShell();
                StartMonitoring();
            }
            catch (Exception ex)
            {
                _eventLog.WriteEntry($"Failed to start SecureNova Service: {ex.Message}", EventLogEntryType.Error);
                throw;
            }
        }

        protected override void OnStop()
        {
            try
            {
                _eventLog.WriteEntry("SecureNova Service is stopping...", EventLogEntryType.Information);
                StopMonitoring();
            }
            catch (Exception ex)
            {
                _eventLog.WriteEntry($"Error while stopping SecureNova Service: {ex.Message}", EventLogEntryType.Error);
            }
        }

        private void InitializePowerShell()
        {
            _runspace = RunspaceFactory.CreateRunspace();
            _runspace.Open();
            
            _powershell = PowerShell.Create();
            _powershell.Runspace = _runspace;
        }

        private void StartMonitoring()
        {
            if (_isRunning) return;
            if (_powershell is null)
            {
                _eventLog.WriteEntry("PowerShell engine is not initialized.", EventLogEntryType.Error);
                return;
            }

            _isRunning = true;

            Task.Run(() =>
            {
                try
                {
                    _powershell.AddScript(File.ReadAllText(_scriptPath));
                    _powershell.AddParameter("ServiceMode", true);
                    
                    var results = _powershell.Invoke();
                    ProcessResults(results);
                }
                catch (Exception ex)
                {
                    _eventLog.WriteEntry($"Error in monitoring: {ex.Message}", EventLogEntryType.Error);
                }
            });
        }

        private void StopMonitoring()
        {
            if (!_isRunning) return;
            _isRunning = false;

            _powershell?.Stop();
            _powershell?.Commands.Clear();
            _powershell?.Dispose();
            _runspace?.Dispose();
            _powershell = null;
            _runspace = null;
        }

        private void ProcessResults(System.Collections.ObjectModel.Collection<PSObject> results)
        {
            foreach (var result in results)
            {
                if (result.Properties["Severity"]?.Value?.ToString() == "high")
                {
                    _eventLog.WriteEntry(
                        result.Properties["Details"]?.Value?.ToString() ?? "High severity threat detected",
                        EventLogEntryType.Warning
                    );
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                StopMonitoring();
                _eventLog.Dispose();
            }
            base.Dispose(disposing);
        }
    }
} 
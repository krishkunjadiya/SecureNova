using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.ComponentModel;
using System.IO;
using Microsoft.Win32;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Linq;
using SecureNova.GUI.Models;
using SecureNova.GUI.Services;
using System.Security.Principal;
using System.Diagnostics;

namespace SecureNova.GUI
{
    public partial class MainWindow : Window
    {
        private readonly ObservableCollection<ActivityItem> _activityItems;
        private readonly ObservableCollection<FileChangeItem> _fileChanges;
        private readonly ObservableCollection<ProcessItem> _processes;
        private readonly SecurityScanner _scanner;
        private bool _isScanning;
        private bool _isDevEnvironment;

        public MainWindow()
        {
            InitializeComponent();

            // Check if we're running in development environment
            _isDevEnvironment = Environment.GetCommandLineArgs().Any(arg => arg.Contains("dotnet.exe"));

            // Initialize collections
            _activityItems = new ObservableCollection<ActivityItem>();
            _fileChanges = new ObservableCollection<FileChangeItem>();
            _processes = new ObservableCollection<ProcessItem>();

            // Bind collections to ListViews
            lvActivityStream.ItemsSource = _activityItems;
            lvFileChanges.ItemsSource = _fileChanges;
            lvProcesses.ItemsSource = _processes;

            // Initialize scanner
            _scanner = new SecurityScanner();
            _scanner.OnFindingDetected += Scanner_OnFindingDetected;
            _scanner.OnFileChanged += Scanner_OnFileChanged;
            _scanner.OnProcessChanged += Scanner_OnProcessChanged;
            _scanner.OnProgressUpdate += Scanner_OnProgressUpdate;

            // Check admin privileges
            if (!IsRunningAsAdmin())
            {
                if (_isDevEnvironment)
                {
                    MessageBox.Show(
                        "SecureNova requires administrator privileges to perform security scans.\n\n" +
                        "Since you're running in development mode, please:\n" +
                        "1. Build the application using 'dotnet build'\n" +
                        "2. Run the executable directly from bin/Debug/net8.0-windows/SecureNova.GUI.exe",
                        "Administrator Privileges Required",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    Application.Current.Shutdown();
                }
                else
                {
                    MessageBox.Show(
                        "SecureNova requires administrator privileges to perform security scans. The application will now restart with elevated privileges.",
                        "Administrator Privileges Required",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    RestartAsAdmin();
                }
            }
        }

        private void Scanner_OnProgressUpdate(object? sender, string message)
        {
            txtStatus.Text = $"Status: {message}";
        }

        private bool IsRunningAsAdmin()
        {
            using (var identity = WindowsIdentity.GetCurrent())
            {
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }

        private void RestartAsAdmin()
        {
            try
            {
                string? exePath;
                if (_isDevEnvironment)
                {
                    // Use the built executable path
                    var buildPath = Path.Combine(
                        AppDomain.CurrentDomain.BaseDirectory,
                        "SecureNova.GUI.exe");
                    exePath = Path.GetFullPath(buildPath);
                }
                else
                {
                    exePath = Process.GetCurrentProcess().MainModule?.FileName;
                }

                if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
                {
                    throw new InvalidOperationException("Could not find application executable");
                }

                var startInfo = new ProcessStartInfo
                {
                    UseShellExecute = true,
                    WorkingDirectory = Path.GetDirectoryName(exePath),
                    FileName = exePath,
                    Verb = "runas"
                };

                Process.Start(startInfo);
                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to restart with administrator privileges. The application may not function correctly.\n\n" +
                    $"Error: {ex.Message}\n\n" +
                    $"To run with administrator privileges:\n" +
                    $"1. Navigate to the application executable in File Explorer\n" +
                    $"2. Right-click the executable and select 'Run as administrator'",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async void OnScanClick(object sender, RoutedEventArgs e)
        {
            if (!IsRunningAsAdmin())
            {
                MessageBox.Show(
                    "SecureNova requires administrator privileges to perform security scans.\n\n" +
                    "Please restart the application as administrator.",
                    "Administrator Privileges Required",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            if (_isScanning)
            {
                _scanner.StopScan();
                btnScan.Content = "Start Scan";
                txtStatus.Text = "Status: Scan stopped";
                _isScanning = false;
                return;
            }

            try
            {
                _isScanning = true;
                btnScan.Content = "Stop Scan";
                txtStatus.Text = "Status: Initializing scan...";

                // Clear previous results
                _activityItems.Clear();
                _fileChanges.Clear();
                _processes.Clear();
                UpdateAlertCounters();

                await _scanner.PerformScan();
                
                if (_isScanning) // Only update if scan wasn't stopped
                {
                    txtStatus.Text = "Status: Scan completed";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Scan failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _isScanning = false;
                btnScan.Content = "Start Scan";
            }
        }

        private void OnExportLogClick(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                Filter = "JSON files (*.json)|*.json|CSV files (*.csv)|*.csv|PDF files (*.pdf)|*.pdf",
                DefaultExt = ".json",
                FileName = $"SecureNova_Export_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}"
            };

            if (dialog.ShowDialog() == true)
            {
                ExportData(dialog.FileName);
            }
        }

        private void OnSettingsClick(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new SettingsWindow();
            settingsWindow.ShowDialog();
        }

        private void Scanner_OnFindingDetected(object? sender, FindingEventArgs e)
        {
            _activityItems.Insert(0, new ActivityItem
            {
                Time = DateTime.Now,
                Type = e.Finding.Type,
                Severity = e.Finding.Severity,
                Details = e.Finding.Details
            });

            UpdateAlertCounters();
        }

        private void Scanner_OnFileChanged(object? sender, FileChangeEventArgs e)
        {
            _fileChanges.Insert(0, new FileChangeItem
            {
                Time = DateTime.Now,
                FileName = Path.GetFileName(e.FilePath),
                Action = e.ChangeType.ToString(),
                Path = e.FilePath
            });
        }

        private void Scanner_OnProcessChanged(object? sender, ProcessChangeEventArgs e)
        {
            _processes.Insert(0, new ProcessItem
            {
                Name = e.ProcessName,
                Id = e.ProcessId,
                RiskLevel = e.RiskLevel,
                Path = e.ProcessPath,
                SignatureStatus = e.SignatureStatus
            });
        }

        private void UpdateAlertCounters()
        {
            int totalAlerts = _activityItems.Count;
            int highSeverity = _activityItems.Count(x => x.Severity == "high");

            txtTotalAlerts.Text = totalAlerts.ToString();
            txtHighSeverity.Text = highSeverity.ToString();
        }

        private void ExportData(string filePath)
        {
            try
            {
                var exporter = new DataExporter();
                exporter.Export(filePath, _activityItems, _fileChanges, _processes);
                MessageBox.Show("Export completed successfully!", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (_isScanning)
            {
                _scanner.StopScan();
            }
            base.OnClosing(e);
        }
    }
} 
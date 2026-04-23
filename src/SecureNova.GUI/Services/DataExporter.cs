using System;
using System.Collections.ObjectModel;
using System.IO;
using Newtonsoft.Json;
using SecureNova.GUI.Models;

namespace SecureNova.GUI.Services
{
    public class DataExporter
    {
        public void Export(string filePath, ObservableCollection<ActivityItem> activities,
            ObservableCollection<FileChangeItem> fileChanges, ObservableCollection<ProcessItem> processes)
        {
            var data = new
            {
                ExportTime = DateTime.Now,
                SystemInfo = new
                {
                    ComputerName = Environment.MachineName,
                    OSVersion = Environment.OSVersion.ToString(),
                    UserName = Environment.UserName
                },
                Activities = activities,
                FileChanges = fileChanges,
                Processes = processes
            };

            string extension = Path.GetExtension(filePath).ToLower();
            switch (extension)
            {
                case ".json":
                    ExportToJson(filePath, data);
                    break;
                case ".csv":
                    ExportToCsv(filePath, activities, fileChanges, processes);
                    break;
                case ".pdf":
                    throw new NotImplementedException("PDF export is not yet implemented.");
                default:
                    throw new ArgumentException("Unsupported file format");
            }
        }

        private void ExportToJson(string filePath, object data)
        {
            var json = JsonConvert.SerializeObject(data, Formatting.Indented);
            File.WriteAllText(filePath, json);
        }

        private void ExportToCsv(string filePath, ObservableCollection<ActivityItem> activities,
            ObservableCollection<FileChangeItem> fileChanges, ObservableCollection<ProcessItem> processes)
        {
            using (var writer = new StreamWriter(filePath))
            {
                // Write Activities
                writer.WriteLine("=== Security Activities ===");
                writer.WriteLine("Time,Type,Severity,Details");
                foreach (var activity in activities)
                {
                    writer.WriteLine($"{activity.Time},{activity.Type},{activity.Severity},{activity.Details}");
                }

                writer.WriteLine();

                // Write File Changes
                writer.WriteLine("=== File Changes ===");
                writer.WriteLine("Time,File,Action,Path");
                foreach (var change in fileChanges)
                {
                    writer.WriteLine($"{change.Time},{change.FileName},{change.Action},{change.Path}");
                }

                writer.WriteLine();

                // Write Processes
                writer.WriteLine("=== Processes ===");
                writer.WriteLine("Name,PID,Risk Level,Location,Signature Status");
                foreach (var process in processes)
                {
                    writer.WriteLine($"{process.Name},{process.Id},{process.RiskLevel},{process.Path},{process.SignatureStatus}");
                }
            }
        }
    }
} 
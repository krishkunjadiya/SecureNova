using System;

namespace SecureNova.GUI.Models
{
    public class FileChangeItem
    {
        public DateTime Time { get; set; }
        public required string FileName { get; set; }
        public required string Action { get; set; }
        public required string Path { get; set; }
    }
} 
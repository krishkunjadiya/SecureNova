using System;

namespace SecureNova.GUI.Models
{
    public class ActivityItem
    {
        public DateTime Time { get; set; }
        public required string Type { get; set; }
        public required string Severity { get; set; }
        public required string Details { get; set; }
    }
} 
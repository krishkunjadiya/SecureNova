namespace SecureNova.GUI.Models
{
    public class ProcessItem
    {
        public required string Name { get; set; }
        public int Id { get; set; }
        public required string RiskLevel { get; set; }
        public required string Path { get; set; }
        public required string SignatureStatus { get; set; }
    }
} 
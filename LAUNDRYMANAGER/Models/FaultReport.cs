namespace LaundryManager.Models
{
    public class FaultReport
    {
        public int Id { get; set; }
        public int MachineId { get; set; }

        // Cleaned up text fields to remove warnings
        public string StudentEmail { get; set; } = "";
        public string Description { get; set; } = "";
        public bool IsFixed { get; set; } = false;
        public string AdminFeedback { get; set; } = "";
        public DateTime DateReported { get; set; } = DateTime.Now;
        public bool IsResolved { get; set; } = false;
    }
}
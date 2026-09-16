namespace LaundryManager.Models
{
    public class Booking
    {
        public int Id { get; set; }
        public int MachineId { get; set; }
        public string StudentEmail { get; set; } = "";
        public DateTime SlotStart { get; set; }
        public DateTime SlotEnd { get; set; }

        // Set when the student confirms they have arrived at the scheduled time.
        public DateTime? ConfirmedAt { get; set; }
        public bool IsNoShow { get; set; } = false;

        public Machine? Machine { get; set; }
    }
}

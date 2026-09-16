using System;

namespace LaundryManager.Models
{
    public class Machine
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Type { get; set; } = "";

        public bool IsBooked { get; set; } = false;
        public string? BookedBy { get; set; }

        public MachineStatus Status { get; set; } = MachineStatus.Working;

        public DateTime? BookedAt { get; set; }

        // NEW: only used for time-slot bookings that just activated
        public bool IsConfirmed { get; set; } = true;
        public DateTime? ConfirmationDeadline { get; set; }

        // Multi-residence support: link machine to a specific residence
        public int ResidenceId { get; set; }
        public Residence? Residence { get; set; }

        public static int GetAverageDurationMinutes(string type)
        {
            return type == "Dryer" ? 40 : 60;
        }

        public DateTime? GetEstimatedFinishTime()
        {
            if (BookedAt == null) return null;
            int duration = GetAverageDurationMinutes(Type);
            return BookedAt.Value.AddMinutes(duration);
        }
    }
}
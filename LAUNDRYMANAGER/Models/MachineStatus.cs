namespace LaundryManager.Models
{
    // Represents the physical condition of a machine.
    // Separate from IsBooked, which tracks whether it's currently in use.
    public enum MachineStatus
    {
        Working,
        UnderRepair,
        OutOfOrder
    }
}

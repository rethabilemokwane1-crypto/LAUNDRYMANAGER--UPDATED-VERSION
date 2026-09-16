using System.Collections.Generic;

namespace LaundryManager.Models
{
    public class Residence
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Address { get; set; } = "";

        // Navigation properties
        public List<User> Users { get; set; } = new();
        public List<Machine> WashingMachines { get; set; } = new();
    }
}

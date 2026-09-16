using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LaundryManager.Data;
using LaundryManager.Models;

namespace LaundryManager.Controllers
{
    public class BookingsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BookingsController(ApplicationDbContext context) => _context = context;

        private User? CurrentStudent()
        {
            var email = HttpContext.Session.GetString("UserEmail");
            var role = HttpContext.Session.GetString("UserRole");
            if (string.IsNullOrWhiteSpace(email) || role != "Student") return null;
            return _context.Users.FirstOrDefault(u => u.Email == email);
        }

        [HttpGet]
        public IActionResult Slots(int machineId)
        {
            var student = CurrentStudent();
            if (student == null) return RedirectToAction("Login", "Account");

            ReleaseExpiredNoShows();

            var machine = _context.Machines.FirstOrDefault(m => m.Id == machineId);
            if (machine == null || machine.ResidenceId != student.ResidenceId) return NotFound();

            int duration = Machine.GetAverageDurationMinutes(machine.Type);
            var now = DateTime.Now;
            var slotStart = now.AddMinutes(5);
            var dayEnd = DateTime.Today.AddHours(22);

            var bookings = _context.Bookings
                .Where(b => b.MachineId == machineId && b.SlotEnd > now && b.SlotStart.Date == DateTime.Today)
                .OrderBy(b => b.SlotStart)
                .ToList();

            var slots = new List<(DateTime Start, DateTime End, bool IsTaken)>();
            while (slotStart.AddMinutes(duration) <= dayEnd)
            {
                var slotEnd = slotStart.AddMinutes(duration);
                var taken = bookings.Any(b => b.SlotStart < slotEnd && slotStart < b.SlotEnd);
                slots.Add((slotStart, slotEnd, taken));
                slotStart = slotEnd;
            }

            ViewBag.Machine = machine;
            ViewBag.Duration = duration;
            ViewBag.Bookings = bookings;
            ViewBag.StudentEmail = student.Email;
            return View(slots);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Reserve(int machineId, DateTime slotStart, DateTime slotEnd)
        {
            var student = CurrentStudent();
            if (student == null) return RedirectToAction("Login", "Account");

            var machine = _context.Machines.FirstOrDefault(m => m.Id == machineId);
            if (machine == null || machine.ResidenceId != student.ResidenceId || machine.Status != MachineStatus.Working)
                return Forbid();

            var now = DateTime.Now;
            if (slotStart <= now || slotEnd <= slotStart)
            {
                TempData["BookingError"] = "That time slot is no longer available.";
                return RedirectToAction("Slots", new { machineId });
            }

            // Prevent a student from holding overlapping bookings.
            if (_context.Bookings.Any(b => b.StudentEmail == student.Email && b.SlotStart < slotEnd && slotStart < b.SlotEnd && b.SlotEnd > now))
            {
                TempData["BookingError"] = "You already have a booking that overlaps this time.";
                return RedirectToAction("Slots", new { machineId });
            }

            if (_context.Bookings.Any(b => b.MachineId == machineId && b.SlotStart < slotEnd && slotStart < b.SlotEnd && b.SlotEnd > now))
            {
                TempData["BookingError"] = "Another student has already reserved that time.";
                return RedirectToAction("Slots", new { machineId });
            }

            _context.Bookings.Add(new Booking
            {
                MachineId = machineId,
                StudentEmail = student.Email,
                SlotStart = slotStart,
                SlotEnd = slotEnd
            });
            _context.SaveChanges();

            TempData["BookingConfirmation"] = $"{machine.Name} reserved from {slotStart:HH:mm} to {slotEnd:HH:mm} ({(int)(slotEnd - slotStart).TotalMinutes} minutes).";
            return RedirectToAction("Slots", new { machineId });
        }

        private void ReleaseExpiredNoShows()
        {
            var now = DateTime.Now;

            var noShowMachines = _context.Machines
                .Where(m => m.IsBooked && !m.IsConfirmed &&
                            m.ConfirmationDeadline != null &&
                            m.ConfirmationDeadline <= now)
                .ToList();

            foreach (var machine in noShowMachines)
            {
                var booking = _context.Bookings.FirstOrDefault(b =>
                    b.MachineId == machine.Id &&
                    b.StudentEmail == machine.BookedBy &&
                    b.SlotStart <= now && b.SlotEnd > now);

                if (booking != null)
                {
                    booking.IsNoShow = true;
                    _context.Bookings.Remove(booking);
                }

                machine.IsBooked = false;
                machine.BookedBy = null;
                machine.BookedAt = null;
                machine.IsConfirmed = true;
                machine.ConfirmationDeadline = null;
            }

            if (noShowMachines.Any()) _context.SaveChanges();
        }
    }
}

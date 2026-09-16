using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LaundryManager.Data;
using LaundryManager.Models;
using LaundryManager.Services;

namespace LaundryManager.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly PushNotificationService _pushService;
        private readonly EmailService _emailService;

        public HomeController(ApplicationDbContext context, PushNotificationService pushService, EmailService emailService)
        {
            _context = context;
            _pushService = pushService;
            _emailService = emailService;
        }

        private User? CurrentStudent()
        {
            var email = HttpContext.Session.GetString("UserEmail");
            var role = HttpContext.Session.GetString("UserRole");
            if (string.IsNullOrWhiteSpace(email) || role != "Student") return null;

            return _context.Users.Include(u => u.Residence)
                .FirstOrDefault(u => u.Email == email);
        }

        private void ProcessBookingWindows()
        {
            var now = DateTime.Now;

            // Activate scheduled bookings and give the owner exactly 10 minutes to confirm arrival.
            var dueBookings = _context.Bookings
                .Where(b => b.SlotStart <= now && b.SlotEnd > now && !b.IsNoShow)
                .OrderBy(b => b.SlotStart)
                .ToList();

            foreach (var booking in dueBookings)
            {
                var machine = _context.Machines.FirstOrDefault(m => m.Id == booking.MachineId);
                if (machine == null || machine.Status != MachineStatus.Working) continue;

                if (!machine.IsBooked)
                {
                    machine.IsBooked = true;
                    machine.BookedBy = booking.StudentEmail;
                    machine.BookedAt = booking.SlotStart;
                    machine.IsConfirmed = booking.ConfirmedAt.HasValue;
                    machine.ConfirmationDeadline = booking.ConfirmedAt.HasValue
                        ? null
                        : booking.SlotStart.AddMinutes(10);
                }
            }

            // A scheduled booking that was not confirmed within 10 minutes becomes a no-show.
            var expired = _context.Machines
                .Where(m => m.IsBooked && !m.IsConfirmed &&
                            m.ConfirmationDeadline != null &&
                            m.ConfirmationDeadline <= now)
                .ToList();

            foreach (var machine in expired)
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

            // Remove finished scheduled bookings.
            var finished = _context.Bookings.Where(b => b.SlotEnd <= now).ToList();
            _context.Bookings.RemoveRange(finished);

            // Also release immediate-use machines even when the student closes their browser.
            var activeScheduledMachineIds = _context.Bookings
                .Where(b => b.SlotStart <= now && b.SlotEnd > now)
                .Select(b => b.MachineId)
                .ToHashSet();

            var completedMachines = _context.Machines
                .Where(m => m.IsBooked && m.BookedAt != null)
                .ToList()
                .Where(m => !activeScheduledMachineIds.Contains(m.Id) &&
                            m.BookedAt!.Value.AddMinutes(Machine.GetAverageDurationMinutes(m.Type)) <= now)
                .ToList();

            foreach (var machine in completedMachines)
            {
                machine.IsBooked = false;
                machine.BookedBy = null;
                machine.BookedAt = null;
                machine.IsConfirmed = true;
                machine.ConfirmationDeadline = null;
            }

            if (dueBookings.Any() || expired.Any() || finished.Any() || completedMachines.Any())
                _context.SaveChanges();
        }

        [HttpGet]
        public IActionResult Landing()
        {
            var email = HttpContext.Session.GetString("UserEmail");
            if (!string.IsNullOrWhiteSpace(email))
            {
                return HttpContext.Session.GetString("UserRole") == "Student"
                    ? RedirectToAction("Index")
                    : RedirectToAction("Index", "Admin");
            }
            return View();
        }

        [HttpGet]
        public IActionResult Index()
        {
            var student = CurrentStudent();
            if (student == null) return RedirectToAction("Login", "Account");

            ProcessBookingWindows();

            var machines = _context.Machines
                .Where(m => m.ResidenceId == student.ResidenceId)
                .OrderBy(m => m.Name)
                .ToList();

            var now = DateTime.Now;
            var upcoming = _context.Bookings
                .Where(b => b.SlotStart > now &&
                            b.SlotStart.Date == DateTime.Today &&
                            machines.Select(m => m.Id).Contains(b.MachineId))
                .OrderBy(b => b.SlotStart)
                .ToList()
                .GroupBy(b => b.MachineId)
                .ToDictionary(g => g.Key, g => g.ToList());

            ViewBag.UpcomingBookings = upcoming;
            ViewBag.StudentEmail = student.Email;
            ViewBag.Residence = student.Residence;
            ViewBag.MyBookings = _context.Bookings
                .Where(b => b.StudentEmail == student.Email && b.SlotEnd > now)
                .OrderBy(b => b.SlotStart)
                .ToList();

            return View(machines);
        }

        [HttpGet]
        public IActionResult Privacy() => View();

        [HttpGet]
        public IActionResult ReportFault(int machineId)
        {
            var student = CurrentStudent();
            if (student == null) return RedirectToAction("Login", "Account");

            var machine = _context.Machines.FirstOrDefault(m => m.Id == machineId && m.ResidenceId == student.ResidenceId);
            if (machine == null) return NotFound();

            ViewBag.StudentEmail = student.Email;
            ViewBag.MachineName = machine.Name;
            ViewBag.MachineId = machineId;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReportFault(int machineId, string description)
        {
            var student = CurrentStudent();
            if (student == null) return RedirectToAction("Login", "Account");

            var machine = _context.Machines.FirstOrDefault(m => m.Id == machineId && m.ResidenceId == student.ResidenceId);
            if (machine == null) return NotFound();

            if (string.IsNullOrWhiteSpace(description))
            {
                ModelState.AddModelError("", "Please provide a brief description of the issue.");
                ViewBag.StudentEmail = student.Email;
                ViewBag.MachineName = machine.Name;
                ViewBag.MachineId = machineId;
                return View();
            }

            _context.FaultReports.Add(new FaultReport
            {
                MachineId = machineId,
                StudentEmail = student.Email,
                Description = description.Trim(),
                DateReported = DateTime.Now,
                IsResolved = false
            });

            machine.Status = MachineStatus.OutOfOrder;
            machine.IsBooked = false;
            machine.BookedBy = null;
            machine.BookedAt = null;
            machine.IsConfirmed = true;
            machine.ConfirmationDeadline = null;

            await _context.SaveChangesAsync();

            await NotifyResidenceStudentsAsync(
                machine.ResidenceId,
                machine.Name,
                "Out of Order",
                "A fault was reported and the machine has been taken out of service.");

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BookMachine(int machineId)
        {
            var student = CurrentStudent();
            if (student == null) return RedirectToAction("Login", "Account");

            ProcessBookingWindows();

            var machine = _context.Machines.FirstOrDefault(m => m.Id == machineId && m.ResidenceId == student.ResidenceId);
            if (machine == null) return NotFound();

            var hasUpcomingReservation = _context.Bookings.Any(b =>
                b.MachineId == machineId && b.SlotStart > DateTime.Now && b.SlotEnd > DateTime.Now);

            if (machine.Status == MachineStatus.Working && !machine.IsBooked && !hasUpcomingReservation)
            {
                machine.IsBooked = true;
                machine.BookedBy = student.Email;
                machine.BookedAt = DateTime.Now;
                machine.IsConfirmed = true;
                machine.ConfirmationDeadline = null;
                await _context.SaveChangesAsync();

                await _pushService.SendNotificationAsync(
                    student.Email,
                    "Machine booked",
                    $"{machine.Name} is booked for you now. Estimated cycle: {Machine.GetAverageDurationMinutes(machine.Type)} minutes.",
                    "/Home/Index");
            }
            else
            {
                TempData["BookingError"] = hasUpcomingReservation
                    ? "This machine has a scheduled reservation and cannot be used before that reservation."
                    : "This machine is currently unavailable.";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReleaseMachine(int machineId)
        {
            var student = CurrentStudent();
            if (student == null) return RedirectToAction("Login", "Account");

            var machine = _context.Machines.FirstOrDefault(m =>
                m.Id == machineId && m.ResidenceId == student.ResidenceId);

            if (machine != null && machine.IsBooked && machine.BookedBy == student.Email)
            {
                machine.IsBooked = false;
                machine.BookedBy = null;
                machine.BookedAt = null;
                machine.IsConfirmed = true;
                machine.ConfirmationDeadline = null;

                var activeBooking = _context.Bookings.FirstOrDefault(b =>
                    b.MachineId == machineId &&
                    b.StudentEmail == student.Email &&
                    b.SlotStart <= DateTime.Now && b.SlotEnd > DateTime.Now);

                if (activeBooking != null)
                    _context.Bookings.Remove(activeBooking);

                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ConfirmMachineUsage(int machineId)
        {
            var student = CurrentStudent();
            if (student == null) return RedirectToAction("Login", "Account");

            var now = DateTime.Now;
            var machine = _context.Machines.FirstOrDefault(m =>
                m.Id == machineId && m.ResidenceId == student.ResidenceId);

            var booking = _context.Bookings.FirstOrDefault(b =>
                b.MachineId == machineId &&
                b.StudentEmail == student.Email &&
                b.SlotStart <= now && b.SlotEnd > now &&
                !b.IsNoShow);

            if (machine != null && booking != null && machine.IsBooked && machine.BookedBy == student.Email &&
                !machine.IsConfirmed && machine.ConfirmationDeadline >= now)
            {
                booking.ConfirmedAt = now;
                machine.IsConfirmed = true;
                machine.ConfirmationDeadline = null;
                _context.SaveChanges();
                TempData["BookingConfirmation"] = $"{machine.Name}: arrival confirmed. Your machine is locked to you until the end of the booked slot.";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AutoReleaseMachine(int machineId)
        {
            var student = CurrentStudent();
            if (student == null) return Unauthorized();

            ProcessBookingWindows();

            var machine = _context.Machines.FirstOrDefault(m =>
                m.Id == machineId && m.ResidenceId == student.ResidenceId);

            if (machine != null && machine.IsBooked && machine.BookedBy == student.Email &&
                machine.BookedAt.HasValue &&
                machine.BookedAt.Value.AddMinutes(Machine.GetAverageDurationMinutes(machine.Type)) <= DateTime.Now)
            {
                machine.IsBooked = false;
                machine.BookedBy = null;
                machine.BookedAt = null;
                machine.IsConfirmed = true;
                machine.ConfirmationDeadline = null;

                var activeBooking = _context.Bookings.FirstOrDefault(b =>
                    b.MachineId == machineId &&
                    b.StudentEmail == student.Email &&
                    b.SlotStart <= DateTime.Now && b.SlotEnd > DateTime.Now);

                if (activeBooking != null)
                    _context.Bookings.Remove(activeBooking);

                _context.SaveChanges();
            }

            return Ok();
        }

        // Backwards-compatible admin URL.
        [HttpGet]
        public IActionResult AdminDashboard() => RedirectToAction("Index", "Admin");

        private async Task NotifyResidenceStudentsAsync(int residenceId, string machineName, string status, string extra)
        {
            var emails = await _context.Users
                .Where(u => u.ResidenceId == residenceId && u.Role == "Student" && u.IsEmailVerified)
                .Select(u => u.Email)
                .ToListAsync();

            if (!emails.Any()) return;

            var subject = $"Laundry update: {machineName} is {status}";
            var body = $"Your residence laundry update:\n\n{machineName} is now {status}.\n{extra}\n\nPlease check the iWS Laundry Portal for the latest availability.";

            try { await _emailService.SendEmailToManyAsync(emails, subject, body); } catch { }
        }
    }
}

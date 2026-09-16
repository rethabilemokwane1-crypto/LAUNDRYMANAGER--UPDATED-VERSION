using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LaundryManager.Models;
using LaundryManager.Data;

namespace LaundryManager.Controllers
{
    public class MachinesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MachinesController(ApplicationDbContext context) => _context = context;

        private User? CurrentAdmin()
        {
            var email = HttpContext.Session.GetString("UserEmail");
            var role = HttpContext.Session.GetString("UserRole");
            if (string.IsNullOrWhiteSpace(email) || (role != "ResAdmin" && role != "Admin")) return null;
            return _context.Users.FirstOrDefault(u => u.Email == email);
        }

        [HttpGet]
        public IActionResult Index()
        {
            var admin = CurrentAdmin();
            if (admin == null) return RedirectToAction("Login", "Account");

            var query = _context.Machines.Include(m => m.Residence).AsNoTracking();
            if (admin.Role != "Admin")
                query = query.Where(m => m.ResidenceId == admin.ResidenceId);

            return View(query.OrderBy(m => m.Name).ToList());
        }

        [HttpGet]
        public IActionResult Create()
        {
            var admin = CurrentAdmin();
            if (admin == null) return RedirectToAction("Login", "Account");
            ViewBag.Residence = admin.Residence;
            return View(new Machine());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Machine machine)
        {
            var admin = CurrentAdmin();
            if (admin == null) return RedirectToAction("Login", "Account");

            if (admin.Role != "Admin")
                machine.ResidenceId = admin.ResidenceId ?? 0;

            ModelState.Remove(nameof(Machine.Residence));
            if (machine.ResidenceId <= 0)
                ModelState.AddModelError("", "Your admin account is not linked to a residence.");

            if (!ModelState.IsValid)
            {
                ViewBag.Residence = admin.Residence;
                return View(machine);
            }

            _context.Machines.Add(machine);
            _context.SaveChanges();
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            var admin = CurrentAdmin();
            if (admin == null) return RedirectToAction("Login", "Account");

            var machine = _context.Machines.FirstOrDefault(m => m.Id == id);
            if (machine != null && (admin.Role == "Admin" || machine.ResidenceId == admin.ResidenceId))
            {
                _context.Machines.Remove(machine);
                _context.SaveChanges();
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, MachineStatus status)
        {
            var admin = CurrentAdmin();
            if (admin == null) return RedirectToAction("Login", "Account");

            var machine = await _context.Machines.Include(m => m.Residence).FirstOrDefaultAsync(m => m.Id == id);
            if (machine == null || !Owns(admin, machine.ResidenceId))
                return Forbid();

            machine.Status = status;

            // A broken machine cannot remain reserved.
            if (status != MachineStatus.Working)
            {
                machine.IsBooked = false;
                machine.BookedBy = null;
                machine.BookedAt = null;
                machine.IsConfirmed = true;
                machine.ConfirmationDeadline = null;

                var bookings = await _context.Bookings.Where(b => b.MachineId == id && b.SlotEnd > DateTime.Now).ToListAsync();
                _context.Bookings.RemoveRange(bookings);
            }

            await _context.SaveChangesAsync();

            await NotifyResidenceStudentsAsync(
                machine.ResidenceId,
                machine.Name,
                status == MachineStatus.Working ? "Working" :
                status == MachineStatus.UnderRepair ? "Under Repair" : "Out of Order");

            return RedirectToAction("Index");
        }

        private bool Owns(User admin, int residenceId) =>
            admin.Role == "Admin" || admin.ResidenceId == residenceId;

        private async Task NotifyResidenceStudentsAsync(int residenceId, string machineName, string status)
        {
            var emails = await _context.Users
                .Where(u => u.ResidenceId == residenceId && u.Role == "Student" && u.IsEmailVerified)
                .Select(u => u.Email)
                .ToListAsync();

            if (!emails.Any()) return;

            var subject = $"{machineName} status update - {status}";
            var body = $"Laundry update for your residence:\n\n{machineName} is now {status}.\n\nPlease check the iWS Laundry Portal for the latest availability.";

            try
            {
                await HttpContext.RequestServices.GetRequiredService<LaundryManager.Services.EmailService>()
                    .SendEmailToManyAsync(emails, subject, body);
            }
            catch
            {
                // Status change is already saved; email delivery should not break the admin workflow.
            }
        }
    }
}

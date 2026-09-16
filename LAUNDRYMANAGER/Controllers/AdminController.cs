using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LaundryManager.Data;
using LaundryManager.Models;
using LaundryManager.Services;

namespace LaundryManager.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly EmailService _emailService;
        private readonly PushNotificationService _pushService;

        public AdminController(ApplicationDbContext context, EmailService emailService, PushNotificationService pushService)
        {
            _context = context;
            _emailService = emailService;
            _pushService = pushService;
        }

        private User? CurrentAdmin()
        {
            var email = HttpContext.Session.GetString("UserEmail");
            var role = HttpContext.Session.GetString("UserRole");
            if (string.IsNullOrWhiteSpace(email) || (role != "ResAdmin" && role != "Admin")) return null;
            return _context.Users.Include(u => u.Residence).FirstOrDefault(u => u.Email == email);
        }

        private bool OwnsResidence(User admin, int residenceId) =>
            admin.Role == "Admin" || admin.ResidenceId == residenceId;

        [HttpGet]
        public IActionResult Index()
        {
            var admin = CurrentAdmin();
            if (admin == null) return RedirectToAction("Login", "Account");

            var residence = admin.Residence;
            var machineQuery = _context.Machines.AsNoTracking();
            if (admin.Role != "Admin")
                machineQuery = machineQuery.Where(m => m.ResidenceId == admin.ResidenceId);

            var machines = machineQuery.ToList();
            var machineIds = machines.Select(m => m.Id).ToList();
            var activeBookings = _context.Bookings
                .Where(b => machineIds.Contains(b.MachineId) && b.SlotEnd > DateTime.Now)
                .OrderBy(b => b.SlotStart)
                .ToList();

            var reports = _context.FaultReports
                .Where(r => machineIds.Contains(r.MachineId))
                .OrderByDescending(r => r.Id)
                .ToList();

            ViewBag.Residence = residence;
            ViewBag.Machines = machines;
            ViewBag.ActiveBookings = activeBookings;
            ViewBag.Reports = reports;
            return View();
        }

        [HttpGet]
        public IActionResult Register()
        {
            if (HttpContext.Session.GetString("UserEmail") != null)
                return RedirectToAction("Index");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(AdminRegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var email = model.Email.Trim().ToLowerInvariant();
            if (_context.Users.Any(u => u.Email.ToLower() == email))
            {
                ModelState.AddModelError("Email", "An account with this Gmail address already exists.");
                return View(model);
            }

            var residence = await _context.Residences
                .FirstOrDefaultAsync(r => r.Name.ToLower() == model.ResidenceName.Trim().ToLower());

            if (residence != null)
            {
                var alreadyManaged = await _context.Users.AnyAsync(u => u.ResidenceId == residence.Id && (u.Role == "ResAdmin" || u.Role == "Admin"));
                if (alreadyManaged)
                {
                    ModelState.AddModelError("ResidenceName", "A residence with this name is already managed by an admin.");
                    return View(model);
                }

                // Allows the requested demo residence (Brandon Mansions) to be claimed.
                if (!residence.Address.StartsWith("Demo address", StringComparison.OrdinalIgnoreCase))
                {
                    ModelState.AddModelError("ResidenceName", "A residence with this name already exists.");
                    return View(model);
                }

                residence.Address = model.ResidenceAddress.Trim();
            }
            else
            {
                residence = new Residence
                {
                    Name = model.ResidenceName.Trim(),
                    Address = model.ResidenceAddress.Trim()
                };
                _context.Residences.Add(residence);
                await _context.SaveChangesAsync();
            }

            var token = Guid.NewGuid().ToString("N");
            var admin = new User
            {
                Email = email,
                Password = BCrypt.Net.BCrypt.HashPassword(model.Password),
                IsEmailVerified = false,
                EmailVerificationToken = token,
                Role = "ResAdmin",
                ResidenceId = residence.Id
            };

            _context.Users.Add(admin);
            await _context.SaveChangesAsync();

            var link = Url.Action("VerifyEmail", "Account",
                new { email = admin.Email, token }, Request.Scheme);

            try
            {
                await _emailService.SendEmailAsync(
                    admin.Email,
                    $"Verify your {residence.Name} admin account",
                    $"Welcome to iWS Laundry Portal.\n\nResidence: {residence.Name}\nAddress: {residence.Address}\n\nVerify your admin account here:\n{link}\n\nAfter verification, log in to manage your residence.");
            }
            catch
            {
                ViewBag.Message = "Your residence and admin account were created, but the verification email could not be sent. Check the email settings before using the account.";
                return View();
            }

            ViewBag.Message = $"Your admin account for {residence.Name} was created. Check {admin.Email} and verify it before logging in.";
            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResolveFault(int machineId, int reportId)
        {
            var admin = CurrentAdmin();
            if (admin == null) return RedirectToAction("Login", "Account");

            var machine = await _context.Machines.FirstOrDefaultAsync(m => m.Id == machineId);
            if (machine == null || !OwnsResidence(admin, machine.ResidenceId)) return Forbid();

            machine.Status = MachineStatus.Working;
            machine.IsBooked = false;
            machine.BookedBy = null;
            machine.BookedAt = null;
            machine.IsConfirmed = true;
            machine.ConfirmationDeadline = null;

            var report = await _context.FaultReports.FirstOrDefaultAsync(r => r.Id == reportId && r.MachineId == machineId);
            if (report != null) _context.FaultReports.Remove(report);

            await _context.SaveChangesAsync();

            await NotifyResidenceStudentsAsync(machine.ResidenceId, machine.Name, "Working");
            return RedirectToAction("Index");
        }

        private async Task NotifyResidenceStudentsAsync(int residenceId, string machineName, string status)
        {
            var emails = await _context.Users
                .Where(u => u.ResidenceId == residenceId && u.Role == "Student" && u.IsEmailVerified)
                .Select(u => u.Email)
                .ToListAsync();

            if (!emails.Any()) return;

            try
            {
                await _emailService.SendEmailToManyAsync(
                    emails,
                    $"{machineName} is now {status}",
                    $"Laundry update for your residence:\n\n{machineName} is now {status}.\n\nPlease check the iWS Laundry Portal.");
            }
            catch { }
        }
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using LaundryManager.Data;
using LaundryManager.Models;
using LaundryManager.Services;

namespace LaundryManager.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly EmailService _emailService;

        public AccountController(
            ApplicationDbContext context,
            EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        // =========================================================
        // LOGIN - GET
        // =========================================================

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // =========================================================
        // LOGIN - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var email = model.Email.Trim().ToLowerInvariant();

            var user = _context.Users
                .Include(u => u.Residence)
                .FirstOrDefault(u => u.Email.ToLower() == email);

            // -----------------------------------------------------
            // INVALID LOGIN
            // -----------------------------------------------------

            if (user == null ||
                string.IsNullOrWhiteSpace(user.Password) ||
                !BCrypt.Net.BCrypt.Verify(
                    model.Password,
                    user.Password))
            {
                ModelState.AddModelError(
                    "",
                    "Invalid email address or incorrect password.");

                return View(model);
            }

            // -----------------------------------------------------
            // EMAIL VERIFICATION
            // -----------------------------------------------------

            if (!user.IsEmailVerified)
            {
                ModelState.AddModelError(
                    "",
                    "Please verify your email before logging in. Check your inbox for the verification link.");

                return View(model);
            }

            // -----------------------------------------------------
            // NORMALIZE ROLE
            // -----------------------------------------------------

            var role = (user.Role ?? "Student").Trim();

            // -----------------------------------------------------
            // STUDENT ACCOUNT
            // -----------------------------------------------------

            if (role.Equals(
                    "Student",
                    StringComparison.OrdinalIgnoreCase))
            {
                // Student must belong to a residence
                if (!user.ResidenceId.HasValue)
                {
                    ModelState.AddModelError(
                        "",
                        "Your account is not linked to a residence. Please contact your residence administrator.");

                    return View(model);
                }

                // Residence must actually exist
                if (user.Residence == null)
                {
                    ModelState.AddModelError(
                        "",
                        "The residence linked to your account could not be found. Please contact your residence administrator.");

                    return View(model);
                }
            }

            // -----------------------------------------------------
            // CLEAR OLD SESSION
            // -----------------------------------------------------

            HttpContext.Session.Clear();

            // -----------------------------------------------------
            // SAVE LOGIN SESSION
            // -----------------------------------------------------

            HttpContext.Session.SetString(
                "UserEmail",
                user.Email);

            HttpContext.Session.SetString(
                "UserRole",
                role);

            // Save residence for users who have one
            if (user.ResidenceId.HasValue)
            {
                HttpContext.Session.SetInt32(
                    "ResidenceId",
                    user.ResidenceId.Value);
            }

            // -----------------------------------------------------
            // ADMIN / RESIDENCE ADMIN
            // -----------------------------------------------------

            if (role.Equals(
                    "Admin",
                    StringComparison.OrdinalIgnoreCase) ||
                role.Equals(
                    "ResAdmin",
                    StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction(
                    "Index",
                    "Admin");
            }

            // -----------------------------------------------------
            // STUDENT DASHBOARD
            // -----------------------------------------------------

            if (role.Equals(
                    "Student",
                    StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction(
                    "Index",
                    "Home");
            }

            // -----------------------------------------------------
            // UNKNOWN ROLE
            // -----------------------------------------------------

            HttpContext.Session.Clear();

            ModelState.AddModelError(
                "",
                "Your account has an invalid user role. Please contact the administrator.");

            return View(model);
        }

        // =========================================================
        // REGISTER - GET (STANDARD)
        // =========================================================

        [HttpGet]
        public IActionResult Register()
        {
            LoadResidences();

            return View();
        }

        // =========================================================
        // REGISTER - POST (STANDARD)
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(
            RegisterViewModel model)
        {
            LoadResidences();

            if (!ModelState.IsValid)
                return View(model);

            var email = model.Email
                .Trim()
                .ToLowerInvariant();

            // -----------------------------------------------------
            // CHECK EXISTING ACCOUNT
            // -----------------------------------------------------

            if (_context.Users.Any(
                u => u.Email.ToLower() == email))
            {
                ModelState.AddModelError(
                    "Email",
                    "An account with this email already exists.");

                return View(model);
            }

            // -----------------------------------------------------
            // CHECK RESIDENCE
            // -----------------------------------------------------

            if (!model.ResidenceId.HasValue ||
                !_context.Residences.Any(
                    r => r.Id == model.ResidenceId.Value))
            {
                ModelState.AddModelError(
                    "ResidenceId",
                    "Please select a valid residence.");

                return View(model);
            }

            // -----------------------------------------------------
            // CREATE EMAIL VERIFICATION TOKEN
            // -----------------------------------------------------

            var token = Guid.NewGuid().ToString("N");

            // -----------------------------------------------------
            // CREATE USER
            // -----------------------------------------------------

            var user = new User
            {
                Email = email,

                Password = BCrypt.Net.BCrypt.HashPassword(
                    model.Password),

                IsEmailVerified = false,

                EmailVerificationToken = token,

                PasswordResetToken = null,

                PasswordResetTokenExpiry = null,

                Role = "Student",

                ResidenceId = model.ResidenceId.Value
            };

            _context.Users.Add(user);

            await _context.SaveChangesAsync();

            // -----------------------------------------------------
            // CREATE VERIFICATION LINK
            // -----------------------------------------------------

            var link = Url.Action(
                "VerifyEmail",
                "Account",
                new
                {
                    email = user.Email,
                    token = token
                },
                Request.Scheme);

            // -----------------------------------------------------
            // SEND VERIFICATION EMAIL
            // -----------------------------------------------------

            try
            {
                await _emailService.SendEmailAsync(
                    user.Email,
                    "Verify your iWS Laundry Portal account",
                    $"Hi,\n\n" +
                    $"Welcome to the iWS Laundry Portal.\n\n" +
                    $"Please verify your account by opening the link below:\n\n" +
                    $"{link}\n\n" +
                    $"Once your email has been verified, you can log in and access your residence laundry dashboard.\n\n" +
                    $"- iWS Laundry Portal");
            }
            catch
            {
                ViewBag.Message =
                    "Account created, but the verification email could not be sent. Please contact the residence manager.";

                return View(
                    "RegisterConfirmation");
            }

            ViewBag.Message =
                "Account created! Check your email and verify your account before logging in.";

            return View(
                "RegisterConfirmation");
        }

        // =========================================================
        // REGISTER STUDENT - GET
        // =========================================================

        [HttpGet]
        public IActionResult RegisterStudent()
        {
            LoadResidences();

            return View();
        }

        // =========================================================
        // REGISTER STUDENT - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterStudent(
            RegisterViewModel model)
        {
            LoadResidences();

            if (!ModelState.IsValid)
                return View(model);

            var email = model.Email.Trim().ToLowerInvariant();

            if (_context.Users.Any(u => u.Email.ToLower() == email))
            {
                ModelState.AddModelError("Email", "An account with this email already exists.");
                return View(model);
            }

            if (!model.ResidenceId.HasValue || !_context.Residences.Any(r => r.Id == model.ResidenceId.Value))
            {
                ModelState.AddModelError("ResidenceId", "Please select a valid residence.");
                return View(model);
            }

            var token = Guid.NewGuid().ToString("N");

            var user = new User
            {
                Email = email,
                Password = BCrypt.Net.BCrypt.HashPassword(model.Password),
                IsEmailVerified = false,
                EmailVerificationToken = token,
                Role = "Student",
                ResidenceId = model.ResidenceId.Value
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var link = Url.Action(
                "VerifyEmail",
                "Account",
                new { email = user.Email, token = token },
                Request.Scheme);

            try
            {
                await _emailService.SendEmailAsync(
                    user.Email,
                    "Verify your iWS Laundry Portal account",
                    $"Hi,\n\n" +
                    $"Welcome to the iWS Laundry Portal.\n\n" +
                    $"Please verify your student account by opening the link below:\n\n" +
                    $"{link}\n\n" +
                    $"- iWS Laundry Portal");
            }
            catch
            {
                ViewBag.Message = "Student account created, but the verification email could not be sent. Please contact support.";
                return View("RegisterConfirmation");
            }

            ViewBag.Message = "Student account created! Please check your email and verify your account before logging in.";
            return View("RegisterConfirmation");
        }

        // =========================================================
        // REGISTER MANAGER - GET
        // =========================================================

        [HttpGet]
        public IActionResult RegisterManager()
        {
            LoadResidences();

            return View();
        }

        // =========================================================
        // REGISTER MANAGER - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterManager(
            RegisterViewModel model)
        {
            LoadResidences();

            if (!ModelState.IsValid)
                return View(model);

            var email = model.Email.Trim().ToLowerInvariant();

            if (_context.Users.Any(u => u.Email.ToLower() == email))
            {
                ModelState.AddModelError("Email", "An account with this email already exists.");
                return View(model);
            }

            var token = Guid.NewGuid().ToString("N");

            var user = new User
            {
                Email = email,
                Password = BCrypt.Net.BCrypt.HashPassword(model.Password),
                IsEmailVerified = false,
                EmailVerificationToken = token,
                Role = "ResAdmin", // Residence Manager role mapping
                ResidenceId = model.ResidenceId
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var link = Url.Action(
                "VerifyEmail",
                "Account",
                new { email = user.Email, token = token },
                Request.Scheme);

            try
            {
                await _emailService.SendEmailAsync(
                    user.Email,
                    "Verify your iWS Laundry Manager account",
                    $"Hi,\n\n" +
                    $"Welcome to the iWS Laundry Portal.\n\n" +
                    $"Please verify your Residence Manager account by opening the link below:\n\n" +
                    $"{link}\n\n" +
                    $"- iWS Laundry Portal");
            }
            catch
            {
                ViewBag.Message = "Manager account created, but the verification email could not be sent.";
                return View("RegisterConfirmation");
            }

            ViewBag.Message = "Residence Manager account created! Please check your email to verify your account.";
            return View("RegisterConfirmation");
        }

        // =========================================================
        // VERIFY EMAIL
        // =========================================================

        [HttpGet]
        public IActionResult VerifyEmail(
            string email,
            string token)
        {
            var user = _context.Users
                .FirstOrDefault(
                    u => u.Email == email);

            if (user == null ||
                string.IsNullOrWhiteSpace(token) ||
                user.EmailVerificationToken != token)
            {
                ViewBag.Message =
                    "This verification link is invalid or has already been used.";

                return View(
                    "VerifyEmailResult");
            }

            user.IsEmailVerified = true;

            user.EmailVerificationToken = null;

            _context.SaveChanges();

            ViewBag.Message =
                "Your email has been verified! You can now log in.";

            return View(
                "VerifyEmailResult");
        }

        // =========================================================
        // LOGOUT
        // =========================================================

        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();

            return RedirectToAction(
                "Login",
                "Account");
        }

        // =========================================================
        // FORGOT PASSWORD - GET
        // =========================================================

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // =========================================================
        // FORGOT PASSWORD - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(
            ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var email = model.Email
                .Trim()
                .ToLowerInvariant();

            var user = _context.Users
                .FirstOrDefault(
                    u => u.Email.ToLower() == email);

            // Do not reveal whether account exists
            if (user != null)
            {
                var token =
                    Guid.NewGuid().ToString("N");

                user.PasswordResetToken = token;

                user.PasswordResetTokenExpiry =
                    DateTime.Now.AddHours(1);

                await _context.SaveChangesAsync();

                var link = Url.Action(
                    "ResetPassword",
                    "Account",
                    new
                    {
                        email = user.Email,
                        token = token
                    },
                    Request.Scheme);

                try
                {
                    await _emailService.SendEmailAsync(
                        user.Email,
                        "Reset your iWS Laundry Portal password",
                        $"Hi,\n\n" +
                        $"Use the link below to reset your password.\n\n" +
                        $"This link expires in 1 hour.\n\n" +
                        $"{link}\n\n" +
                        $"- iWS Laundry Portal");
                }
                catch
                {
                    // Do not expose SMTP configuration
                    // or credentials to the user.
                }
            }

            ViewBag.Message =
                "If an account exists for that email, a password reset link has been sent.";

            return View(
                "ForgotPasswordConfirmation");
        }

        // =========================================================
        // RESET PASSWORD - GET
        // =========================================================

        [HttpGet]
        public IActionResult ResetPassword(
            string email,
            string token)
        {
            var user = _context.Users
                .FirstOrDefault(
                    u => u.Email == email);

            if (user == null ||
                user.PasswordResetToken != token ||
                user.PasswordResetTokenExpiry == null ||
                user.PasswordResetTokenExpiry < DateTime.Now)
            {
                ViewBag.Message =
                    "This password reset link is invalid or has expired.";

                return View(
                    "ResetPasswordResult");
            }

            return View(
                new ResetPasswordViewModel
                {
                    Email = email,
                    Token = token
                });
        }

        // =========================================================
        // RESET PASSWORD - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ResetPassword(
            ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = _context.Users
                .FirstOrDefault(
                    u => u.Email == model.Email);

            if (user == null ||
                user.PasswordResetToken != model.Token ||
                user.PasswordResetTokenExpiry == null ||
                user.PasswordResetTokenExpiry < DateTime.Now)
            {
                ViewBag.Message =
                    "This password reset link is invalid or has expired.";

                return View(
                    "ResetPasswordResult");
            }

            // UPDATE PASSWORD
            user.Password =
                BCrypt.Net.BCrypt.HashPassword(
                    model.NewPassword);

            // INVALIDATE RESET TOKEN
            user.PasswordResetToken = null;

            user.PasswordResetTokenExpiry = null;

            _context.SaveChanges();

            ViewBag.Message =
                "Your password has been reset. You can now log in.";

            return View(
                "ResetPasswordResult");
        }

        // =========================================================
        // LOAD RESIDENCES
        // =========================================================

        private void LoadResidences()
        {
            ViewBag.Residences =
                new SelectList(
                    _context.Residences
                        .AsNoTracking()
                        .OrderBy(r => r.Name)
                        .ToList(),
                    "Id",
                    "Name");
        }
    }
}
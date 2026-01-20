using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using TechBazar.Services;
using Microsoft.AspNetCore.Authorization;
using TechBazar.Models;

namespace TechBazar.Controllers
{
    public class AccountController : Controller
    {
        private readonly EmailService _emailService;
        private readonly ILogger<AccountController> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public AccountController(
            EmailService emailService,
            ILogger<AccountController> logger,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _emailService = emailService;
            _logger = logger;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password, bool rememberMe)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ModelState.AddModelError("", "Email and Password are required.");
                return View();
            }

            _logger.LogInformation($"Login attempt for: {email}");

            try
            {
                // Try regular Identity login
                var result = await _signInManager.PasswordSignInAsync(email, password, rememberMe, lockoutOnFailure: false);

                if (result.Succeeded)
                {
                    _logger.LogInformation($"✅ Login successful for: {email}");

                    // Get user and their roles
                    var user = await _userManager.FindByEmailAsync(email);
                    if (user != null)
                    {
                        var roles = await _userManager.GetRolesAsync(user);
                        _logger.LogInformation($"User {email} has roles: {string.Join(", ", roles)}");

                        if (roles.Contains("Admin"))
                        {
                            TempData["SuccessMessage"] = "Admin login successful!";
                        }
                        else if (roles.Contains("Manager"))
                        {
                            TempData["SuccessMessage"] = "Manager login successful!";
                        }
                        else
                        {
                            TempData["SuccessMessage"] = "Login successful!";
                        }
                    }

                    return RedirectToAction("Index", "Home");
                }

                // If login failed
                _logger.LogWarning($"❌ Login failed for: {email} - Invalid credentials");
                ModelState.AddModelError("", "Invalid login attempt. Please check your email and password.");
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Exception during login for: {email}");
                ModelState.AddModelError("", "An error occurred during login. Please try again.");
                return View();
            }
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost] // FIX: Added missing HttpPost attribute
        [ValidateAntiForgeryToken] // FIX: Added anti-forgery token validation
        public async Task<IActionResult> Register(string email, string password, string confirmPassword, string fullName)
        {
            try
            {
                _logger.LogInformation($"Registration attempt for: {email}");

                // Basic validation
                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(confirmPassword) || string.IsNullOrEmpty(fullName))
                {
                    ModelState.AddModelError("", "All fields are required.");
                    return View();
                }

                if (password != confirmPassword)
                {
                    ModelState.AddModelError("", "Passwords do not match.");
                    return View();
                }

                // Check if user already exists
                var existingUser = await _userManager.FindByEmailAsync(email);
                if (existingUser != null)
                {
                    ModelState.AddModelError("", "An account with this email already exists.");
                    return View();
                }

                // Validate password requirements
                if (password.Length < 6)
                {
                    ModelState.AddModelError("", "Password must be at least 6 characters long.");
                    return View();
                }

                if (!password.Any(char.IsDigit))
                {
                    ModelState.AddModelError("", "Password must contain at least one digit.");
                    return View();
                }

                if (!password.Any(char.IsLower))
                {
                    ModelState.AddModelError("", "Password must contain at least one lowercase letter.");
                    return View();
                }

                if (!password.Any(char.IsUpper))
                {
                    ModelState.AddModelError("", "Password must contain at least one uppercase letter.");
                    return View();
                }

                // Generate OTP
                var rng = new Random();
                var otp = rng.Next(100000, 999999).ToString();

                // Store OTP and registration data in session
                HttpContext.Session.SetString("OTP", otp);
                HttpContext.Session.SetString("TempEmail", email);
                HttpContext.Session.SetString("TempPassword", password);
                HttpContext.Session.SetString("TempFullName", fullName);

                _logger.LogInformation($"Generated OTP for {email}: {otp}");

                // Send OTP email
                var otpSent = await _emailService.SendOtpEmailAsync(email, otp);

                if (otpSent)
                {
                    _logger.LogInformation($"OTP email sent successfully to: {email}");
                    TempData["SuccessMessage"] = "OTP sent to your email. Please check your inbox.";
                    return RedirectToAction("VerifyOTP");
                }
                else
                {
                    _logger.LogError($"Failed to send OTP email to: {email}");
                    ModelState.AddModelError("", "Failed to send OTP email. Please try again.");
                    return View();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception during registration for: {email}");
                ModelState.AddModelError("", "An error occurred during registration. Please try again.");
                return View();
            }
        }

        [HttpGet]
        public IActionResult VerifyOTP()
        {
            // Check if OTP session exists
            var sessionOtp = HttpContext.Session.GetString("OTP");
            if (string.IsNullOrEmpty(sessionOtp))
            {
                TempData["ErrorMessage"] = "OTP session expired. Please register again.";
                return RedirectToAction("Register");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyOTP(string otpInput)
        {
            try
            {
                var sessionOtp = HttpContext.Session.GetString("OTP");
                if (string.IsNullOrEmpty(sessionOtp))
                {
                    ModelState.AddModelError("", "OTP expired or session ended. Please register again.");
                    return RedirectToAction("Register");
                }

                _logger.LogInformation($"OTP verification: Input={otpInput}, Session={sessionOtp}");

                if (otpInput == sessionOtp)
                {
                    var email = HttpContext.Session.GetString("TempEmail");
                    var password = HttpContext.Session.GetString("TempPassword");
                    var fullName = HttpContext.Session.GetString("TempFullName");

                    // Clear OTP and temp data from session
                    HttpContext.Session.Remove("OTP");
                    HttpContext.Session.Remove("TempEmail");
                    HttpContext.Session.Remove("TempPassword");
                    HttpContext.Session.Remove("TempFullName");

                    if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
                    {
                        ModelState.AddModelError("", "Registration data missing. Please register again.");
                        return RedirectToAction("Register");
                    }

                    var user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
                    var result = await _userManager.CreateAsync(user, password);

                    if (result.Succeeded)
                    {
                        // Assign User role by default
                        await _userManager.AddToRoleAsync(user, "User");

                        _logger.LogInformation($"User registered successfully: {email}");
                        TempData["SuccessMessage"] = $"Registration successful! Welcome {fullName}. You can now login.";
                        return RedirectToAction("Login");
                    }
                    else
                    {
                        _logger.LogError($"User creation failed for {email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                        foreach (var error in result.Errors)
                        {
                            ModelState.AddModelError("", error.Description);
                        }
                        return View();
                    }
                }
                else
                {
                    ModelState.AddModelError("", "Invalid OTP. Please try again.");
                    return View();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during OTP verification");
                ModelState.AddModelError("", "An error occurred during OTP verification. Please try again.");
                return View();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            TempData["SuccessMessage"] = "You have been logged out successfully.";
            return RedirectToAction("Index", "Home");
        }

        [Authorize]
        public IActionResult Profile()
        {
            ViewBag.UserEmail = User.Identity?.Name ?? "Unknown";
            return View();
        }

        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
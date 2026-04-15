using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UserManagement.Data;
using UserManagement.Models;

namespace UserManagement.Controllers
{
    [Authorize]
    public class UserController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UserController> _logger;

        public UserController(ApplicationDbContext context, ILogger<UserController> logger)
        {
            _context = context;
            _logger = logger;
        }

        private async Task<bool> CheckUserAndBlockedStatus()
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return false;

            var userId = int.Parse(userIdClaim.Value);
            var user = await _context.Users.FindAsync(userId);

            if (user == null || user.Status == "blocked")
            {
                await HttpContext.SignOutAsync();
                return false;
            }

            return true;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            if (!await CheckUserAndBlockedStatus())
            {
                TempData["ErrorMessage"] = "Your account has been blocked or deleted. Please contact support.";
                return RedirectToAction("Login", "Account");
            }

            var users = await _context.Users
                .OrderByDescending(u => u.LastLoginTime) 
                .Select(u => new User
                {
                    Id = u.Id,
                    Name = u.Name,
                    Email = u.Email,
                    Status = u.Status,
                    LastLoginTime = u.LastLoginTime,
                    RegistrationTime = u.RegistrationTime
                })
                .ToListAsync();

            return View(users);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BlockUsers([FromBody] List<int> userIds)
        {
            if (!await CheckUserAndBlockedStatus())
            {
                return Unauthorized(new { success = false, message = "Session expired. Please login again." });
            }

            if (userIds == null || !userIds.Any())
            {
                return BadRequest(new { success = false, message = "No users selected." });
            }

            var users = await _context.Users.Where(u => userIds.Contains(u.Id)).ToListAsync();

            foreach (var user in users)
            {
                if (user.Status != "blocked")
                {
                    user.Status = "blocked";
                }
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation($"Blocked users: {string.Join(", ", userIds)}");

            return Ok(new { success = true, message = $"{users.Count} user(s) blocked successfully." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnblockUsers([FromBody] List<int> userIds)
        {
            if (!await CheckUserAndBlockedStatus())
            {
                return Unauthorized(new { success = false, message = "Session expired. Please login again." });
            }

            if (userIds == null || !userIds.Any())
            {
                return BadRequest(new { success = false, message = "No users selected." });
            }

            var users = await _context.Users.Where(u => userIds.Contains(u.Id)).ToListAsync();
            var updatedUsers = new List<object>();
            foreach (var user in users)
            {
                if (user.Status == "blocked")
                {
                    user.Status = string.IsNullOrEmpty(user.EmailVerificationToken) ? "active" : "unverified";
                    updatedUsers.Add(new { id = user.Id, status = user.Status });
                }
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation($"Unblocked users: {string.Join(", ", userIds)}");

            return Ok(new { success = true, message = $"{users.Count} user(s) unblocked successfully." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUsers([FromBody] List<int> userIds)
        {
            if (!await CheckUserAndBlockedStatus())
            {
                return Unauthorized(new { success = false, message = "Session expired. Please login again." });
            }

            if (userIds == null || !userIds.Any())
            {
                return BadRequest(new { success = false, message = "No users selected." });
            }

            var users = await _context.Users.Where(u => userIds.Contains(u.Id)).ToListAsync();
            _context.Users.RemoveRange(users);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Deleted users: {string.Join(", ", userIds)}");

            return Ok(new { success = true, message = $"{users.Count} user(s) deleted successfully." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUnverifiedUsers()
        {
            if (!await CheckUserAndBlockedStatus())
            {
                return Unauthorized(new { success = false, message = "Session expired. Please login again." });
            }

            var unverifiedUsers = await _context.Users
                .Where(u => u.Status == "unverified")
                .ToListAsync();

            var count = unverifiedUsers.Count;
            _context.Users.RemoveRange(unverifiedUsers);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Deleted {count} unverified users");

            return Ok(new { success = true, message = $"{count} unverified user(s) deleted successfully." });
        }
    }
}
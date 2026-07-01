using ChatApp.Web.Data;
using ChatApp.Web.Models;
using ChatApp.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Web.Controllers;

[Authorize]
public class ChatController : Controller
{
    private readonly IRoomService _roomService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AppDbContext _db;

    public ChatController(IRoomService roomService, UserManager<ApplicationUser> userManager, AppDbContext db)
    {
        _roomService = roomService;
        _userManager = userManager;
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var currentUserId = _userManager.GetUserId(User)!;
        var currentUserGuid = Guid.Parse(currentUserId);

        var rooms = await _roomService.GetRoomsForUserAsync(currentUserGuid);

        var otherUsers = await _db.Users
            .Where(u => u.Id != currentUserGuid)
            .Select(u => new UserListItem { Id = u.Id, Email = u.Email! })
            .ToListAsync();

        var viewModel = new ChatViewModel
        {
            CurrentUserId = currentUserGuid,
            CurrentUserName = User.Identity!.Name!,
            Rooms = rooms,
            OtherUsers = otherUsers
        };

        return View(viewModel);
    }
}
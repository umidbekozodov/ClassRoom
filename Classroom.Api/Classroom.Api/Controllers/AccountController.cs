using Classroom.Api.Entities;
using Classroom.Api.Models;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Classroom.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AccountController : ControllerBase
{
    // Implementation of IUserStore, databazaga saqlaydi, oqib beradi
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly ILogger _logger;

    public AccountController(
        UserManager<User> userManager,
        SignInManager<User> signInManager, ILogger<AccountController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
    }

    [HttpPost("signup")]
    public async Task<IActionResult> SignUp(SingUpUserDto createUserDto)
    {
        if (!ModelState.IsValid)
            return BadRequest();

        if (createUserDto.Password != createUserDto.ConfirmPassword)
            return BadRequest();

        //username ni tekshirishimiz kerak
        if (await _userManager.Users.AnyAsync(u => u.UserName == createUserDto.UserName))
            return BadRequest();

        // dtodan entityga convert qilish
        var user = createUserDto.Adapt<User>();

        // userni databazaga saqlab beradi
        await _userManager.CreateAsync(user, createUserDto.Password);

        _logger.LogInformation("User saved to database with id {0}", user.Id);

        // token yaratib cookiega yozib junatadi
        await _signInManager.SignInAsync(user, isPersistent: true);

        _logger.LogInformation("User registered");

        return Ok();
    }

    [HttpPost("signin")]
    public async Task<IActionResult> SignIn(SignInUserDto signInUserDto)
    {
        if (!ModelState.IsValid)
            return BadRequest();

        if (!await _userManager.Users.AnyAsync(user => user.UserName == signInUserDto.UserName))
            return NotFound();

        var result = await _signInManager.PasswordSignInAsync(signInUserDto.UserName, signInUserDto.Password, isPersistent: true, false);

        if (!result.Succeeded)
            return BadRequest();

        return Ok();
    }

    [HttpGet("{username}")]
    [Authorize]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Profile(string username)
    {
        var user = await _userManager.GetUserAsync(User);
        
        if (user.UserName != username)
            return NotFound();

        var userDto = user.Adapt<UserDto>();

        _logger.LogInformation("User profile with id {0}", user.Id);

        return Ok(userDto);
    }
}
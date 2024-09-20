using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ssd_authorization_solution.DTOs;
using ssd_authorization_solution.JwtTokenService;

namespace MyApp.Namespace

{
    [Route("api/[controller]")]
[ApiController]
[AllowAnonymous]
public class AuthController(
        UserManager<IdentityUser> userManager,
        SignInManager<IdentityUser> signInManager,
        JwtTokenService jwtTokenService)
        : ControllerBase
{
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto loginDto)
    {
        var user = await userManager.FindByNameAsync(loginDto.Username);
        if (user == null)
        {
            return Unauthorized(new { message = "Invalid username or password." });
        }

        var result = await signInManager.CheckPasswordSignInAsync(user, loginDto.Password, false);
        if (!result.Succeeded)
        {
            return Unauthorized(new { message = "Invalid username or password." });
        }

        var roles = await userManager.GetRolesAsync(user);
        var role = roles.Count > 0 ? roles[0] : "User";

        var token = jwtTokenService.GenerateTokenAsync(user.UserName, role);

        return Ok(new { token });
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto registerDto)
    {
        // Create a new IdentityUser object
        var newUser = new IdentityUser
        {
            UserName = registerDto.UserName,
            Email = registerDto.Email
        };

        // Create the user using UserManager
        var result = await userManager.CreateAsync(newUser, registerDto.Password);
        if (!result.Succeeded)
        {
            // Return error if user creation failed
            return BadRequest(result.Errors);
        }

        // Assign the default role to the user
        await userManager.AddToRoleAsync(newUser, "RegisteredUser");

        var roles = await userManager.GetRolesAsync(newUser);
        var role = roles.Count > 0 ? roles[0] : "RegisteredUser";

        // Generate the JWT token
        var token = jwtTokenService.GenerateTokenAsync(newUser.UserName, role);

        return Ok(new { token });
    }
}
}

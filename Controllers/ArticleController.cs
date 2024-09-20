using System.Runtime.Intrinsics.X86;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ssd_authorization_solution.DTOs;
using ssd_authorization_solution.Entities;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace MyApp.Namespace;

[Route("api/[controller]")]
[ApiController]
public class ArticleController(AppDbContext ctx) : ControllerBase
{
    private readonly AppDbContext ctx = ctx;

    // Everyone can read articles
    [HttpGet]
    [AllowAnonymous] // Open to everyone
    public IEnumerable<ArticleDto> Get()
    {
        return ctx.Articles.Include(x => x.Author).Select(ArticleDto.FromEntity);
    }

    // Everyone can view articles by ID
    [HttpGet("{id}")]
    [AllowAnonymous] // Open to everyone
    public ArticleDto? GetById(int id)
    {
        return ctx
            .Articles.Include(x => x.Author)
            .Where(x => x.Id == id)
            .Select(ArticleDto.FromEntity)
            .SingleOrDefault();
    }

    // Only Editor and Writer can create articles
    [HttpPost]
    [Authorize(Roles = "Editor, Writer")]
    public ArticleDto Post([FromBody] ArticleFormDto dto)
    {
        var userName = HttpContext.User.Identity?.Name;
        var author = ctx.Users.Single(x => x.UserName == userName);
        var entity = new Article
        {
            Title = dto.Title,
            Content = dto.Content,
            Author = author,
            CreatedAt = DateTime.Now
        };
        var created = ctx.Articles.Add(entity).Entity;
        ctx.SaveChanges();
        return ArticleDto.FromEntity(created);
    }

    // Writers can only edit their own articles, Editors can edit any article
    [HttpPut("{id}")]
    [Authorize(Roles = "Editor, Writer")]
    public IActionResult Put(int id, [FromBody] ArticleFormDto dto)
    {
        var userName = HttpContext.User.Identity?.Name;
        var userRoles = HttpContext.User.Claims
            .Where(c => c.Type == ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList();

        var entity = ctx.Articles
            .Include(x => x.Author)
            .SingleOrDefault(x => x.Id == id);

        if (entity == null)
        {
            return NotFound(); // Return 404 if the article is not found
        }

        // Only allow Writers to edit their own articles, Editors can edit any
        if (userRoles.Contains("Writer") && entity.Author.UserName != userName)
        {
            return Forbid();
        }

        // Update article fields
        entity.Title = dto.Title;
        entity.Content = dto.Content;
        ctx.SaveChanges();

        return Ok(ArticleDto.FromEntity(entity));
    }

    // Only Editors can delete articles
    [HttpDelete("{id}")]
    [Authorize(Roles = "Editor")]
    public IActionResult Delete(int id)
    {
        var entity = ctx.Articles.SingleOrDefault(x => x.Id == id);

        if (entity == null)
        {
            return NotFound(); // Return 404 if the article is not found
        }

        ctx.Articles.Remove(entity);
        ctx.SaveChanges();
        return NoContent();
    }
}

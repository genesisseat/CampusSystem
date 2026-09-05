using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RegistrarMain.Contracts;
using RegistrarMain.Data;
using RegistrarMain.Models;

namespace RegistrarMain.Controllers;

[Route("api/courses")]
public sealed class CoursesController(RegistrarDbContext db) : RegistrarControllerBase(db)
{
    [HttpGet]
    [Authorize]
    public async Task<ActionResult<IReadOnlyList<CourseDto>>> List([FromQuery] string? search, CancellationToken cancellationToken)
    {
        var query = Db.Courses.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(course => course.Code.Contains(term) || course.Title.Contains(term));
        }

        var courses = await query.OrderBy(course => course.Code)
            .Select(course => new CourseDto(course.Id, course.Code, course.Title, course.Credits))
            .ToListAsync(cancellationToken);
        return Ok(courses);
    }

    [HttpGet("{id:int}")]
    [Authorize]
    public async Task<ActionResult<CourseDto>> Get(int id, CancellationToken cancellationToken)
    {
        var course = await Db.Courses.AsNoTracking()
            .Where(item => item.Id == id)
            .Select(item => new CourseDto(item.Id, item.Code, item.Title, item.Credits))
            .SingleOrDefaultAsync(cancellationToken);
        return course is null ? NotFound() : Ok(course);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<CourseDto>> Create(CreateCourseRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.Title) || request.Credits <= 0)
            return BadRequest("Code, title, and positive credits are required.");

        var course = new Course { Code = request.Code.Trim(), Title = request.Title.Trim(), Credits = request.Credits };
        Db.Courses.Add(course);
        await Db.SaveChangesAsync(cancellationToken);
        var response = new CourseDto(course.Id, course.Code, course.Title, course.Credits);
        return CreatedAtAction(nameof(Get), new { id = course.Id }, response);
    }
}

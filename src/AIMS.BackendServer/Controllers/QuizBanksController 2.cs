using AIMS.BackendServer.Data;
using AIMS.BackendServer.Data.Entities;
using AIMS.ViewModels.LMS;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using AIMS.BackendServer.Extensions;

namespace AIMS.BackendServer.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class QuizBanksController : ControllerBase
{
    private readonly AimsDbContext _context;

    public QuizBanksController(AimsDbContext context)
        => _context = context;

    // GET /api/quizbanks?courseId=1
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? courseId = null)
    {
        var query = _context.QuizBanks
            .Include(q => q.Course)
            .Include(q => q.CreatedByUser)
            .Include(q => q.Questions)
            .AsQueryable();

        if (courseId.HasValue)
            query = query.Where(q => q.CourseId == courseId.Value);

        var result = await query
            .Select(q => new QuizBankVm
            {
                Id = q.Id,
                CourseId = q.CourseId,
                CourseTitle = q.Course.Title,
                Title = q.Title,
                PassScore = q.PassScore,
                TimeLimit = q.TimeLimit,
                MaxAttempts = q.MaxAttempts,
                TotalQuestions = q.Questions.Count,
                CreatedByUser = q.CreatedByUser.FirstName + " " +
                                 q.CreatedByUser.LastName,
            })
            .ToListAsync();

        return Ok(result);
    }

    // GET /api/quizbanks/{id}  — Trả về cả questions + options
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var quiz = await _context.QuizBanks
            .Include(q => q.Course)
            .Include(q => q.CreatedByUser)
            .Include(q => q.Questions.OrderBy(qq => qq.SortOrder))
                .ThenInclude(qq => qq.Options.OrderBy(o => o.SortOrder))
            .FirstOrDefaultAsync(q => q.Id == id);

        if (quiz == null)
            return NotFound(new { message = $"QuizBank #{id} không tồn tại." });

        return Ok(new QuizBankVm
        {
            Id = quiz.Id,
            CourseId = quiz.CourseId,
            CourseTitle = quiz.Course.Title,
            Title = quiz.Title,
            PassScore = quiz.PassScore,
            TimeLimit = quiz.TimeLimit,
            MaxAttempts = quiz.MaxAttempts,
            TotalQuestions = quiz.Questions.Count,
            CreatedByUser = $"{quiz.CreatedByUser.FirstName} {quiz.CreatedByUser.LastName}",
            Questions = quiz.Questions.Select(q => new QuizQuestionVm
            {
                Id = q.Id,
                QuizBankId = q.QuizBankId,
                QuestionText = q.QuestionText,
                QuestionType = q.QuestionType,
                Score = q.Score,
                SortOrder = q.SortOrder,
                // Intern không thấy IsCorrect khi làm bài
                Options = q.Options.Select(o => new QuestionOptionVm
                {
                    Id = o.Id,
                    OptionText = o.OptionText,
                    IsCorrect = User.IsInRole("Admin") || User.IsInRole("Mentor")
                        ? o.IsCorrect : false, // Ẩn đáp án đúng với Intern
                    SortOrder = o.SortOrder,
                }).ToList(),
            }).ToList(),
        });
    }

    // POST /api/quizbanks
    [HttpPost]
    [Authorize(Roles = "Admin,Mentor")]
    public async Task<IActionResult> Create(
        [FromBody] CreateQuizBankRequest request)
    {
        var userId = User.GetUserId();

        var courseExists = await _context.Courses
            .AnyAsync(c => c.Id == request.CourseId);
        if (!courseExists)
            return NotFound(new { message = $"Course #{request.CourseId} không tồn tại." });

        var quiz = new QuizBank
        {
            CourseId = request.CourseId,
            Title = request.Title,
            PassScore = request.PassScore,
            TimeLimit = request.TimeLimit,
            MaxAttempts = request.MaxAttempts,
            CreatedByUserId = userId,
        };

        _context.QuizBanks.Add(quiz);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = quiz.Id },
            new { quiz.Id, quiz.Title, quiz.PassScore });
    }

    // POST /api/quizbanks/{id}/questions  — Thêm câu hỏi + options
    [HttpPost("{id}/questions")]
    [Authorize(Roles = "Admin,Mentor")]
    public async Task<IActionResult> AddQuestion(
        int id, [FromBody] CreateQuestionRequest request)
    {
        var quizExists = await _context.QuizBanks.AnyAsync(q => q.Id == id);
        if (!quizExists)
            return NotFound(new { message = $"QuizBank #{id} không tồn tại." });

        // Validate: phải có ít nhất 1 đáp án đúng
        if (!request.Options.Any(o => o.IsCorrect))
            return BadRequest(new { message = "Phải có ít nhất 1 đáp án đúng." });

        var question = new QuizQuestion
        {
            QuizBankId = id,
            QuestionText = request.QuestionText,
            QuestionType = request.QuestionType.ToUpper(),
            Score = request.Score,
            SortOrder = request.SortOrder,
            Options = request.Options.Select(o => new QuestionOption
            {
                OptionText = o.OptionText,
                IsCorrect = o.IsCorrect,
                SortOrder = o.SortOrder,
            }).ToList(),
        };

        _context.QuizQuestions.Add(question);
        await _context.SaveChangesAsync();

        return Ok(new { question.Id, question.QuestionText, OptionCount = question.Options.Count });
    }

    // DELETE /api/quizbanks/{id}
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,Mentor")]
    public async Task<IActionResult> Delete(int id)
    {
        var quiz = await _context.QuizBanks
            .Include(q => q.Questions)
            .FirstOrDefaultAsync(q => q.Id == id);

        if (quiz == null)
            return NotFound(new { message = $"QuizBank #{id} không tồn tại." });

        _context.QuizBanks.Remove(quiz);
        await _context.SaveChangesAsync();
        return Ok(new { message = "Đã xóa QuizBank." });
    }
}
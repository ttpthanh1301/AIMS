using AIMS.BackendServer.Data;
using AIMS.BackendServer.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using AIMS.BackendServer.Extensions;
using AIMS.ViewModels.LMS;
namespace AIMS.BackendServer.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class QuizBanksController : ControllerBase
{
    private readonly AimsDbContext _context;
    private readonly ILogger<QuizBanksController> _logger;

    public QuizBanksController(AimsDbContext context, ILogger<QuizBanksController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET /api/quizbanks?courseId=1
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? courseId = null)
    {
        var query = _context.QuizBanks.AsNoTracking().AsQueryable();

        if (courseId.HasValue)
            query = query.Where(q => q.CourseId == courseId.Value);

        var result = await query
            .OrderByDescending(q => q.Id)
            .Select(q => new QuizBankVm
            {
                Id = q.Id,
                CourseId = q.CourseId,
                CourseTitle = q.Course != null ? q.Course.Title : string.Empty,
                LessonId = q.LessonId,
                LessonTitle = q.Lesson != null ? q.Lesson.Title : null,
                Title = q.Title,
                PassScore = q.PassScore,
                TimeLimit = q.TimeLimit,
                MaxAttempts = q.MaxAttempts,
                TotalQuestions = q.Questions.Count,
                CreatedByUser = q.CreatedByUser != null
                    ? (q.CreatedByUser.FirstName + " " + q.CreatedByUser.LastName).Trim()
                    : string.Empty,
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
            .Include(q => q.Lesson)
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
            LessonId = quiz.LessonId,
            LessonTitle = quiz.Lesson?.Title,
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

        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(new { message = "Không xác định được người dùng hiện tại." });

        if (string.IsNullOrWhiteSpace(request.Title))
            return BadRequest(new { message = "Tên quiz không được để trống." });

        if (request.PassScore < 0 || request.PassScore > 100)
            return BadRequest(new { message = "Điểm pass phải nằm trong khoảng 0 đến 100." });

        if (request.MaxAttempts < 1)
            return BadRequest(new { message = "Số lần thi tối đa phải lớn hơn 0." });

        var courseExists = await _context.Courses
            .AnyAsync(c => c.Id == request.CourseId);
        if (!courseExists)
            return NotFound(new { message = $"Course #{request.CourseId} không tồn tại." });

        if (request.LessonId.HasValue)
        {
            var lessonExists = await _context.Lessons
                .Include(l => l.Chapter)
                .AnyAsync(l => l.Id == request.LessonId.Value && l.Chapter.CourseId == request.CourseId);
            if (!lessonExists)
                return BadRequest(new { message = "Bài học không thuộc khóa học đã chọn." });
        }

        var duplicateQuiz = await _context.QuizBanks.AnyAsync(q =>
            q.CourseId == request.CourseId &&
            q.LessonId == request.LessonId &&
            q.Title.ToLower() == request.Title.Trim().ToLower());
        if (duplicateQuiz)
            return BadRequest(new { message = "Quiz này đã tồn tại trong khóa học hoặc bài học đã chọn." });

        var quiz = new QuizBank
        {
            CourseId = request.CourseId,
            LessonId = request.LessonId,
            Title = request.Title.Trim(),
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

    // PUT /api/quizbanks/{id}
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Mentor")]
    public async Task<IActionResult> Update(
        int id, [FromBody] UpdateQuizBankRequest request)
    {
        var quiz = await _context.QuizBanks.FindAsync(id);
        if (quiz == null)
            return NotFound(new { message = $"QuizBank #{id} không tồn tại." });

        if (string.IsNullOrWhiteSpace(request.Title))
            return BadRequest(new { message = "Tên quiz không được để trống." });

        if (request.PassScore < 0 || request.PassScore > 100)
            return BadRequest(new { message = "Điểm pass phải nằm trong khoảng 0 đến 100." });

        if (request.MaxAttempts < 1)
            return BadRequest(new { message = "Số lần thi tối đa phải lớn hơn 0." });

        var courseExists = await _context.Courses.AnyAsync(c => c.Id == request.CourseId);
        if (!courseExists)
            return NotFound(new { message = $"Course #{request.CourseId} không tồn tại." });

        if (request.LessonId.HasValue)
        {
            var lessonExists = await _context.Lessons
                .Include(l => l.Chapter)
                .AnyAsync(l => l.Id == request.LessonId.Value && l.Chapter.CourseId == request.CourseId);
            if (!lessonExists)
                return BadRequest(new { message = "Bài học không thuộc khóa học đã chọn." });
        }

        var duplicateQuiz = await _context.QuizBanks.AnyAsync(q =>
            q.Id != id &&
            q.CourseId == request.CourseId &&
            q.LessonId == request.LessonId &&
            q.Title.ToLower() == request.Title.Trim().ToLower());
        if (duplicateQuiz)
            return BadRequest(new { message = "Đã có quiz khác cùng tên trong khóa học hoặc bài học đã chọn." });

        quiz.CourseId = request.CourseId;
        quiz.LessonId = request.LessonId;
        quiz.Title = request.Title.Trim();
        quiz.PassScore = request.PassScore;
        quiz.TimeLimit = request.TimeLimit;
        quiz.MaxAttempts = request.MaxAttempts;

        await _context.SaveChangesAsync();
        return Ok(new { message = "Cập nhật Quiz thành công." });
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

        if (string.IsNullOrWhiteSpace(request.QuestionText))
            return BadRequest(new { message = "Nội dung câu hỏi không được để trống." });

        request.QuestionType = request.QuestionType?.Trim().ToUpperInvariant() ?? "SINGLE";

        if (request.QuestionType is not ("SINGLE" or "MULTIPLE" or "TEXT"))
            return BadRequest(new { message = "Loại câu hỏi không hợp lệ." });

        if (request.Score <= 0)
            return BadRequest(new { message = "Điểm của câu hỏi phải lớn hơn 0." });

        var options = request.Options
            .Where(o => !string.IsNullOrWhiteSpace(o.OptionText))
            .Select((o, index) => new QuestionOption
            {
                OptionText = o.OptionText.Trim(),
                IsCorrect = o.IsCorrect,
                SortOrder = index + 1,
            })
            .ToList();

        if (request.QuestionType != "TEXT" && options.Count < 2)
            return BadRequest(new { message = "Câu hỏi trắc nghiệm phải có ít nhất 2 đáp án." });

        if (request.QuestionType != "TEXT" && !options.Any(o => o.IsCorrect))
            return BadRequest(new { message = "Phải có ít nhất 1 đáp án đúng." });

        if (request.QuestionType == "SINGLE" && options.Count(o => o.IsCorrect) != 1)
            return BadRequest(new { message = "Câu hỏi single choice phải có đúng 1 đáp án đúng." });

        var question = new QuizQuestion
        {
            QuizBankId = id,
            QuestionText = request.QuestionText.Trim(),
            QuestionType = request.QuestionType,
            Score = request.Score,
            SortOrder = request.SortOrder,
            Options = options,
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

        // ⭐ Kiểm tra đã có học sinh thi chưa
        var hasAttempts = await _context.UserQuizAttempts
            .AnyAsync(a => a.QuizBankId == id);

        if (hasAttempts)
            return BadRequest(new
            {
                message = "Không thể xóa Quiz này vì đã có học sinh làm bài. " +
                          "Hãy đóng Quiz thay vì xóa."
            });

        _context.QuizBanks.Remove(quiz);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Đã xóa QuizBank." });
    }
}

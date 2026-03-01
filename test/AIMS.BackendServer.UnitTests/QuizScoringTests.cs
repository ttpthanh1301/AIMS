using AIMS.BackendServer.UnitTests.Helpers;
using AIMS.BackendServer.Data.Entities;
using FluentAssertions;
using Xunit;

namespace AIMS.BackendServer.UnitTests;

public class QuizScoringTests
{
    // ─────────────────────────────────────────────────────────
    // TEST: Chấm điểm quiz — tất cả đúng
    // ─────────────────────────────────────────────────────────
    [Fact]
    public void QuizScoring_AllCorrect_Returns100Percent()
    {
        // Arrange
        var questions = new List<QuizQuestion>
        {
            new QuizQuestion { Id = 1, Score = 1, QuizBankId = 1,
                QuestionText = "C# là gì?", QuestionType = "SINGLE", SortOrder = 1,
                Options = new List<QuestionOption>
                {
                    new QuestionOption { Id = 1, IsCorrect = true,  OptionText = "Ngôn ngữ lập trình" },
                    new QuestionOption { Id = 2, IsCorrect = false, OptionText = "Cơ sở dữ liệu" },
                }
            },
            new QuizQuestion { Id = 2, Score = 1, QuizBankId = 1,
                QuestionText = ".NET là gì?", QuestionType = "SINGLE", SortOrder = 2,
                Options = new List<QuestionOption>
                {
                    new QuestionOption { Id = 3, IsCorrect = true,  OptionText = "Framework" },
                    new QuestionOption { Id = 4, IsCorrect = false, OptionText = "Database" },
                }
            },
        };

        // Answers: chọn đúng tất cả
        var answers = new List<(int QuestionId, int SelectedOptionId)>
        {
            (1, 1),  // Chọn đáp án đúng của Q1
            (2, 3),  // Chọn đáp án đúng của Q2
        };

        // Act
        var score = CalculateScore(questions, answers);

        // Assert
        score.TotalScore.Should().Be(2);
        score.IsPassed.Should().BeTrue();   // 2/2 = 100% > 70%
    }

    // ─────────────────────────────────────────────────────────
    // TEST: Chấm điểm quiz — tất cả sai
    // ─────────────────────────────────────────────────────────
    [Fact]
    public void QuizScoring_AllWrong_Returns0()
    {
        // Arrange
        var questions = new List<QuizQuestion>
        {
            new QuizQuestion { Id = 1, Score = 1, QuizBankId = 1,
                QuestionText = "Q1", QuestionType = "SINGLE", SortOrder = 1,
                Options = new List<QuestionOption>
                {
                    new QuestionOption { Id = 1, IsCorrect = true,  OptionText = "Đúng" },
                    new QuestionOption { Id = 2, IsCorrect = false, OptionText = "Sai" },
                }
            },
        };

        var answers = new List<(int QuestionId, int SelectedOptionId)>
        {
            (1, 2),  // Chọn sai
        };

        // Act
        var score = CalculateScore(questions, answers);

        // Assert
        score.TotalScore.Should().Be(0);
        score.IsPassed.Should().BeFalse();
    }

    // ─────────────────────────────────────────────────────────
    // TEST: PassScore boundary — đúng đúng 70%
    // ─────────────────────────────────────────────────────────
    [Theory]
    [InlineData(7, 10, true)]   // 7/10 = 70% → Pass
    [InlineData(6, 10, false)]  // 6/10 = 60% → Fail
    [InlineData(10, 10, true)]  // 10/10 = 100% → Pass
    [InlineData(0, 10, false)]  // 0/10 = 0% → Fail
    public void QuizScoring_PassScoreBoundary(
        int correctCount, int totalCount, bool expectedPassed)
    {
        // Arrange
        decimal passScore = 70m;
        decimal totalScore = correctCount;
        decimal maxScore = totalCount;

        // Act
        decimal percent = (totalScore / maxScore) * 100;
        bool isPassed = percent >= passScore;

        // Assert
        isPassed.Should().Be(expectedPassed);
    }

    // ─────────────────────────────────────────────────────────
    // Helper: tính điểm quiz
    // ─────────────────────────────────────────────────────────
    private static (decimal TotalScore, bool IsPassed) CalculateScore(
        List<QuizQuestion> questions,
        List<(int QuestionId, int SelectedOptionId)> answers)
    {
        decimal total = 0;

        foreach (var answer in answers)
        {
            var question = questions.FirstOrDefault(q => q.Id == answer.QuestionId);
            if (question == null) continue;

            var selectedOption = question.Options
                .FirstOrDefault(o => o.Id == answer.SelectedOptionId);

            if (selectedOption?.IsCorrect == true)
                total += question.Score;
        }

        decimal maxScore = questions.Sum(q => q.Score);
        decimal percent = maxScore > 0 ? (total / maxScore) * 100 : 0;
        bool isPassed = percent >= 70m;

        return (total, isPassed);
    }
}
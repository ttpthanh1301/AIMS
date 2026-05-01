using AIMS.ViewModels.Recruitment;
using AIMS.WebPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIMS.WebPortal.Areas.HR.Controllers;

[Area("HR")]
[Authorize(Roles = "HR,Admin")]
public class JobDescriptionController : Controller
{
    private readonly BackendApiClient _api;

    public JobDescriptionController(BackendApiClient api)
        => _api = api;

    // Danh sách JD
    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Job Descriptions";
        var result = await _api.GetWithMessageAsync<List<JobDescriptionVm>>(
            "/api/jobdescriptions");

        if (!string.IsNullOrWhiteSpace(result.Message))
            ViewData["Error"] = result.Message;

        return View(result.Data ?? new List<JobDescriptionVm>());
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        ViewData["Title"] = "Chi tiết Job Description";
        var result = await _api.GetWithMessageAsync<JobDescriptionVm>(
            $"/api/jobdescriptions/{id}");

        if (result.Data == null)
        {
            TempData["Error"] = result.Message ?? $"Không tìm thấy JD #{id}.";
            return RedirectToAction("Index");
        }

        if (!string.IsNullOrWhiteSpace(result.Message))
            ViewData["Error"] = result.Message;

        return View(result.Data);
    }

    // Form tạo JD
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        ViewData["Title"] = "Tạo Job Description";
        await LoadPositionsAsync();
        return View(new JobDescriptionFormVm());
    }

    // Submit tạo JD
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(JobDescriptionFormVm model)
    {
        ViewData["Title"] = "Tạo Job Description";
        await LoadPositionsAsync();

        var positionId = await EnsureJobPositionAsync(model);
        if (positionId == null)
            return View(model);

        var body = new
        {
            jobPositionId = positionId.Value,
            title = model.Title,
            detailContent = model.DetailContent,
            requiredSkills = model.RequiredSkills,
            minGPA = model.MinGPA,
        };

        var result = await _api.PostWithMessageAsync(
            "/api/jobdescriptions", body);

        if (!result.Success)
        {
            ViewData["Error"] = result.Message ?? "Không thể tạo JD.";
            return View(model);
        }

        TempData["Success"] = result.Message ?? "Tạo JD thành công!";
        return RedirectToAction("Index");
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        ViewData["Title"] = "Chỉnh sửa Job Description";

        var jdResult = await _api.GetWithMessageAsync<JobDescriptionVm>(
            $"/api/jobdescriptions/{id}");
        await LoadPositionsAsync();

        if (jdResult.Data == null)
        {
            TempData["Error"] = jdResult.Message ?? $"Không tìm thấy JD #{id}.";
            return RedirectToAction("Index");
        }

        if (!string.IsNullOrWhiteSpace(jdResult.Message))
            ViewData["Error"] = jdResult.Message;

        return View(new JobDescriptionFormVm
        {
            Id = jdResult.Data.Id,
            JobPositionId = jdResult.Data.JobPositionId,
            Title = jdResult.Data.Title,
            DetailContent = jdResult.Data.DetailContent,
            RequiredSkills = jdResult.Data.RequiredSkills,
            MinGPA = jdResult.Data.MinGPA,
            DeadlineDate = jdResult.Data.DeadlineDate,
            Status = jdResult.Data.Status,
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, JobDescriptionFormVm model)
    {
        ViewData["Title"] = "Chỉnh sửa Job Description";
        model.Id = id;
        await LoadPositionsAsync();

        var positionId = await EnsureJobPositionAsync(model);
        if (positionId == null)
            return View(model);

        var body = new
        {
            jobPositionId = positionId.Value,
            title = model.Title,
            detailContent = model.DetailContent,
            requiredSkills = model.RequiredSkills,
            minGPA = model.MinGPA,
            status = model.Status,
        };

        var result = await _api.PutWithMessageAsync($"/api/jobdescriptions/{id}", body);
        if (!result.Success)
        {
            ViewData["Error"] = result.Message ?? "Không thể cập nhật JD.";
            return View(model);
        }

        TempData["Success"] = result.Message ?? "Cập nhật JD thành công!";
        return RedirectToAction("Details", new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _api.DeleteWithMessageAsync($"/api/jobdescriptions/{id}");
        TempData[result.Success ? "Success" : "Error"] =
            result.Message ?? (result.Success ? "Đã xóa JD." : "Không thể xóa JD.");

        return RedirectToAction("Index");
    }

    private async Task LoadPositionsAsync()
    {
        var result = await _api.GetWithMessageAsync<List<JobPositionVm>>(
            "/api/jobpositions");

        if (!string.IsNullOrWhiteSpace(result.Message))
            ViewData["Error"] = result.Message;

        ViewBag.Positions = result.Data ?? new List<JobPositionVm>();
    }

    private async Task<int?> EnsureJobPositionAsync(JobDescriptionFormVm model)
    {
        if (!string.IsNullOrWhiteSpace(model.NewJobPositionTitle))
        {
            var createPosition = await _api.PostWithMessageAsync<JobPositionVm>(
                "/api/jobpositions",
                new
                {
                    title = model.NewJobPositionTitle.Trim(),
                    description = string.IsNullOrWhiteSpace(model.NewJobPositionDescription)
                        ? null
                        : model.NewJobPositionDescription.Trim(),
                });

            if (createPosition.Data == null)
            {
                ViewData["Error"] = createPosition.Message ?? "Không thể tạo vị trí tuyển dụng mới.";
                return null;
            }

            model.JobPositionId = createPosition.Data.Id;
            return createPosition.Data.Id;
        }

        if (model.JobPositionId.GetValueOrDefault() <= 0)
        {
            ViewData["Error"] = "Vui lòng chọn vị trí tuyển dụng hoặc tạo vị trí mới.";
            return null;
        }

        return model.JobPositionId.GetValueOrDefault();
    }
}

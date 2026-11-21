using System.Net;
using MediQueue.Contracts.Catalogs;
using MediQueue.Web.Authentication;
using MediQueue.Web.Clients;
using MediQueue.Web.Models.ViewModels.Catalog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediQueue.Web.Controllers;

[Authorize(Roles = "Admin"), Route("Departments")]
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
public sealed class DepartmentsController(ICatalogApiClient api, IMvcAuthenticationService authentication) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index([FromQuery] DepartmentListQuery query, CancellationToken ct)
    {
        AuthenticationSession? session = await Session(ct); if (session is null) return Expired();
        ApiClientResult<PagedDepartmentsResponse> result = await api.GetDepartmentsAsync(session.AccessToken, query, ct);
        return result.Succeeded ? View(new DepartmentsIndexViewModel(query, result.Value!)) : Failure(result.Problem!);
    }
    [HttpGet("Details/{id:guid}")]
    public async Task<IActionResult> Details(Guid id, CancellationToken ct) { var result = await Get(id, ct); return result.Result ?? View(result.Value); }
    [HttpGet("Create")] public IActionResult Create() => View(new DepartmentFormViewModel());
    [HttpPost("Create"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(DepartmentFormViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(model);
        AuthenticationSession? session = await Session(ct); if (session is null) return Expired();
        var result = await api.CreateDepartmentAsync(session.AccessToken, new(model.Code, model.Name), ct);
        if (!result.Succeeded) { if (Editable(result.Problem!)) { Apply(result.Problem!); return View(model); } return Failure(result.Problem!); }
        TempData["SuccessMessage"] = "Đã tạo chuyên khoa mới."; return RedirectToAction(nameof(Details), new { id = result.Value!.Id });
    }
    [HttpGet("Edit/{id:guid}")]
    public async Task<IActionResult> Edit(Guid id, CancellationToken ct) { var result = await Get(id, ct); return result.Result ?? View(new DepartmentFormViewModel { Id = id, Code = result.Value!.Code, Name = result.Value.Name }); }
    [HttpPost("Edit/{id:guid}"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, DepartmentFormViewModel model, CancellationToken ct)
    {
        if (id != model.Id) return BadRequest(); if (!ModelState.IsValid) return View(model);
        AuthenticationSession? session = await Session(ct); if (session is null) return Expired();
        var result = await api.UpdateDepartmentAsync(session.AccessToken, id, new(model.Code, model.Name), ct);
        if (!result.Succeeded) { if (Editable(result.Problem!)) { Apply(result.Problem!); return View(model); } return Failure(result.Problem!); }
        TempData["SuccessMessage"] = "Đã cập nhật chuyên khoa."; return RedirectToAction(nameof(Details), new { id });
    }
    [HttpPost("{id:guid}/activate"), ValidateAntiForgeryToken] public Task<IActionResult> Activate(Guid id, CancellationToken ct) => Command(id, true, ct);
    [HttpPost("{id:guid}/deactivate"), ValidateAntiForgeryToken] public Task<IActionResult> Deactivate(Guid id, CancellationToken ct) => Command(id, false, ct);
    private async Task<IActionResult> Command(Guid id, bool active, CancellationToken ct)
    {
        AuthenticationSession? session = await Session(ct); if (session is null) return Expired();
        ApiClientResult result = await api.SetDepartmentActiveAsync(session.AccessToken, id, active, ct);
        if (!result.Succeeded) return Failure(result.Problem!);
        TempData["SuccessMessage"] = active ? "Đã kích hoạt chuyên khoa." : "Đã vô hiệu hóa chuyên khoa."; return RedirectToAction(nameof(Details), new { id });
    }
    private async Task<(DepartmentDetailsResponse? Value, IActionResult? Result)> Get(Guid id, CancellationToken ct)
    { AuthenticationSession? session = await Session(ct); if (session is null) return (null, Expired()); var result = await api.GetDepartmentAsync(session.AccessToken, id, ct); return result.Succeeded ? (result.Value, null) : (null, Failure(result.Problem!)); }
    private Task<AuthenticationSession?> Session(CancellationToken ct) => authentication.GetCurrentSessionAsync(HttpContext, ct);
    private IActionResult Expired() { TempData["WarningMessage"] = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại."; return RedirectToAction("Login", "Account"); }
    private IActionResult Failure(ApiProblem p)
    {
        if (p.StatusCode == HttpStatusCode.Unauthorized) return Expired(); if (p.StatusCode == HttpStatusCode.Forbidden) return RedirectToAction("AccessDenied", "Account");
        int status = (int)(p.StatusCode ?? HttpStatusCode.ServiceUnavailable); Response.StatusCode = status;
        return View("~/Views/Shared/CatalogError.cshtml", new CatalogErrorViewModel(p.Title, p.Detail, status, "Departments"));
    }
    private static bool Editable(ApiProblem p) => p.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Conflict;
    private void Apply(ApiProblem p) { foreach (var pair in p.ValidationErrors) foreach (string message in pair.Value) ModelState.AddModelError(pair.Key, message); if (p.ValidationErrors.Count == 0) ModelState.AddModelError(string.Empty, p.Detail); }
}

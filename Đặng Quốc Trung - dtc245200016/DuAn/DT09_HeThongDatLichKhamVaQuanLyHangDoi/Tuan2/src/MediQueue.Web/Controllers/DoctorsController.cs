using System.Net;
using MediQueue.Contracts.Profiles;
using MediQueue.Web.Authentication;
using MediQueue.Web.Clients;
using MediQueue.Web.Models.ViewModels.Catalog;
using MediQueue.Web.Models.ViewModels.Profiles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediQueue.Web.Controllers;

[Authorize, Route("Doctors")]
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
public sealed class DoctorsController(IProfilesApiClient api, IMvcAuthenticationService authentication) : Controller
{
    [HttpGet(""), Authorize(Roles = "Admin")]
    public async Task<IActionResult> Index([FromQuery] DoctorListQuery query, CancellationToken ct)
    {
        AuthenticationSession? session = await Session(ct); if (session is null) return Expired();
        var result = await api.GetDoctorsAsync(session.AccessToken, query, ct);
        return result.Succeeded ? View(new DoctorsIndexViewModel(query, result.Value!)) : Failure(result.Problem!, "Doctors");
    }

    [HttpGet("Details/{id:guid}"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> Details(Guid id, CancellationToken ct) { var result = await Get(id, ct); return result.Result ?? View(result.Value); }

    [HttpGet("Create"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(CancellationToken ct)
    {
        var result = await Options(ct); return result.Result ?? View(new CreateDoctorProfileViewModel { Accounts = result.Value!.Accounts, Departments = result.Value.Departments });
    }

    [HttpPost("Create"), Authorize(Roles = "Admin"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateDoctorProfileViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid) return await CreateView(model, ct);
        AuthenticationSession? session = await Session(ct); if (session is null) return Expired();
        var result = await api.CreateDoctorAsync(session.AccessToken, new() { UserId = model.UserId, DepartmentId = model.DepartmentId, DisplayName = model.DisplayName }, ct);
        if (!result.Succeeded)
        {
            if (Editable(result.Problem!)) { Apply(result.Problem!); return await CreateView(model, ct); }
            return Failure(result.Problem!, "Doctors");
        }
        TempData["SuccessMessage"] = "Đã tạo hồ sơ bác sĩ và liên kết tài khoản.";
        return RedirectToAction(nameof(Details), new { id = result.Value!.Id });
    }

    [HttpGet("Edit/{id:guid}"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
    {
        var result = await Get(id, ct); if (result.Result is not null) return result.Result;
        DoctorDetailsResponse doctor = result.Value!;
        return await EditView(new EditDoctorProfileViewModel { Id = id, UserId = doctor.UserId, Email = doctor.Email, DisplayName = doctor.DisplayName, DepartmentId = doctor.DepartmentId, RowVersion = doctor.RowVersion }, doctor.DepartmentCode, doctor.DepartmentName, ct);
    }

    [HttpPost("Edit/{id:guid}"), Authorize(Roles = "Admin"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, EditDoctorProfileViewModel model, CancellationToken ct)
    {
        if (id != model.Id) return BadRequest();
        if (!ModelState.IsValid) return await EditView(model, null, null, ct);
        AuthenticationSession? session = await Session(ct); if (session is null) return Expired();
        var result = await api.UpdateDoctorAsync(session.AccessToken, id, new() { DisplayName = model.DisplayName, DepartmentId = model.DepartmentId, RowVersion = model.RowVersion }, ct);
        if (!result.Succeeded)
        {
            if (Editable(result.Problem!)) { Apply(result.Problem!); return await EditView(model, null, null, ct); }
            return Failure(result.Problem!, "Doctors");
        }
        TempData["SuccessMessage"] = "Đã cập nhật hồ sơ bác sĩ.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet("Profile"), Authorize(Roles = "Doctor")]
    public async Task<IActionResult> Profile(CancellationToken ct)
    {
        AuthenticationSession? session = await Session(ct); if (session is null) return Expired();
        var result = await api.GetDoctorSelfAsync(session.AccessToken, ct);
        return result.Succeeded ? View("Self", result.Value!) : Failure(result.Problem!, "Dashboard");
    }

    private async Task<IActionResult> CreateView(CreateDoctorProfileViewModel model, CancellationToken ct)
    {
        var options = await Options(ct); if (options.Result is not null) return options.Result;
        model.Accounts = options.Value!.Accounts; model.Departments = options.Value.Departments; return View("Create", model);
    }

    private async Task<IActionResult> EditView(EditDoctorProfileViewModel model, string? currentCode, string? currentName, CancellationToken ct)
    {
        var options = await Options(ct); if (options.Result is not null) return options.Result;
        List<DoctorDepartmentOptionResponse> departments = [.. options.Value!.Departments];
        if (!departments.Any(x => x.Id == model.DepartmentId) && currentCode is not null && currentName is not null)
            departments.Add(new(model.DepartmentId, currentCode, currentName));
        model.Departments = departments.OrderBy(x => x.Name).ToArray(); return View("Edit", model);
    }

    private async Task<(DoctorDetailsResponse? Value, IActionResult? Result)> Get(Guid id, CancellationToken ct)
    { AuthenticationSession? session = await Session(ct); if (session is null) return (null, Expired()); var result = await api.GetDoctorAsync(session.AccessToken, id, ct); return result.Succeeded ? (result.Value, null) : (null, Failure(result.Problem!, "Doctors")); }
    private async Task<(DoctorCreateOptionsResponse? Value, IActionResult? Result)> Options(CancellationToken ct)
    { AuthenticationSession? session = await Session(ct); if (session is null) return (null, Expired()); var result = await api.GetDoctorCreateOptionsAsync(session.AccessToken, ct); return result.Succeeded ? (result.Value, null) : (null, Failure(result.Problem!, "Doctors")); }
    private Task<AuthenticationSession?> Session(CancellationToken ct) => authentication.GetCurrentSessionAsync(HttpContext, ct);
    private IActionResult Expired() { TempData["WarningMessage"] = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại."; return RedirectToAction("Login", "Account"); }
    private IActionResult Failure(ApiProblem p, string returnController)
    { if (p.StatusCode == HttpStatusCode.Unauthorized) return Expired(); if (p.StatusCode == HttpStatusCode.Forbidden) return RedirectToAction("AccessDenied", "Account"); int status = (int)(p.StatusCode ?? HttpStatusCode.ServiceUnavailable); Response.StatusCode = status; return View("~/Views/Shared/CatalogError.cshtml", new CatalogErrorViewModel(p.Title, p.Detail, status, returnController)); }
    private static bool Editable(ApiProblem p) => p.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Conflict;
    private void Apply(ApiProblem p) { foreach (var pair in p.ValidationErrors) foreach (string message in pair.Value) ModelState.AddModelError(pair.Key, message); if (p.ValidationErrors.Count == 0) ModelState.AddModelError(string.Empty, p.Detail); }
}

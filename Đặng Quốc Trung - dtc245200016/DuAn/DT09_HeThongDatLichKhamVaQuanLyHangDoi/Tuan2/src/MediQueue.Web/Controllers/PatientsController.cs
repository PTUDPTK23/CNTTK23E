using System.Net;
using MediQueue.Contracts.Profiles;
using MediQueue.Web.Authentication;
using MediQueue.Web.Clients;
using MediQueue.Web.Models.ViewModels.Catalog;
using MediQueue.Web.Models.ViewModels.Profiles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediQueue.Web.Controllers;

[Authorize(Roles = "Admin"), Route("Patients")]
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
public sealed class PatientsController(IProfilesApiClient api, IMvcAuthenticationService authentication) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index([FromQuery] PatientListQuery query, CancellationToken ct)
    { AuthenticationSession? s = await Session(ct); if (s is null) return Expired(); var r = await api.GetPatientsAsync(s.AccessToken, query, ct); return r.Succeeded ? View(new PatientsIndexViewModel(query, r.Value!)) : Failure(r.Problem!); }

    [HttpGet("Details/{id:guid}")]
    public async Task<IActionResult> Details(Guid id, CancellationToken ct) { var r = await Get(id, ct); return r.Result ?? View(r.Value); }

    [HttpGet("Edit/{id:guid}")]
    public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
    {
        var r = await Get(id, ct); if (r.Result is not null) return r.Result; PatientDetailsResponse p = r.Value!;
        return View(new EditPatientProfileViewModel { Id = id, Email = p.Email, FullName = p.FullName, PhoneNumber = p.PhoneNumber, DateOfBirth = p.DateOfBirth, RowVersion = p.RowVersion });
    }

    [HttpPost("Edit/{id:guid}"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, EditPatientProfileViewModel model, CancellationToken ct)
    {
        if (id != model.Id) return BadRequest(); if (!ModelState.IsValid) return View(model);
        AuthenticationSession? s = await Session(ct); if (s is null) return Expired();
        var r = await api.UpdatePatientAsync(s.AccessToken, id, new() { FullName = model.FullName, PhoneNumber = model.PhoneNumber, DateOfBirth = model.DateOfBirth, RowVersion = model.RowVersion }, ct);
        if (!r.Succeeded) { if (Editable(r.Problem!)) { Apply(r.Problem!); return View(model); } return Failure(r.Problem!); }
        TempData["SuccessMessage"] = "Đã cập nhật hồ sơ bệnh nhân."; return RedirectToAction(nameof(Details), new { id });
    }

    private async Task<(PatientDetailsResponse? Value, IActionResult? Result)> Get(Guid id, CancellationToken ct)
    { AuthenticationSession? s = await Session(ct); if (s is null) return (null, Expired()); var r = await api.GetPatientAsync(s.AccessToken, id, ct); return r.Succeeded ? (r.Value, null) : (null, Failure(r.Problem!)); }
    private Task<AuthenticationSession?> Session(CancellationToken ct) => authentication.GetCurrentSessionAsync(HttpContext, ct);
    private IActionResult Expired() { TempData["WarningMessage"] = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại."; return RedirectToAction("Login", "Account"); }
    private IActionResult Failure(ApiProblem p)
    { if (p.StatusCode == HttpStatusCode.Unauthorized) return Expired(); if (p.StatusCode == HttpStatusCode.Forbidden) return RedirectToAction("AccessDenied", "Account"); int status = (int)(p.StatusCode ?? HttpStatusCode.ServiceUnavailable); Response.StatusCode = status; return View("~/Views/Shared/CatalogError.cshtml", new CatalogErrorViewModel(p.Title, p.Detail, status, "Patients")); }
    private static bool Editable(ApiProblem p) => p.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Conflict;
    private void Apply(ApiProblem p) { foreach (var pair in p.ValidationErrors) foreach (string message in pair.Value) ModelState.AddModelError(pair.Key, message); if (p.ValidationErrors.Count == 0) ModelState.AddModelError(string.Empty, p.Detail); }
}

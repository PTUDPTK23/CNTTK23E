using System.Net;
using MediQueue.Contracts.Catalogs;
using MediQueue.Web.Authentication;
using MediQueue.Web.Clients;
using MediQueue.Web.Models.ViewModels.Catalog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediQueue.Web.Controllers;

[Authorize(Roles = "Admin"), Route("Rooms")]
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
public sealed class RoomsController(ICatalogApiClient api, IMvcAuthenticationService authentication) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index([FromQuery] RoomListQuery query, CancellationToken ct)
    { AuthenticationSession? s = await Session(ct); if (s is null) return Expired(); var r = await api.GetRoomsAsync(s.AccessToken, query, ct); return r.Succeeded ? View(new RoomsIndexViewModel(query, r.Value!)) : Failure(r.Problem!); }
    [HttpGet("Details/{id:guid}")] public async Task<IActionResult> Details(Guid id, CancellationToken ct) { var r = await Get(id, ct); return r.Result ?? View(r.Value); }
    [HttpGet("Create")] public IActionResult Create() => View(new RoomFormViewModel());
    [HttpPost("Create"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RoomFormViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(model); AuthenticationSession? s = await Session(ct); if (s is null) return Expired();
        var r = await api.CreateRoomAsync(s.AccessToken, new(model.Code, model.Name, model.Location), ct);
        if (!r.Succeeded) { if (Editable(r.Problem!)) { Apply(r.Problem!); return View(model); } return Failure(r.Problem!); }
        TempData["SuccessMessage"] = "Đã tạo phòng khám mới."; return RedirectToAction(nameof(Details), new { id = r.Value!.Id });
    }
    [HttpGet("Edit/{id:guid}")]
    public async Task<IActionResult> Edit(Guid id, CancellationToken ct) { var r = await Get(id, ct); return r.Result ?? View(new RoomFormViewModel { Id = id, Code = r.Value!.Code, Name = r.Value.Name, Location = r.Value.Location }); }
    [HttpPost("Edit/{id:guid}"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, RoomFormViewModel model, CancellationToken ct)
    {
        if (id != model.Id) return BadRequest(); if (!ModelState.IsValid) return View(model); AuthenticationSession? s = await Session(ct); if (s is null) return Expired();
        var r = await api.UpdateRoomAsync(s.AccessToken, id, new(model.Code, model.Name, model.Location), ct);
        if (!r.Succeeded) { if (Editable(r.Problem!)) { Apply(r.Problem!); return View(model); } return Failure(r.Problem!); }
        TempData["SuccessMessage"] = "Đã cập nhật phòng khám."; return RedirectToAction(nameof(Details), new { id });
    }
    [HttpPost("{id:guid}/activate"), ValidateAntiForgeryToken] public Task<IActionResult> Activate(Guid id, CancellationToken ct) => Command(id, true, ct);
    [HttpPost("{id:guid}/deactivate"), ValidateAntiForgeryToken] public Task<IActionResult> Deactivate(Guid id, CancellationToken ct) => Command(id, false, ct);
    private async Task<IActionResult> Command(Guid id, bool active, CancellationToken ct)
    { AuthenticationSession? s = await Session(ct); if (s is null) return Expired(); var r = await api.SetRoomActiveAsync(s.AccessToken, id, active, ct); if (!r.Succeeded) return Failure(r.Problem!); TempData["SuccessMessage"] = active ? "Đã kích hoạt lại phòng khám." : "Đã ngừng hoạt động phòng khám."; return RedirectToAction(nameof(Details), new { id }); }
    private async Task<(RoomDetailsResponse? Value, IActionResult? Result)> Get(Guid id, CancellationToken ct)
    { AuthenticationSession? s = await Session(ct); if (s is null) return (null, Expired()); var r = await api.GetRoomAsync(s.AccessToken, id, ct); return r.Succeeded ? (r.Value, null) : (null, Failure(r.Problem!)); }
    private Task<AuthenticationSession?> Session(CancellationToken ct) => authentication.GetCurrentSessionAsync(HttpContext, ct);
    private IActionResult Expired() { TempData["WarningMessage"] = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại."; return RedirectToAction("Login", "Account"); }
    private IActionResult Failure(ApiProblem p)
    { if (p.StatusCode == HttpStatusCode.Unauthorized) return Expired(); if (p.StatusCode == HttpStatusCode.Forbidden) return RedirectToAction("AccessDenied", "Account"); int status = (int)(p.StatusCode ?? HttpStatusCode.ServiceUnavailable); Response.StatusCode = status; return View("~/Views/Shared/CatalogError.cshtml", new CatalogErrorViewModel(p.Title, p.Detail, status, "Rooms")); }
    private static bool Editable(ApiProblem p) => p.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Conflict;
    private void Apply(ApiProblem p) { foreach (var pair in p.ValidationErrors) foreach (string message in pair.Value) ModelState.AddModelError(pair.Key, message); if (p.ValidationErrors.Count == 0) ModelState.AddModelError(string.Empty, p.Detail); }
}

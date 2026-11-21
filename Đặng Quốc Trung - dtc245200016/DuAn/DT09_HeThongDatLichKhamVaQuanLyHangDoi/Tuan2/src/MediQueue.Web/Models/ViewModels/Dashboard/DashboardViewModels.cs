using MediQueue.Contracts.Dashboard;

namespace MediQueue.Web.Models.ViewModels.Dashboard;

public abstract record DashboardPageViewModel(DashboardResponse Dashboard);

public sealed record AdminDashboardViewModel(DashboardResponse Dashboard)
    : DashboardPageViewModel(Dashboard);

public sealed record DoctorDashboardViewModel(DashboardResponse Dashboard)
    : DashboardPageViewModel(Dashboard);

public sealed record ReceptionistDashboardViewModel(DashboardResponse Dashboard)
    : DashboardPageViewModel(Dashboard);

public sealed record PatientDashboardViewModel(DashboardResponse Dashboard)
    : DashboardPageViewModel(Dashboard);

public sealed record StatisticCardViewModel(
    string Label,
    string Value,
    string Description,
    string Icon,
    string Tone = "primary");

public sealed record QuickActionViewModel(
    string Label,
    string Description,
    string Icon,
    string Controller,
    string Action);

public sealed record DashboardStateViewModel(
    string Title,
    string Description,
    string Icon,
    string Tone = "neutral",
    bool CanRetry = false);

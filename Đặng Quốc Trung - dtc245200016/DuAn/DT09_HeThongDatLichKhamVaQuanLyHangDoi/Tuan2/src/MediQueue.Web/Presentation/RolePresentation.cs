namespace MediQueue.Web.Presentation;

public static class RolePresentation
{
    public static string GetLabel(string? role) => role switch
    {
        "Admin" => "Quản trị viên",
        "Doctor" => "Bác sĩ",
        "Receptionist" => "Lễ tân",
        "Patient" => "Bệnh nhân",
        _ => "Tài khoản"
    };

    public static string GetAccentClass(string? role) => role switch
    {
        "Admin" => "admin",
        "Doctor" => "doctor",
        "Receptionist" => "receptionist",
        "Patient" => "patient",
        _ => "default"
    };

    public static string GetWorkspaceLabel(string? role) => role switch
    {
        "Admin" => "Quản lý hệ thống",
        "Doctor" => "Không gian bác sĩ",
        "Receptionist" => "Không gian lễ tân",
        "Patient" => "Không gian bệnh nhân",
        _ => "Không gian làm việc"
    };
}

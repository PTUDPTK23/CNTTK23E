using Microsoft.EntityFrameworkCore;

namespace ChatSupport.Backend;

public sealed class ChatSupportDbContext(DbContextOptions<ChatSupportDbContext> options) : DbContext(options)
{
    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<StaffMember> StaffMembers => Set<StaffMember>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<ChatMessage> Messages => Set<ChatMessage>();
    public DbSet<SystemSettings> SystemSettings => Set<SystemSettings>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AdminUser>(entity =>
        {
            entity.ToTable("admin_users");
            entity.HasKey(user => user.Id);
            entity.Property(user => user.Id).HasColumnName("id");
            entity.Property(user => user.DisplayName).HasColumnName("display_name");
            entity.Property(user => user.Username).HasColumnName("username");
            entity.Property(user => user.Role).HasColumnName("role");
            entity.Property(user => user.PasswordSalt).HasColumnName("password_salt");
            entity.Property(user => user.PasswordHash).HasColumnName("password_hash");
            entity.HasIndex(user => user.Username).IsUnique();
        });
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.ToTable("customers");
            entity.HasKey(customer => customer.Id);
            entity.Property(customer => customer.Id).HasColumnName("id");
            entity.Property(customer => customer.FullName).HasColumnName("full_name");
            entity.Property(customer => customer.Email).HasColumnName("email");
            entity.Property(customer => customer.Phone).HasColumnName("phone");
            entity.Property(customer => customer.CreatedAt).HasColumnName("created_at");
            entity.Property(customer => customer.IsActive).HasColumnName("is_active");
            entity.HasIndex(customer => customer.Email).IsUnique();
        });
        modelBuilder.Entity<StaffMember>(entity =>
        {
            entity.ToTable("staff");
            entity.HasKey(staff => staff.Id);
            entity.Property(staff => staff.Id).HasColumnName("id");
            entity.Property(staff => staff.FullName).HasColumnName("full_name");
            entity.Property(staff => staff.Email).HasColumnName("email");
            entity.Property(staff => staff.Role).HasColumnName("role");
            entity.Property(staff => staff.Status).HasColumnName("status");
            entity.HasIndex(staff => staff.Email).IsUnique();
        });
        modelBuilder.Entity<Conversation>(entity =>
        {
            entity.ToTable("conversations");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Id).HasColumnName("id");
            entity.Property(item => item.CustomerName).HasColumnName("customer_name");
            entity.Property(item => item.AssignedStaffId).HasColumnName("assigned_staff_id");
            entity.Property(item => item.Status).HasColumnName("status");
            entity.Property(item => item.UpdatedAt).HasColumnName("updated_at");
        });
        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.ToTable("messages");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Id).HasColumnName("id");
            entity.Property(item => item.ConversationId).HasColumnName("conversation_id");
            entity.Property(item => item.Sender).HasColumnName("sender");
            entity.Property(item => item.SenderId).HasColumnName("sender_id");
            entity.Property(item => item.Text).HasColumnName("text");
            entity.Property(item => item.SentAt).HasColumnName("sent_at");
        });
        modelBuilder.Entity<SystemSettings>(entity =>
        {
            entity.ToTable("system_settings");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Id).HasColumnName("id");
            entity.Property(item => item.SystemName).HasColumnName("system_name");
            entity.Property(item => item.SupportEmail).HasColumnName("support_email");
            entity.Property(item => item.AutoReplyEnabled).HasColumnName("auto_reply_enabled");
            entity.Property(item => item.WelcomeMessage).HasColumnName("welcome_message");
        });
        base.OnModelCreating(modelBuilder);
    }
}

public sealed class AdminUser
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = "Admin";
    public string PasswordSalt { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
}

public sealed class Customer
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class StaffMember
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = "Nhân viên hỗ trợ";
    public string Status { get; set; } = "Đang hoạt động";
}

public sealed class Conversation
{
    public Guid Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public Guid? AssignedStaffId { get; set; }
    public string Status { get; set; } = "Đang xử lý";
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class ChatMessage
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public string Sender { get; set; } = string.Empty;
    public Guid? SenderId { get; set; }
    public string Text { get; set; } = string.Empty;
    public DateTimeOffset SentAt { get; set; }
}

public sealed class SystemSettings
{
    public int Id { get; set; } = 1;
    public string SystemName { get; set; } = "Hệ thống chat hỗ trợ trực tuyến";
    public string SupportEmail { get; set; } = "hotro@congty.vn";
    public bool AutoReplyEnabled { get; set; } = true;
    public string WelcomeMessage { get; set; } = "Xin chào! Chúng tôi có thể hỗ trợ bạn như thế nào?";
}

public sealed record StaffPerformance(Guid StaffId, string FullName, string Role, int ConversationCount, int MessageCount, string Status);

public sealed record AdminSummary(int CustomerCount, int StaffCount, int ActiveStaffCount, int OpenConversationCount, int MessageCount);
public sealed record CustomerInput(string FullName, string Email, string? Phone, bool IsActive = true);
public sealed record StaffInput(string FullName, string Email, string Role, string Status = "Đang hoạt động");

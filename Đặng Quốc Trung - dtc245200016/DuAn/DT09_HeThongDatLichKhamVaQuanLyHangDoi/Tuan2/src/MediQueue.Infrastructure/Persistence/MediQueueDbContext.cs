using MediQueue.Domain.Appointments;
using MediQueue.Domain.Departments;
using MediQueue.Domain.Doctors;
using MediQueue.Domain.Patients;
using MediQueue.Domain.Queues;
using MediQueue.Domain.Rooms;
using MediQueue.Domain.Scheduling;
using MediQueue.Infrastructure.Identity;
using MediQueue.Infrastructure.Persistence.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace MediQueue.Infrastructure.Persistence;

public sealed class MediQueueDbContext(
    DbContextOptions<MediQueueDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Patient> Patients => Set<Patient>();

    public DbSet<Doctor> Doctors => Set<Doctor>();

    public DbSet<Department> Departments => Set<Department>();

    public DbSet<Room> Rooms => Set<Room>();

    public DbSet<DoctorSchedule> DoctorSchedules => Set<DoctorSchedule>();

    public DbSet<TimeSlot> TimeSlots => Set<TimeSlot>();

    public DbSet<Appointment> Appointments => Set<Appointment>();

    public DbSet<DailyQueue> DailyQueues => Set<DailyQueue>();

    public DbSet<QueueTicket> QueueTickets => Set<QueueTicket>();

    public DbSet<QueueTransition> QueueTransitions => Set<QueueTransition>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

    public DbSet<IdempotencyRequest> IdempotencyRequests => Set<IdempotencyRequest>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(MediQueueDbContext).Assembly);
    }
}

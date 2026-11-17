using Microsoft.EntityFrameworkCore;

namespace ChatSupport.Backend;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(ChatSupportDbContext db, string contentRoot)
    {
        string[] schema =
        [
            "CREATE TABLE IF NOT EXISTS admin_users (id uuid PRIMARY KEY, display_name text NOT NULL, username text NOT NULL UNIQUE, role text NOT NULL, password_salt text NOT NULL, password_hash text NOT NULL)",
            "CREATE TABLE IF NOT EXISTS customers (id uuid PRIMARY KEY, full_name text NOT NULL, email text NOT NULL UNIQUE, phone text NOT NULL DEFAULT '', created_at timestamp with time zone NOT NULL, is_active boolean NOT NULL DEFAULT TRUE)",
            "CREATE TABLE IF NOT EXISTS staff (id uuid PRIMARY KEY, full_name text NOT NULL, email text NOT NULL UNIQUE, role text NOT NULL, status text NOT NULL)",
            "CREATE TABLE IF NOT EXISTS conversations (id uuid PRIMARY KEY, customer_name text NOT NULL, assigned_staff_id uuid NULL, status text NOT NULL, updated_at timestamp with time zone NOT NULL)",
            "CREATE TABLE IF NOT EXISTS messages (id uuid PRIMARY KEY, conversation_id uuid NOT NULL, sender text NOT NULL, sender_id uuid NULL, text text NOT NULL, sent_at timestamp with time zone NOT NULL)",
            "CREATE TABLE IF NOT EXISTS system_settings (id integer PRIMARY KEY, system_name text NOT NULL, support_email text NOT NULL, auto_reply_enabled boolean NOT NULL, welcome_message text NOT NULL)"
        ];

        foreach (var statement in schema)
            await db.Database.ExecuteSqlRawAsync(statement);

        await AdminAccountService.SeedInitialAdminAsync(db, contentRoot);
        if (!await db.SystemSettings.AnyAsync())
        {
            db.SystemSettings.Add(new SystemSettings());
            await db.SaveChangesAsync();
        }
    }
}

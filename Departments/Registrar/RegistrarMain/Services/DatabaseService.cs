using Dapper;
using MySqlConnector;

namespace RegistrarMain.Services;

/// <summary>
/// Central database access service — equivalent to PHP PDO + config.php helpers.
/// </summary>
public class DatabaseService
{
    private readonly MySqlConnection _db;
    private readonly IHttpContextAccessor _ctx;

    public DatabaseService(MySqlConnection db, IHttpContextAccessor ctx)
    {
        _db = db;
        _ctx = ctx;
    }

    public MySqlConnection Connection => _db;

    // ─── Settings ────────────────────────────────────────────────────────────

    public async Task<string> GetSettingAsync(string key, string defaultValue = "")
    {
        var val = await _db.QueryFirstOrDefaultAsync<string>(
            "SELECT `value` FROM `settings` WHERE `key` = @key", new { key });
        return val ?? defaultValue;
    }

    public async Task SetSettingAsync(string key, string value)
    {
        await _db.ExecuteAsync(
            "INSERT INTO `settings` (`key`, `value`) VALUES (@key, @value) " +
            "ON DUPLICATE KEY UPDATE `value` = VALUES(`value`)",
            new { key, value });
    }

    // ─── Activity Log ────────────────────────────────────────────────────────

    public async Task LogActivityAsync(int? userId, string message)
    {
        await _db.ExecuteAsync(
            "INSERT INTO `activity_log` (`user_id`, `message`, `created_at`) VALUES (@userId, @message, NOW())",
            new { userId, message });
    }

    // ─── Session helpers (mirrors PHP config.php) ────────────────────────────

    private ISession Session => _ctx.HttpContext!.Session;

    public int CurrentUserId
    {
        get => Session.GetInt32("user_id") ?? 1;
        set => Session.SetInt32("user_id", value);
    }

    public string CurrentUserName
    {
        get => Session.GetString("user_name") ?? "Dr. Rosalinda Santos";
        set => Session.SetString("user_name", value);
    }

    public string CurrentUserRole
    {
        get => Session.GetString("user_role") ?? "registrar";
        set => Session.SetString("user_role", value);
    }

    /// <summary>
    /// Ensures a registrar session is active (auto-defaults like PHP config.php).
    /// Call at the top of each registrar PageModel.OnGet*.
    /// </summary>
    public void EnsureRegistrarSession()
    {
        if (Session.GetString("user_role") == null || Session.GetString("user_role") == "student")
        {
            Session.SetInt32("user_id", 1);
            Session.SetString("user_name", "Dr. Rosalinda Santos");
            Session.SetString("user_role", "registrar");
            Session.SetString("user_email", "registrar@lipa.nu.edu.ph");
        }
    }


    public void SwitchToRegistrar()
    {
        Session.SetInt32("user_id", 1);
        Session.SetString("user_name", "Dr. Rosalinda Santos");
        Session.SetString("user_role", "registrar");
        Session.SetString("user_email", "registrar@lipa.nu.edu.ph");
    }
}

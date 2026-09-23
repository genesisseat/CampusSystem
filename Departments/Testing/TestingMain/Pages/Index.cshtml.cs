using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySqlConnector;

namespace TestingMain.Pages;

public class IndexModel : PageModel
{
    private readonly IConfiguration _configuration;

    public IndexModel(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [BindProperty]
    public string Name { get; set; } = string.Empty;

    [BindProperty]
    public string Email { get; set; } = string.Empty;

    [BindProperty]
    public string SelectedDatabase { get; set; } = "mydb";

    [BindProperty]
    public string SelectedTable { get; set; } = "users";

    public List<string> Databases { get; set; } = new();
    public List<string> Tables { get; set; } = new();
    public List<Dictionary<string, object?>> Rows { get; set; } = new();
    public string? Message { get; set; }
    public bool IsSuccess { get; set; }

    public async Task OnGetAsync()
    {
        await LoadDatabaseOptionsAsync();
        await LoadTableDataAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(Name) || string.IsNullOrWhiteSpace(Email))
        {
            Message = "Please fill in all fields.";
            IsSuccess = false;
            await LoadDatabaseOptionsAsync();
            await LoadTableDataAsync();
            return Page();
        }

        var connectionString = _configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            Message = "Connection string 'DefaultConnection' is missing.";
            IsSuccess = false;
            await LoadDatabaseOptionsAsync();
            await LoadTableDataAsync();
            return Page();
        }

        try
        {
            await using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            const string sql = "INSERT INTO users (name, email) VALUES (@name, @email);";

            await using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@name", Name);
            command.Parameters.AddWithValue("@email", Email);

            await command.ExecuteNonQueryAsync();

            Message = $"Successfully saved record for '{Name}' into Debian MariaDB!";
            IsSuccess = true;
            Name = string.Empty;
            Email = string.Empty;
        }
        catch (Exception ex)
        {
            Message = $"Database Error: {ex.Message}";
            IsSuccess = false;
        }

        await LoadDatabaseOptionsAsync();
        await LoadTableDataAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostLoadTableAsync()
    {
        await LoadDatabaseOptionsAsync();
        await LoadTableDataAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostDeleteRowAsync()
    {
        var rowId = Request.Form["rowId"].ToString();
        var db = Request.Form["databaseName"].ToString();
        var table = Request.Form["tableName"].ToString();

        if (string.IsNullOrWhiteSpace(rowId) || string.IsNullOrWhiteSpace(db) || string.IsNullOrWhiteSpace(table))
        {
            Message = "Delete request is missing the required data.";
            IsSuccess = false;
            await LoadDatabaseOptionsAsync();
            await LoadTableDataAsync();
            return Page();
        }

        var connectionString = _configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            Message = "Connection string 'DefaultConnection' is missing.";
            IsSuccess = false;
            await LoadDatabaseOptionsAsync();
            await LoadTableDataAsync();
            return Page();
        }

        try
        {
            var idColumn = await DetectIdColumnAsync(connectionString, db, table);
            if (string.IsNullOrWhiteSpace(idColumn))
            {
                Message = $"No suitable primary key was found on table '{table}'.";
                IsSuccess = false;
                await LoadDatabaseOptionsAsync();
                await LoadTableDataAsync();
                return Page();
            }

            await using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            var sql = $"DELETE FROM `{db}`.`{table}` WHERE `{idColumn}` = @id;";
            await using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@id", rowId);
            await command.ExecuteNonQueryAsync();

            Message = $"Record {rowId} deleted from {db}.{table}.";
            IsSuccess = true;
        }
        catch (Exception ex)
        {
            Message = $"Delete failed: {ex.Message}";
            IsSuccess = false;
        }

        await LoadDatabaseOptionsAsync();
        await LoadTableDataAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostUpdateRowAsync()
    {
        var rowId = Request.Form["rowId"].ToString();
        var db = Request.Form["databaseName"].ToString();
        var table = Request.Form["tableName"].ToString();

        if (string.IsNullOrWhiteSpace(rowId) || string.IsNullOrWhiteSpace(db) || string.IsNullOrWhiteSpace(table))
        {
            Message = "Update request is missing the required data.";
            IsSuccess = false;
            await LoadDatabaseOptionsAsync();
            await LoadTableDataAsync();
            return Page();
        }

        var connectionString = _configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            Message = "Connection string 'DefaultConnection' is missing.";
            IsSuccess = false;
            await LoadDatabaseOptionsAsync();
            await LoadTableDataAsync();
            return Page();
        }

        try
        {
            var idColumn = await DetectIdColumnAsync(connectionString, db, table);
            if (string.IsNullOrWhiteSpace(idColumn))
            {
                Message = $"No suitable primary key was found on table '{table}'.";
                IsSuccess = false;
                await LoadDatabaseOptionsAsync();
                await LoadTableDataAsync();
                return Page();
            }

            var fieldUpdates = new List<string>();
            var parameters = new List<MySqlParameter>();

            foreach (var key in Request.Form.Keys.Where(k => k != "rowId" && k != "databaseName" && k != "tableName" && !string.IsNullOrWhiteSpace(k)))
            {
                var columnName = key;
                var value = Request.Form[key].ToString();

                if (columnName.Equals(idColumn, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                fieldUpdates.Add($"`{columnName}` = @p_{columnName}");
                parameters.Add(new MySqlParameter($"@p_{columnName}", value));
            }

            if (fieldUpdates.Count == 0)
            {
                Message = "No editable fields were provided for the update.";
                IsSuccess = false;
                await LoadDatabaseOptionsAsync();
                await LoadTableDataAsync();
                return Page();
            }

            var sql = $"UPDATE `{db}`.`{table}` SET {string.Join(", ", fieldUpdates)} WHERE `{idColumn}` = @rowId;";
            await using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            await using var command = new MySqlCommand(sql, connection);
            foreach (var parameter in parameters)
            {
                command.Parameters.Add(parameter);
            }

            command.Parameters.AddWithValue("@rowId", rowId);
            await command.ExecuteNonQueryAsync();

            Message = $"Record {rowId} updated in {db}.{table}.";
            IsSuccess = true;
        }
        catch (Exception ex)
        {
            Message = $"Update failed: {ex.Message}";
            IsSuccess = false;
        }

        await LoadDatabaseOptionsAsync();
        await LoadTableDataAsync();
        return Page();
    }

    private async Task LoadDatabaseOptionsAsync()
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        try
        {
            await using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            const string sql = "SHOW DATABASES;";
            await using var command = new MySqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync();

            var values = new List<string>();
            while (await reader.ReadAsync())
            {
                values.Add(reader.GetString(0));
            }

            Databases = values;
            if (string.IsNullOrWhiteSpace(SelectedDatabase) && Databases.Count > 0)
            {
                SelectedDatabase = Databases[0];
            }
        }
        catch
        {
            Databases = new List<string>();
        }
    }

    private async Task LoadTableDataAsync()
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        try
        {
            await using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            var dbName = string.IsNullOrWhiteSpace(SelectedDatabase) ? "mydb" : SelectedDatabase;
            var tableName = string.IsNullOrWhiteSpace(SelectedTable) ? "users" : SelectedTable;

            var tableSql = $"SHOW TABLES FROM `{dbName}`;";
            await using (var tableCommand = new MySqlCommand(tableSql, connection))
            {
                await using var tableReader = await tableCommand.ExecuteReaderAsync();
                var names = new List<string>();
                while (await tableReader.ReadAsync())
                {
                    names.Add(tableReader.GetString(0));
                }

                Tables = names;
                if (string.IsNullOrWhiteSpace(SelectedTable) && Tables.Count > 0)
                {
                    SelectedTable = Tables[0];
                }
            }

            if (Tables.Contains(tableName))
            {
                var query = $"SELECT * FROM `{dbName}`.`{tableName}`;";
                await using var command = new MySqlCommand(query, connection);
                await using var reader = await command.ExecuteReaderAsync();
                var schema = reader.GetColumnSchema();

                Rows = new List<Dictionary<string, object?>>();
                while (await reader.ReadAsync())
                {
                    var row = new Dictionary<string, object?>();
                    foreach (var column in schema)
                    {
                        var key = column.ColumnName ?? string.Empty;
                        row[key] = reader[key];
                    }

                    Rows.Add(row);
                }
            }
        }
        catch
        {
            Tables = new List<string>();
            Rows = new List<Dictionary<string, object?>>();
        }
    }

    private async Task<string> DetectIdColumnAsync(string connectionString, string db, string table)
    {
        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();

        var sql = $"SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @db AND TABLE_NAME = @table AND COLUMN_KEY = 'PRI' LIMIT 1;";
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@db", db);
        command.Parameters.AddWithValue("@table", table);

        var result = await command.ExecuteScalarAsync();
        return result?.ToString() ?? string.Empty;
    }
}

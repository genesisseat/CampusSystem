using Dapper;
using MySqlConnector;

namespace RegistrarMain.Services;

public sealed class RegistrarApiSchemaService(MySqlConnection db, ILogger<RegistrarApiSchemaService> logger)
{
    public async Task EnsureSchemaAsync(CancellationToken cancellationToken = default)
    {
        await db.OpenAsync(cancellationToken);
        await db.ExecuteAsync(new CommandDefinition(@"
            CREATE TABLE IF NOT EXISTS document_requests (
                id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
                student_id INT NOT NULL,
                document_type VARCHAR(100) NOT NULL,
                purpose TEXT NOT NULL,
                copies INT NOT NULL DEFAULT 1,
                status VARCHAR(50) NOT NULL DEFAULT 'pending',
                qr_code_token VARCHAR(191) NULL,
                remarks TEXT NULL,
                requested_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                processed_at DATETIME NULL,
                released_at DATETIME NULL,
                INDEX idx_document_requests_student_time (student_id, requested_at),
                INDEX idx_document_requests_type_status (document_type, status)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4",
            cancellationToken: cancellationToken));

        logger.LogInformation("Registrar shared MySQL request schema verified.");
    }
}
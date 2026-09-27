using Microsoft.AspNetCore.Mvc.RazorPages;
using Dapper;
using RegistrarMain.Models;
using RegistrarMain.Services;

namespace RegistrarMain.Pages;

public class IntegrationHubModel : PageModel
{
    private readonly DatabaseService _db;

    public IntegrationHubModel(DatabaseService db)
    {
        _db = db;
    }

    public List<ApiTokenItem> Tokens { get; set; } = new();
    public List<StudentDropdownItem> Students { get; set; } = new();
    public List<OfferingDropdownItem> Offerings { get; set; } = new();

    public async Task OnGetAsync()
    {
        _db.EnsureRegistrarSession();

        var conn = _db.Connection;
        Tokens = (await conn.QueryAsync<ApiTokenItem>(
            "SELECT id, system_name as SystemName, token as Token, permissions as Permissions, is_active as IsActive FROM api_tokens ORDER BY id ASC"
        )).AsList();

        Students = (await conn.QueryAsync<StudentDropdownItem>(
            "SELECT id as Id, student_id_number as StudentIdNumber, name as Name FROM `user` WHERE role = 'student' ORDER BY name ASC LIMIT 10"
        )).AsList();

        Offerings = (await conn.QueryAsync<OfferingDropdownItem>(
            "SELECT id as Id, section_code as SectionCode, subject_id as SubjectId FROM class_offerings LIMIT 10"
        )).AsList();
    }

    public class ApiTokenItem
    {
        public int Id { get; set; }
        public string SystemName { get; set; } = "";
        public string Token { get; set; } = "";
        public string Permissions { get; set; } = "";
        public bool IsActive { get; set; }
    }

    public class StudentDropdownItem
    {
        public int Id { get; set; }
        public string StudentIdNumber { get; set; } = "";
        public string Name { get; set; } = "";
    }

    public class OfferingDropdownItem
    {
        public int Id { get; set; }
        public string SectionCode { get; set; } = "";
        public int SubjectId { get; set; }
    }
}

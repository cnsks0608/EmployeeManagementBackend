namespace EmployeeManagement.Api.Models
{
    public class ActivityLog
    {
        public int Id { get; set; }
        public string? Username { get; set; }         // işlemi yapan kişi, başarısız login'de null olabilir
        public string? TargetName { get; set; }         // işlemden etkilenen kişi, varolmayan biir kullanıcıyla login olmaya çalışırken null olabilir 
        public string Action { get; set; } = string.Empty;       // Create, Read, Update, Delete, Reactivate, Login, Logout
        public string Description { get; set; } = string.Empty;  // anlaşılır bir dille açıklama
        public bool IsSuccess { get; set; }
        public string? FailureReason { get; set; }       // sadece başarısızsa dolu
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
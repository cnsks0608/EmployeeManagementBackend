namespace EmployeeManagement.Api.Models
{
    public class RequestLog
    {
        public int Id { get; set; }
        public string HttpMethod { get; set; } = string.Empty;   
        public string Path { get; set; } = string.Empty;          
        public int StatusCode { get; set; }                       
        public string? Username { get; set; } // login olmamışsa boş olabilir, kullanıcı username veya email ile login olduktan sonra username i tokena yazılır, buradaki username de tokendan okunur (yani sen mail adresinle bile login olsan loglarında username in yazar)
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; 
    }
}
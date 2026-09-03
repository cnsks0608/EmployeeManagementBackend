namespace EmployeeManagement.Api.Models
{
    public class RequestLog
    {
        public int Id { get; set; }
        public string HttpMethod { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string? QueryString { get; set; }   // filtreler, id'ler vs. (örn. "?search=ali&pageNumber=2")
        public string? RequestBody { get; set; }   // gönderilen DTO içeriği, şifre alanları maskeli       
        public int StatusCode { get; set; }
        public string? Username { get; set; } // login olmamışsa boş olabilir, kullanıcı username veya email ile login olduktan sonra username i tokena yazılır, buradaki username de tokendan okunur (yani sen mail adresinle bile login olsan loglarında username in yazar)
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
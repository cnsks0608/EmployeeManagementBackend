namespace EmployeeManagement.Api.DTOs
{
    public class PagedResult<T>  // T dememizin sebebi -> bu dto yu employee veya user için kullanabiliriz 
    {
        public List<T> Items { get; set; } = new();  // o sayfadaki gerçek veriler (örn. 10 employee)
        public int PageNumber { get; set; }           // şu an kaçıncı sayfadayız
        public int PageSize { get; set; }              // her sayfada kaç kayıt var
        public int TotalCount { get; set; }            // filtrelere uyan TOPLAM kayıt sayısı (tüm sayfalar dahil)
        public int TotalPages { get; set; }             // toplamda kaç sayfa var
    }
}
using EmployeeManagement.Api.Enums;

namespace EmployeeManagement.Api.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;

        public int RoleId { get; set; }  // burada da role tablosu ile user tablosunu bağlamış olduk 
        public Role Role { get; set; } = null!;  // Navigation property - RoleId'ye bakıp Roles tablosundan otomatik olarak o role'ün bilgilerini getirir (her userın role ü olması gerektiği için nonnullable)

        public int EmployeeId { get; set; }  // user tablosuna, employee tablosundaki id bilgisini koyarak bağlama işlemini yapıyoruz (her user kaydının employees tablosunda bir karşılığı olması gerektiği için nullable yapmadık, eğer employee tablosuna userid ekleyerek bağlasaydık nullable yapmamız gerekirdi çünkü her employeenin sisteme kayıtlı olma zorunluluğu yok) 
        public Employee Employee { get; set; } = null!;  // bu da bir navigation kısayolu, normalde tabloya yalnızca employeeid si ekleseydik o id ye bakıp sonrasında employee tablosundan o id ye sahip employeenin bilgilerini manuel çekmemiz gerekecekti. ancak bunun sayesinde employeeid ile employees tablosundaki o id ye sahip kullanıcının bilgilerini otomatik getirir / tekrardan her user employee olmak zorunda olduğu için nonnullable yaptık -> burada nonnullable yaptığımız için eğer employee tablosundan bir kayıt silersek postgresql otomatik olarak o employeenin user tablosunda karşılık geldiği user kaydını da siliyor 
    }
}
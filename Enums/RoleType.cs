namespace EmployeeManagement.Api.Enums
{
    public enum RoleType
    {
        Admin = 1,
        User = 2
    }
}

// enumlar sadece c# kodunun içinde yaşarlar (backennde yani). o yüzden role tablosunda ayrıca rolename adında bir alanımız var (ilerde frontende göndermek için)

// ============================================================
// ROLE / USER MİMARİSİ — NEDEN BÖYLE KURULDU?
// ============================================================
//
// SORU 1: Neden sadece bir "enum" yetmiyor, ayrıca bir "Roles" 
// tablosuna da ihtiyacımız var?
//
// Enum, kodun içinde yaşayan, SABİT bir listedir (Admin=1, User=2 gibi).
// Roller tablosu ise, veritabanında yaşayan GERÇEK bir veridir.
//
// Bir kullanıcının rolünü (RoleId) sadece enum ile tutsaydık, bu bilgi
// SADECE kodun içinde, geliştiricinin kafasında anlamlı olurdu.
// Veritabanına (pgAdmin/SSMS ile) bakan biri, "RoleId=1 olan kullanıcı
// gerçekten Admin mi?" sorusuna cevap bulamazdı — kodu açıp bakması
// gerekirdi.
//
// Roles tablosu bu belirsizliği ortadan kaldırıyor: Veritabanına
// bakan HERKES (başka bir geliştirici, veritabanı yöneticisi, ya da
// 6 ay sonraki ben), "Id=1 -> Admin, Id=2 -> User" bilgisini DOĞRUDAN,
// kod okumadan görebiliyor. Yani veritabanı, tek bir "doğruluk kaynağı"
// (single source of truth) haline geliyor.
//
// ------------------------------------------------------------
//
// SORU 2: Madem tablo var, enum'a hâlâ neden ihtiyacımız var?
//
// Enum, KOD YAZARKEN hata yapmamızı önlüyor. Enum olmadan şöyle
// yazardık: if (user.RoleId == 1) { ... } — bu "1" sayısının Admin mi
// User mı olduğunu HER SEFERİNDE hatırlamamız gerekirdi, bu da hataya
// çok açık bir durum (yanlışlıkla == 2 yazabiliriz, farkında bile olmayız).
//
// Enum ile: if (user.Role == Role.Admin) { ... } — "Admin" kelimesini
// gördüğümüz an ne olduğu anında belli oluyor, kod kendi kendini
// açıklıyor (self-documenting code).
//
// ------------------------------------------------------------
//
// SORU 3: Yeni bir rol (mesela "Manager") eklersem, hem tabloya
// hem enum'a eklemem gerekmez mi? O zaman yine kod değişikliği
// kaçınılmaz değil mi?
//
// Cevap, YENİ ROLÜN NE İÇİN kullanılacağına bağlı:
//
// - Eğer yeni role sadece "var olsun, sisteme kayıtlı dursun" 
//   istiyorsak (henüz ona özel bir davranış/yetki yazmadan),
//   SADECE tabloya bir satır eklemek yeterli, kod değişikliği
//   GEREKMEZ.
//
// - Eğer yeni role ÖZEL bir davranış/yetki tanımlamak istiyorsak
//   (mesela "Manager'lar raporları görsün ama silemesin" gibi),
//   o zaman evet, hem tabloya hem enum'a ekleme YAPMAMIZ GEREKİR,
//   çünkü "davranış" dediğimiz şey zaten kodun kendisidir — bu,
//   tablo olsa da olmasa da değişmeyen bir gerçektir.
//
// Yani tablo, "kod hiç değişmeyecek" demek değil; tablo sayesinde
// en azından ROLÜN VARLIĞINI kaydetmek, kod değiştirmeden mümkün oluyor.
//
// ------------------------------------------------------------
//
// SORU 4: İki tablo (Users ve Roles) birbirine, enum üzerinden mi
// bağlanıyor?
//
// HAYIR. Enum, VERİTABANINA HİÇ GİRMİYOR, sadece C# kodunun içinde
// yaşıyor. Gerçek bağlantı, iki tablo arasında, SAYI (Id) üzerinden
// kuruluyor (foreign key):
//
//   Roles tablosu:        Users tablosu:
//   Id | Name              Id | Username | RoleId
//   1  | Admin              45  | ahmet    | 1   <- Roles.Id=1'e işaret eder
//   2  | User                54  | ayse     | 2   <- Roles.Id=2'ye işaret eder
//
// Enum, sadece BİZİM (kod yazarken) bu sayıları "Admin", "User" gibi
// okunabilir isimlerle görebilmemiz için var. Kod, RoleId=1'i okuyup
// bunu (Role)1 şeklinde enum'a çevirdiğinde, bu bize Role.Admin olarak
// görünüyor — ama veritabanının kendisi, bu enum'dan tamamen habersiz,
// sadece sayı (1, 2, ...) tutuyor.
// ============================================================
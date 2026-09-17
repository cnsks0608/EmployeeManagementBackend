using Microsoft.EntityFrameworkCore;
using EmployeeManagement.Api.Data;
using EmployeeManagement.Api.DTOs.ChatDtos;
using EmployeeManagement.Api.Models;

namespace EmployeeManagement.Api.Services.ChatServices
{
    public class ChatService : IChatService
    {
        private readonly AppDbContext _context;

        // Anlam taşımayan, her cümlede sık geçen bağlaç/edat/soru kelimeleri.
        // static readonly yaptık çünkü bu liste hiç değişmiyor, her istekte
        // yeniden oluşturmaya gerek yok - uygulama ayağa kalkarken bir kez oluşur.
        private static readonly HashSet<string> StopWords = new()
        {
            "ve", "veya", "ile", "ama", "fakat", "lakin", "ancak", "çünkü", "de", "da",
            "ki", "mi", "mı", "mu", "mü", "için", "gibi", "kadar", "göre", "diye",
            "nasıl", "ne", "neden", "niçin", "niye", "hangi", "kim", "kime", "kimi",
            "ben", "sen", "o", "biz", "siz", "onlar", "bu", "şu", "bir", "her",
            "değil", "olan", "olarak", "ise", "ya", "yada"
        };

        public ChatService(AppDbContext context)
        {
            _context = context;
        }

        // Bir metni alıp, önce noktalama işaretlerini temizleyen, sonra küçük harfe
        // çevirip kelimelere ayıran ve stop words listesindeki anlamsız kelimeleri
        // eleyen yardımcı metod.
       
        private static string[] GetMeaningfulWords(string text)
        {
            //  (? ! . , ; : gibi işaretleri eler), sonunda geriye kalan karakterlerden yeni bir metin oluşturur
            var cleanedText = new string(text.Where(c => !char.IsPunctuation(c)).ToArray());

            return cleanedText
                .ToLower() // büyük/küçük harf farkını yok sayıyoruz (Çalışan = çalışan)
                .Split(' ', StringSplitOptions.RemoveEmptyEntries) // boşluklara göre kelimelere ayır, çift boşluktan boş kelime oluşmasın
                .Where(word => !StopWords.Contains(word)) // stop words listesinde OLMAYAN kelimeleri tut
                .ToArray(); // sonucu bir diziye çevir
        }

        public async Task<ResponseDto> GetAnswerAsync(RequestDto requestDto, string? roleName)
        {
            // Veritabanından, kullanıcının rolüne uygun (ya herkese açık ya da
            // tam kendi rolüne özel) tüm soru-cevap satırlarını çekiyoruz.
            // Include(c => c.Role) ile her satırın bağlı olduğu Role bilgisini
            // de aynı sorguda getiriyoruz, böylece Role.RoleName'e erişebiliyoruz.
            var chats = await _context.Chats
                .Include(c => c.Role)
                .Where(c => c.RoleId == null || c.Role!.RoleName == roleName)
                .ToListAsync();

            
            var userWords = GetMeaningfulWords(requestDto.Question);

            
            Chat? bestMatch = null;
            double bestRatio = 0;

            foreach (var chat in chats)
            {
                var chatWords = GetMeaningfulWords(chat.Question);

                // Eğer stop words ve noktalama temizlendikten sonra o satırda hiç
                // kelime kalmadıysa bu satırı atlıyoruz.
                // Atlamazsak birazdan yapacağımız bölme işlemi (0'a bölme) hata verir.
                if (chatWords.Length == 0) continue;

                // Kullanıcının kelimeleri ile bu satırın kelimeleri arasındaki
                // ORTAK kelime sayısını buluyoruz (Intersect = kesişim).
                int matchCount = userWords.Intersect(chatWords).Count();

                // Oranı hesaplıyoruz: veritabanındaki sorunun kelimelerinin
                // yüzde kaçı kullanıcının cümlesinde de geçiyor?
                // (double) ile baştan tip dönüşümü yapıyoruz, yoksa C# tam sayı
                // bölmesi yapıp ondalık kısmı atar (örneğin 1/4 = 0 çıkardı).
                double ratio = (double)matchCount / chatWords.Length;

                // Eğer oran yarıdan KESİNLİKLE fazlaysa (tam yarısı yetmiyor)
                // VE şu ana kadar bulduğumuz en iyi orandan daha yüksekse,
                // bu satırı yeni "en iyi eşleşme" olarak kaydediyoruz.
                if (ratio > 0.5 && ratio > bestRatio)
                {
                    bestRatio = ratio;
                    bestMatch = chat;
                }
            }


            if (bestMatch == null)
            {
                return new ResponseDto { Answer = "Üzgünüm, bu konuda bilgim yok. Başka bir şekilde sorabilir misiniz?" };
            }
            
            return new ResponseDto { Answer = bestMatch.Answer };
        }
    }
}
using AutoMapper;
using EmployeeManagement.Api.Models;
using EmployeeManagement.Api.DTOs.EmployeeDtos;
using EmployeeManagement.Api.DTOs.UserDtos;

namespace EmployeeManagement.Api.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Employee, EmployeeUserDto>();  // zaten alanların isimleri aynı olduğu için otomatik maplendi 
            CreateMap<Employee, EmployeeAdminDto>();   // model -> dto
            CreateMap<CreateEmployeeByAdminDto, Employee>();  // dto -> model
            CreateMap<User, UserAdminDto>();

            // EmployeeAdminDto, EmployeeUserDto'dan miras aldığı için 
            // bu mapping'i tanımlamadan _mapper.Map<EmployeeUserDto>(employeeAdminDtoNesnesi) çağırınca
            // AutoMapper "zaten hedef tipe uyuyor, yeni bir dönüşüme gerek yok" diyip nesneyi
            // OLDUĞU GİBİ (Salary dahil) geri döndürüyordu. Bu yüzden GetEmployeeById'de, User rolündeki
            // biri başkasının kaydına baktığında, Salary gizlenmesi gerekirken hâlâ görünüyordu.
            // Bu satır, AutoMapper'a "hayır, gerçekten YENİ bir EmployeeUserDto nesnesi oluştur,
            // sadece ortak alanları kopyala, Salary'yi alma" diyerek sorunu çözüyor.
            CreateMap<EmployeeAdminDto, EmployeeUserDto>();
        }
    }
}
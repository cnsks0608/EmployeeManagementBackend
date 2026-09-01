using AutoMapper;
using EmployeeManagement.Api.Models;
using EmployeeManagement.Api.DTOs.EmployeeDtos;
using EmployeeManagement.Api.DTOs.UserDtos;
using EmployeeManagement.Api.DTOs.CompanyDtos;
using EmployeeManagement.Api.DTOs.LogDtos;

namespace EmployeeManagement.Api.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // CreateMap<Employee, EmployeeUserDto>();  // zaten alanların isimleri aynı olduğu için otomatik maplendi (title departman eklemeden önceydi)
            CreateMap<Employee, EmployeeUserDto>()
                .ForMember(dest => dest.TitleName, opt => opt.MapFrom(src => src.Title.Name))
                .ForMember(dest => dest.DepartmentName, opt => opt.MapFrom(src => src.Title.Department.Name));
            // TitleName ve DepartmentName, Employee modelinde doğrudan yok, Title (ve Title.Department)
            // navigation property'leri üzerinden geldiği için AutoMapper'a ayrıca eşleştirme kuralı yazıyoruz


            CreateMap<Employee, EmployeeAdminDto>()
                .ForMember(dest => dest.TitleName, opt => opt.MapFrom(src => src.Title.Name))
                .ForMember(dest => dest.DepartmentName, opt => opt.MapFrom(src => src.Title.Department.Name));

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

            CreateMap<Department, DepartmentDto>();
            CreateMap<Title, TitleDto>();

            CreateMap<RequestLog, RequestLogDto>();
        }
    }
}
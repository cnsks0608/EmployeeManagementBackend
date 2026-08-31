using EmployeeManagement.Api.Enums;

namespace EmployeeManagement.Api.DTOs.EmployeeDtos
{
    public class EmployeeAdminDto : EmployeeUserDto  // EmployeeUser ile salary alanı dışında hepsi ortak o yüzden employeeuserdto dan miras alıp salary kısmını ekledik sadece 
    {
        public decimal Salary { get; set; }
        public RowStatus RowStatus {get; set;}
    }
}
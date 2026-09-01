using EmployeeManagement.Api.Enums;


namespace EmployeeManagement.Api.Models
{
    public class Employee
    {
        public int Id { get; set; }
        public string RegistrationNumber { get; set; } = string.Empty; // Sicil No
        public string FirstName { get; set; } = string.Empty;  // null olmadığını güvence altına almak için        
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;                  
        public decimal Salary { get; set; }                           
        public DateOnly HireDate { get; set; } 

        public RowStatus RowStatus { get; set; } = RowStatus.Created;  // yeni kayıt oluşunca varsayılan olarak Created                        

        public int TitleId { get; set; }  // her employee'nin mutlaka bir ünvanı olmalı, bu yüzden nullable değil
        public Title Title { get; set; } = null!;  // navigation property, TitleId'ye bakıp Title bilgisini otomatik getirir
    }
}

// burası uygulamamıza giriş yaptığımızda çalışanları yönetebileceğimiz model (her user employee ama her employee user olmak zorunda değil - yani her user bu şirketin empleyeesi ancak her employee user olmak zorunda değil)
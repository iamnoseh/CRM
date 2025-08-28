namespace Domain.DTOs.User.Employee;

public class UpdateEmployeePaymentStatusDto
{
    public int EmployeeId { get; set; }
    public Domain.Enums.PaymentStatus Status { get; set; }
}

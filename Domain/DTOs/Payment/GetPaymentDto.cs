using Domain.Enums;

namespace Domain.DTOs.Payment;

public class GetPaymentDto : CreatePaymentDto
{
    public int Id { get; set; }
}

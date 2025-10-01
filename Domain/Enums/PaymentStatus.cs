namespace Domain.Enums;

public enum PaymentStatus
{
    Failed,
    Completed,
    Pending,
    Paid = Completed  
}
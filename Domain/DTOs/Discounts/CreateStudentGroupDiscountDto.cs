using System.ComponentModel.DataAnnotations;

namespace Domain.DTOs.Discounts;

public class CreateStudentGroupDiscountDto
{
    [Required]
    public int StudentId { get; set; }

    [Required]
    public int GroupId { get; set; }

    [Required]
    [Range(0, double.MaxValue)]
    public decimal DiscountAmount { get; set; }
}



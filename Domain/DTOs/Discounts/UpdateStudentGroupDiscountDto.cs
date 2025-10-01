using System.ComponentModel.DataAnnotations;

namespace Domain.DTOs.Discounts;

public class UpdateStudentGroupDiscountDto
{
    [Required]
    public int Id { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? DiscountAmount { get; set; }
}



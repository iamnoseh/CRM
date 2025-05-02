using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Domain.DTOs.Center;

public class UpdateCenterDto : CreateCenterDto
{
    public int Id { get; set; }
}

using Domain.DTOs.Group;

namespace Infrastructure.Extensions;

public static class GroupDtoExtensions
{
    /// <summary>
    /// Ensures all properties of a GetGroupDto have valid values
    /// </summary>
    /// <param name="dto">The DTO to validate</param>
    /// <returns>The same DTO with validated values</returns>
    public static GetGroupDto EnsureValidValues(this GetGroupDto dto)
    {
        // Ensure DayOfWeek is never 0
        if (dto.DayOfWeek == 0)
        {
            dto.DayOfWeek = 1; // Set to 1 (Monday) if it's 0
        }
        
        // Ensure CurrentWeek is at least 1
        if (dto.CurrentWeek <= 0)
        {
            dto.CurrentWeek = 1;
        }
        
        return dto;
    }
}

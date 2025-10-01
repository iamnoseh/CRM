using Domain.DTOs.Group;

namespace Infrastructure.Extensions;

public static class GroupDtoExtensions
{
    public static GetGroupDto EnsureValidValues(this GetGroupDto dto)
    {
        if (dto.DayOfWeek == 0)
        {
            dto.DayOfWeek = 1;
        }
        if (dto.CurrentWeek <= 0)
        {
            dto.CurrentWeek = 1;
        }
        
        return dto;
    }
}

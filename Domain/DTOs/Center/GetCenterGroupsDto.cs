using Domain.DTOs.Group;

namespace Domain.DTOs.Center;

public class GetCenterGroupsDto
{
    public int CenterId { get; set; }
    public string CenterName { get; set; } = null!;
    public List<GetGroupDto> Groups { get; set; } = new List<GetGroupDto>();
    public int TotalGroups { get; set; }
    public int ActiveGroups { get; set; }
}

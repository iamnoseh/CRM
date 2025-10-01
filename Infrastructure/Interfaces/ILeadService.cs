using Domain.DTOs.Lead;
using Domain.Filters;
using Domain.Responses;

namespace Infrastructure.Interfaces;

public interface ILeadService
{
    Task<Response<string>> CreateLead(CreateLeadDto request);
    Task<Response<string>> UpdateLead(UpdateLeadDto request);
    Task<Response<string>> DeleteLead(int id);
    Task<PaginationResponse<List<GetLeadDto>>> GetLeads(LeadFilter filter);
    Task<Response<GetLeadDto>> GetLead(int id);
}
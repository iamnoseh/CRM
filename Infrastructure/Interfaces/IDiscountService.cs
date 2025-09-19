using Domain.DTOs.Discounts;
using Domain.Responses;

namespace Infrastructure.Interfaces;

public interface IDiscountService
{
    Task<Response<string>> AssignDiscountAsync(CreateStudentGroupDiscountDto dto);
    Task<Response<string>> UpdateDiscountAsync(UpdateStudentGroupDiscountDto dto);
    Task<Response<string>> RemoveDiscountAsync(int id);
    Task<Response<GetStudentGroupDiscountDto>> GetDiscountByIdAsync(int id);
    Task<Response<List<GetStudentGroupDiscountDto>>> GetDiscountsByStudentGroupAsync(int studentId, int groupId);
    Task<Response<DiscountPreviewDto>> PreviewAsync(int studentId, int groupId, int month, int year);
}

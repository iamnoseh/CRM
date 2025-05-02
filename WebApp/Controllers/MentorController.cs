using Domain.DTOs.Mentor;
using Domain.Entities;
using Domain.Filters;
using Domain.Responses;
using Infrastructure.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers;

[Route("api/[controller]")]
[ApiController]
public class MentorController(IMentorService mentorService) : ControllerBase
{
    /// <summary>
    /// Получить всех преподавателей
    /// </summary>
    /// <returns>Список преподавателей</returns>
    [HttpGet]
    public async Task<ActionResult<Response<List<GetMentorDto>>>> GetMentors()
    {
        var response = await mentorService.GetMentors();
        return StatusCode((int)response.StatusCode, response);
    }

    /// <summary>
    /// Получить преподавателя по ID
    /// </summary>
    /// <param name="id">ID преподавателя</param>
    /// <returns>Информация о преподавателе</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<Response<GetMentorDto>>> GetMentorById(int id)
    {
        var response = await mentorService.GetMentorByIdAsync(id);
        return StatusCode((int)response.StatusCode, response);
    }

    /// <summary>
    /// Получить список преподавателей с пагинацией и фильтрацией
    /// </summary>
    /// <param name="filter">Параметры фильтрации и пагинации</param>
    /// <returns>Пагинированный список преподавателей</returns>
    [HttpGet("paginated")]
    public async Task<ActionResult<PaginationResponse<List<GetMentorDto>>>> GetMentorsPagination([FromQuery] MentorFilter filter)
    {
        var response = await mentorService.GetMentorsPagination(filter);
        return StatusCode((int)response.StatusCode, response);
    }

    /// <summary>
    /// Создать нового преподавателя
    /// </summary>
    /// <param name="createMentorDto">Данные для создания преподавателя</param>
    /// <returns>Результат операции</returns>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<Response<string>>> CreateMentor([FromForm] CreateMentorDto createMentorDto)
    {
        var response = await mentorService.CreateMentorAsync(createMentorDto);
        return StatusCode((int)response.StatusCode, response);
    }

    /// <summary>
    /// Обновить существующего преподавателя
    /// </summary>
    /// <param name="id">ID преподавателя</param>
    /// <param name="updateMentorDto">Данные для обновления преподавателя</param>
    /// <returns>Результат операции</returns>
    [HttpPut]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<ActionResult<Response<string>>> UpdateMentor(int id, [FromForm] UpdateMentorDto updateMentorDto)
    {
        var response = await mentorService.UpdateMentorAsync(id, updateMentorDto);
        return StatusCode((int)response.StatusCode, response);
    }

    /// <summary>
    /// Удалить преподавателя
    /// </summary>
    /// <param name="id">ID преподавателя</param>
    /// <returns>Результат операции</returns>
    [HttpDelete]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<Response<string>>> DeleteMentor(int id)
    {
        var response = await mentorService.DeleteMentorAsync(id);
        return StatusCode((int)response.StatusCode, response);
    }

    /// <summary>
    /// Обновить изображение профиля преподавателя
    /// </summary>
    /// <param name="id">ID преподавателя</param>
    /// <param name="profileImage">Новое изображение профиля</param>
    /// <returns>Результат операции</returns>
    [HttpPut("{id}/profile-image")]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<ActionResult<Response<string>>> UpdateProfileImage(int id, IFormFile profileImage)
    {
        var response = await mentorService.UpdateUserProfileImageAsync(id, profileImage);
        return StatusCode((int)response.StatusCode, response);
    }

    /// <summary>
    /// Получить преподавателей по группе
    /// </summary>
    /// <param name="groupId">ID группы</param>
    /// <returns>Список преподавателей группы</returns>
    [HttpGet("by-group/{groupId}")]
    public async Task<ActionResult<Response<List<GetMentorDto>>>> GetMentorsByGroup(int groupId)
    {
        var response = await mentorService.GetMentorsByGroupAsync(groupId);
        return StatusCode((int)response.StatusCode, response);
    }

    /// <summary>
    /// Получить преподавателей по курсу
    /// </summary>
    /// <param name="courseId">ID курса</param>
    /// <returns>Список преподавателей курса</returns>
    [HttpGet("by-course/{courseId}")]
    public async Task<ActionResult<Response<List<GetMentorDto>>>> GetMentorsByCourse(int courseId)
    {
        var response = await mentorService.GetMentorsByCourseAsync(courseId);
        return StatusCode((int)response.StatusCode, response);
    }
}
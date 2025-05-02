using Domain.DTOs.Center;
using Domain.Filters;
using Domain.Responses;
using Infrastructure.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers;

[Route("api/[controller]")]
[ApiController]
public class CenterController(ICenterService centerService) : ControllerBase
{
    /// <summary>
    /// Получить все центры
    /// </summary>
    /// <returns>Список центров</returns>
    [HttpGet]
    public async Task<ActionResult<Response<List<GetCenterDto>>>> GetCenters()
    {
        var response = await centerService.GetCenters();
        return StatusCode((int)response.StatusCode, response);
    }

    /// <summary>
    /// Получить центр по ID
    /// </summary>
    /// <param name="id">ID центра</param>
    /// <returns>Информация о центре</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<Response<GetCenterDto>>> GetCenterById(int id)
    {
        var response = await centerService.GetCenterByIdAsync(id);
        return StatusCode((int)response.StatusCode, response);
    }

    /// <summary>
    /// Получить список центров с пагинацией и фильтрацией
    /// </summary>
    /// <param name="filter">Параметры фильтрации и пагинации</param>
    /// <returns>Пагинированный список центров</returns>
    [HttpGet("paginated")]
    public async Task<ActionResult<PaginationResponse<List<GetCenterDto>>>> GetCentersPaginated([FromQuery] CenterFilter filter)
    {
        var response = await centerService.GetCentersPaginated(filter);
        return StatusCode((int)response.StatusCode, response);
    }

    /// <summary>
    /// Создать новый центр
    /// </summary>
    /// <param name="createCenterDto">Данные для создания центра</param>
    /// <returns>Результат операции</returns>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<Response<string>>> CreateCenter([FromForm] CreateCenterDto createCenterDto)
    {
        var response = await centerService.CreateCenterAsync(createCenterDto);
        return StatusCode((int)response.StatusCode, response);
    }

    /// <summary>
    /// Обновить существующий центр
    /// </summary>
    /// <param name="id">ID центра</param>
    /// <param name="updateCenterDto">Данные для обновления центра</param>
    /// <returns>Результат операции</returns>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<Response<string>>> UpdateCenter(int id, [FromForm] UpdateCenterDto updateCenterDto)
    {
        var response = await centerService.UpdateCenterAsync(id, updateCenterDto);
        return StatusCode((int)response.StatusCode, response);
    }

    /// <summary>
    /// Удалить центр
    /// </summary>
    /// <param name="id">ID центра</param>
    /// <returns>Результат операции</returns>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<Response<string>>> DeleteCenter(int id)
    {
        var response = await centerService.DeleteCenterAsync(id);
        return StatusCode((int)response.StatusCode, response);
    }

    /// <summary>
    /// Получить группы, связанные с центром
    /// </summary>
    /// <param name="id">ID центра</param>
    /// <returns>Список групп центра</returns>
    [HttpGet("{id}/groups")]
    public async Task<ActionResult<Response<List<GetCenterGroupsDto>>>> GetCenterGroups(int id)
    {
        var response = await centerService.GetCenterGroupsAsync(id);
        return StatusCode((int)response.StatusCode, response);
    }

    /// <summary>
    /// Получить студентов центра
    /// </summary>
    /// <param name="id">ID центра</param>
    /// <returns>Список студентов центра</returns>
    [HttpGet("{id}/students")]
    public async Task<ActionResult<Response<List<GetCenterStudentsDto>>>> GetCenterStudents(int id)
    {
        var response = await centerService.GetCenterStudentsAsync(id);
        return StatusCode((int)response.StatusCode, response);
    }

    /// <summary>
    /// Получить преподавателей центра
    /// </summary>
    /// <param name="id">ID центра</param>
    /// <returns>Список преподавателей центра</returns>
    [HttpGet("{id}/mentors")]
    public async Task<ActionResult<Response<List<GetCenterMentorsDto>>>> GetCenterMentors(int id)
    {
        var response = await centerService.GetCenterMentorsAsync(id);
        return StatusCode((int)response.StatusCode, response);
    }

    /// <summary>
    /// Получить курсы центра
    /// </summary>
    /// <param name="id">ID центра</param>
    /// <returns>Список курсов центра</returns>
    [HttpGet("{id}/courses")]
    public async Task<ActionResult<Response<List<GetCenterCoursesDto>>>> GetCenterCourses(int id)
    {
        var response = await centerService.GetCenterCoursesAsync(id);
        return StatusCode((int)response.StatusCode, response);
    }

    /// <summary>
    /// Получить статистику по центру
    /// </summary>
    /// <param name="id">ID центра</param>
    /// <returns>Статистика центра</returns>
    [HttpGet("{id}/statistics")]
    public async Task<ActionResult<Response<CenterStatisticsDto>>> GetCenterStatistics(int id)
    {
        var response = await centerService.GetCenterStatisticsAsync(id);
        return StatusCode((int)response.StatusCode, response);
    }
}

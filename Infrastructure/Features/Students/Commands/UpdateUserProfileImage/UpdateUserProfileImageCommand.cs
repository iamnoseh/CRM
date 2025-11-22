using MediatR;
using Domain.Responses;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Features.Students.Commands.UpdateUserProfileImage;

public record UpdateUserProfileImageCommand(int StudentId, IFormFile ProfileImage) : IRequest<Response<string>>;

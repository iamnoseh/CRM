namespace Infrastructure.Services.HashService;

public interface IHashService
{
    string ConvertToHash(string rawData);
}
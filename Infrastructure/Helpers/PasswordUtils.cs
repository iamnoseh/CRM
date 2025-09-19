namespace Infrastructure.Helpers;

public static class PasswordUtils
{
    public static string GenerateRandomPassword(int length = 8)
    {
        if (length < 2) length = 2; 
        const string digits = "0123456789";
        const string letters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";

        var random = new Random();
        var chars = new char[length];

        for (int i = 0; i < length; i++)
        {
            chars[i] = digits[random.Next(digits.Length)];
        }

        int letterIndex = random.Next(length);
        chars[letterIndex] = letters[random.Next(letters.Length)];

        return new string(chars);
    }
}
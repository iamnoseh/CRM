namespace Infrastructure.Helpers;

public static class PasswordUtils
{
    public static string GenerateRandomPassword(int length = 8)
    {
        // Generate password that contains only digits and exactly one letter
        if (length < 2) length = 2; // ensure room for at least one digit and one letter

        const string digits = "0123456789";
        const string letters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";

        var random = new Random();
        var chars = new char[length];

        // Fill all positions with digits
        for (int i = 0; i < length; i++)
        {
            chars[i] = digits[random.Next(digits.Length)];
        }

        // Replace one random position with a letter (exactly one letter in the password)
        int letterIndex = random.Next(length);
        chars[letterIndex] = letters[random.Next(letters.Length)];

        return new string(chars);
    }
}
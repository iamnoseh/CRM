namespace Infrastructure.Helpers;

public static class PasswordUtils
{
    public static string GenerateRandomPassword(int length = 8)
    {
        const string upperChars = "ABC";
        const string lowerChars = "a";
        const string numericChars = "0123456789";
        const string specialChars = "-";

        var random = new Random();
        var chars = new List<char>();
        chars.Add(upperChars[random.Next(upperChars.Length)]);
        chars.Add(lowerChars[random.Next(lowerChars.Length)]);
        chars.Add(numericChars[random.Next(numericChars.Length)]);
        chars.Add(specialChars[random.Next(specialChars.Length)]);
        for (int i = chars.Count; i < length; i++)
        {
            var allChars = upperChars + lowerChars + numericChars + specialChars;
            chars.Add(allChars[random.Next(allChars.Length)]);
        }

        for (int i = 0; i < chars.Count; i++)
        {
            int swapIndex = random.Next(chars.Count);
            (chars[i], chars[swapIndex]) = (chars[swapIndex], chars[i]);
        }

        return new string(chars.ToArray());
    }
}
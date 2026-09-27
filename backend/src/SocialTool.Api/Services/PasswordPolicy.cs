namespace SocialTool.Api.Services;

public static class PasswordPolicy
{
    public const int MinLength = 10;
    public const int MaxLength = 128;

    public static string? Validate(string? password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < MinLength)
            return $"A senha precisa ter pelo menos {MinLength} caracteres.";
        if (password.Length > MaxLength)
            return $"A senha pode ter no máximo {MaxLength} caracteres.";
        return null;
    }
}

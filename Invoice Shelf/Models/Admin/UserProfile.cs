namespace InvoiceShelf.Models.Admin;

public record UserProfileResponse(
    [property: JsonPropertyName("data")] UserProfile? Data
);

public record UserProfile(
    [property: JsonPropertyName("id")]           int     Id,
    [property: JsonPropertyName("name")]         string  Name,
    [property: JsonPropertyName("email")]        string  Email,
    [property: JsonPropertyName("phone")]        string? Phone,
    [property: JsonPropertyName("role")]         string? Role,
    [property: JsonPropertyName("company_name")] string? CompanyName
)
{
    public string Initials
    {
        get
        {
            if (string.IsNullOrEmpty(Name)) return "?";
            var parts = Name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length >= 2
                ? $"{parts[0][0]}{parts[1][0]}".ToUpper()
                : Name[0].ToString().ToUpper();
        }
    }
}

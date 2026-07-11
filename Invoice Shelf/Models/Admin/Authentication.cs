namespace InvoiceShelf.Models.Admin.Authentication;

public record LoginRequest
{
    [JsonPropertyName("username")]
    public string Username { get; init; } = string.Empty;

    [JsonPropertyName("password")]
    public string Password { get; init; } = string.Empty;

    [JsonPropertyName("device_name")]
    public string DeviceName { get; init; } =
        $"{DeviceInfo.Current.Manufacturer} {DeviceInfo.Current.Model}";
}

public record LoginAnswer(
    [property: JsonPropertyName("type")]  string Type,
    [property: JsonPropertyName("token")] string Token
);

public record LogoutStatus(
    [property: JsonPropertyName("sucess")] bool Success
);

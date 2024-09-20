namespace ssd_authorization_solution.JwtSettings;

public class JwtSettings
{
    public string Issuer { get; set; }
    public string Audience { get; set; }
    public string Secret { get; set; }
    public int TokenExpirationMinutes { get; set; }
}

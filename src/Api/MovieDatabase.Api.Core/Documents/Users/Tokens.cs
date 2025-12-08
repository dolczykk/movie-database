namespace MovieDatabase.Api.Core.Documents.Users;

public class ClaimToken : BaseDocument
{
    public string AccessToken { get; set; }
    public string RefreshToken { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; }
}
namespace MovieDatabase.Api.Core.Exceptions.Auth;

public class TokenCannotBeRevokedApplicationException(string message = "Token cannot be revoked") : BaseApplicationException(message);
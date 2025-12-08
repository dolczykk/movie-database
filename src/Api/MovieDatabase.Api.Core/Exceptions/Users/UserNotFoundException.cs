namespace MovieDatabase.Api.Core.Exceptions.Users;

public class UserNotFoundException(string message = "User not found.") : ApplicationException(message);
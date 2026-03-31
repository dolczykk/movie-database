namespace MovieDatabase.Api.Core.Exceptions.Cqrs;

public class RequestHandlerNotFoundException(string message = "Can't find registered request handler") : BaseApplicationException(message);
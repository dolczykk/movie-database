namespace MovieDatabase.Api.Core.Exceptions.Blobs;

public class NotSupportedContentTypeApplicationException(string message = "The provided content type is not supported") : ApplicationException(message);
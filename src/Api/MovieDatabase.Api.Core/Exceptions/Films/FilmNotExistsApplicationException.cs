namespace MovieDatabase.Api.Core.Exceptions.Films;

public class FilmNotExistsApplicationException(string message = "Film not exist in database") : BaseApplicationException(message);
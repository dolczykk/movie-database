namespace MovieDatabase.FakeDataGenerator;

internal static class FilmUtils
{
    public static string SurnamePart(string? full)
    {
        if (string.IsNullOrWhiteSpace(full))
        {
            return "";
        }

        var parts = full!.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 1 ? string.Join(' ', parts.Skip(1)) : "";
    }

    public static string NamePart(string? full)
    {
        if (string.IsNullOrWhiteSpace(full))
        {
            return "";
        }

        var parts = full!.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 0 ? parts[0] : full!;
    }
}
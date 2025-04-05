namespace KareClass.Utils{

public static class Helper
{
    public static string RemoveTurkishCharacters(string input)
    {
        var replacements = new Dictionary<char, string>
    {
        { 'ı', "i" }, { 'ş', "s" }, { 'ğ', "g" }, { 'ü', "u" }, { 'ç', "c" }, { 'ö', "o" },
        { 'İ', "I" }, { 'Ş', "S" }, { 'Ğ', "G" }, { 'Ü', "U" }, { 'Ç', "C" }, { 'Ö', "O" }
    };

        foreach (var replacement in replacements)
        {
            input = input.Replace(replacement.Key.ToString(), replacement.Value);
        }
        return input;
    }
}
}
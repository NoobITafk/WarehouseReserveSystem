namespace WarehouseReserveSystem.Services;

public static class UkrainianNumberToWords
{
    private static readonly string[] UnitsFemale = ["", "одна", "дві", "три", "чотири", "п'ять", "шість", "сім", "вісім", "дев'ять"];
    private static readonly string[] UnitsMale = ["", "один", "два", "три", "чотири", "п'ять", "шість", "сім", "вісім", "дев'ять"];
    private static readonly string[] Teens = ["десять", "одинадцять", "дванадцять", "тринадцять", "чотирнадцять", "п'ятнадцять", "шістнадцять", "сімнадцять", "вісімнадцять", "дев'ятнадцять"];
    private static readonly string[] Tens = ["", "", "двадцять", "тридцять", "сорок", "п'ятдесят", "шістдесят", "сімдесят", "вісімдесят", "дев'яносто"];
    private static readonly string[] Hundreds = ["", "сто", "двісті", "триста", "чотириста", "п'ятсот", "шістсот", "сімсот", "вісімсот", "дев'ятсот"];

    public static string AmountToWords(decimal amount)
    {
        long hryvnias = (long)Math.Floor(amount);
        int kopecks = (int)Math.Round((amount - hryvnias) * 100);

        string hryvniaWords = IntegerToWords(hryvnias);
        string hryvniaSuffix = GetDeclension(hryvnias, "гривня", "гривні", "гривень");
        string kopeckSuffix = GetDeclension(kopecks, "копійка", "копійки", "копійок");

        // Capitalize first letter
        if (hryvniaWords.Length > 0)
        {
            hryvniaWords = char.ToUpper(hryvniaWords[0]) + hryvniaWords[1..];
        }

        return $"{hryvniaWords} {hryvniaSuffix} {kopecks:D2} {kopeckSuffix}";
    }

    public static string NumberOfItemsToWords(int count)
    {
        string words = IntegerToWords(count, isFemale: false);
        string suffix = GetDeclension(count, "найменування", "найменування", "найменувань");
        return $"{count} ({words}) {suffix}";
    }

    private static string IntegerToWords(long number, bool isFemale = true)
    {
        if (number == 0) return "нуль";
        if (number < 0) return "мінус " + IntegerToWords(-number, isFemale);

        var parts = new List<string>();

        // Billions
        long billions = number / 1_000_000_000;
        if (billions > 0)
        {
            parts.Add(GroupToWords((int)billions, false));
            parts.Add(GetDeclension(billions, "мільярд", "мільярди", "мільярдів"));
            number %= 1_000_000_000;
        }

        // Millions
        long millions = number / 1_000_000;
        if (millions > 0)
        {
            parts.Add(GroupToWords((int)millions, false));
            parts.Add(GetDeclension(millions, "мільйон", "мільйони", "мільйонів"));
            number %= 1_000_000;
        }

        // Thousands
        long thousands = number / 1_000;
        if (thousands > 0)
        {
            parts.Add(GroupToWords((int)thousands, true));
            parts.Add(GetDeclension(thousands, "тисяча", "тисячі", "тисяч"));
            number %= 1_000;
        }

        // Remainder 1..999
        if (number > 0)
        {
            parts.Add(GroupToWords((int)number, isFemale));
        }

        return string.Join(" ", parts.Where(p => !string.IsNullOrWhiteSpace(p))).Trim();
    }

    private static string GroupToWords(int val, bool isFemale)
    {
        var words = new List<string>();
        int h = val / 100;
        int rem = val % 100;

        if (h > 0) words.Add(Hundreds[h]);

        if (rem is >= 10 and <= 19)
        {
            words.Add(Teens[rem - 10]);
        }
        else
        {
            int t = rem / 10;
            int u = rem % 10;
            if (t > 0) words.Add(Tens[t]);
            if (u > 0)
            {
                words.Add(isFemale ? UnitsFemale[u] : UnitsMale[u]);
            }
        }

        return string.Join(" ", words);
    }

    private static string GetDeclension(long n, string one, string twoToFour, string other)
    {
        long n100 = n % 100;
        long n10 = n % 10;

        if (n100 is >= 11 and <= 19) return other;
        if (n10 == 1) return one;
        if (n10 is >= 2 and <= 4) return twoToFour;
        return other;
    }
}

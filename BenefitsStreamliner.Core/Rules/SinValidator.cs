namespace BenefitsStreamliner.Core.Rules;

public static class SinValidator
{
    // Keeps only the characters 0-9 (so "046-454-286" and "046 454 286" both work).
    public static string Digits(string? value) =>
        new((value ?? string.Empty).Where(c => c is >= '0' and <= '9').ToArray());

    // 9 digits and a valid Luhn checksum. Real SINs pass the Luhn check.
    public static bool IsValid(string? value)
    {
        var d = Digits(value);
        if (d.Length != 9) return false;

        var sum = 0;
        for (var i = 0; i < 9; i++)
        {
            var n = d[i] - '0';
            if (i % 2 == 1)            // double every second digit, starting with the 2nd from the left
            {
                n *= 2;
                if (n > 9) n -= 9;
            }
            sum += n;
        }
        return sum % 10 == 0;
    }
}

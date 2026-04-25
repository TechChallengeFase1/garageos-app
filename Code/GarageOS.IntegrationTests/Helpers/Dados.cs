namespace GarageOS.IntegrationTests.Helpers;

/// <summary>
/// Generates unique, mathematically valid CPF/CNPJ/Email/Placa for each test call.
/// Uses atomic counters so parallel test classes never collide.
/// </summary>
internal static class Dados
{
    private static int _cpfBase = 100_000_000;
    private static int _cnpjBase = 10_000_000;
    private static int _placaSeq = 0;

    public static string Cpf()
    {
        while (true)
        {
            var n = Interlocked.Increment(ref _cpfBase);
            var d = n.ToString().PadLeft(9, '0').Select(c => c - '0').ToArray();
            if (d.Distinct().Count() == 1) continue;
            var d1 = Digito(d, new[] { 10, 9, 8, 7, 6, 5, 4, 3, 2 });
            var d2 = Digito(d.Append(d1).ToArray(), new[] { 11, 10, 9, 8, 7, 6, 5, 4, 3, 2 });
            return string.Concat(d) + d1 + d2;
        }
    }

    public static string Cnpj()
    {
        while (true)
        {
            var n = Interlocked.Increment(ref _cnpjBase);
            var b = n.ToString().PadLeft(8, '0').Select(c => c - '0').ToArray();
            if (b.Distinct().Count() == 1) continue;
            var d = b.Concat(new[] { 0, 0, 0, 1 }).ToArray();
            var d1 = Digito(d, new[] { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 });
            var d2 = Digito(d.Append(d1).ToArray(), new[] { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 });
            return string.Concat(d) + d1 + d2;
        }
    }

    public static string Email()
    {
        var id = Interlocked.Increment(ref _placaSeq);
        return $"teste{id}.{Guid.NewGuid():N}@test.com";
    }

    public static string Telefone()
    {
        var n = Interlocked.Increment(ref _placaSeq);
        return $"11{n:D9}";
    }

    public static string Placa()
    {
        var seq = Interlocked.Increment(ref _placaSeq);
        var n = seq % 10000;
        var l = seq / 10000 % (26 * 26 * 26);
        var l3 = (char)('A' + l % 26);
        var l2 = (char)('A' + l / 26 % 26);
        var l1 = (char)('A' + l / 676 % 26);
        return $"{l1}{l2}{l3}{n / 1000 % 10}{n / 100 % 10}{n / 10 % 10}{n % 10}";
    }

    private static int Digito(int[] digits, int[] weights)
    {
        var soma = digits.Select((d, i) => d * weights[i]).Sum();
        var resto = soma % 11;
        return resto < 2 ? 0 : 11 - resto;
    }
}

using GarageOS.Domain.Enums;

namespace GarageOS.Domain.ValueObjects;

public class Documento
{
    public string Valor { get; private set; }
    public TipoDocumento Tipo { get; private set; }

    protected Documento() { }

    public Documento(string documento)
    {
        var doc = new string(documento.Where(char.IsDigit).ToArray());

        if (doc.Length == 11)
        {
            if (!ValidarCPF(doc))
                throw new ArgumentException("CPF inválido.");
            Tipo = TipoDocumento.CPF;
        }
        else if (doc.Length == 14)
        {
            if (!ValidarCNPJ(doc))
                throw new ArgumentException("CNPJ inválido.");
            Tipo = TipoDocumento.CNPJ;
        }
        else
        {
            throw new ArgumentException("Documento deve ser CPF ou CNPJ.");
        }

        Valor = doc;
    }

    private static bool ValidarCPF(string doc)
    {
        if (doc.Distinct().Count() == 1) return false;

        int[] mult1 = { 10, 9, 8, 7, 6, 5, 4, 3, 2 };
        int[] mult2 = { 11, 10, 9, 8, 7, 6, 5, 4, 3, 2 };

        var d1 = CalcularDigito(doc[..9], mult1);
        var d2 = CalcularDigito(doc[..10], mult2);

        return doc[9] - '0' == d1 && doc[10] - '0' == d2;
    }

    private static bool ValidarCNPJ(string doc)
    {
        if (doc.Distinct().Count() == 1) return false;

        int[] mult1 = { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
        int[] mult2 = { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };

        var d1 = CalcularDigito(doc[..12], mult1);
        var d2 = CalcularDigito(doc[..13], mult2);

        return doc[12] - '0' == d1 && doc[13] - '0' == d2;
    }

    private static int CalcularDigito(string trecho, int[] multiplicadores)
    {
        var soma = trecho.Select((c, i) => (c - '0') * multiplicadores[i]).Sum();
        var resto = soma % 11;
        return resto < 2 ? 0 : 11 - resto;
    }

    public override string ToString() => Valor;
}

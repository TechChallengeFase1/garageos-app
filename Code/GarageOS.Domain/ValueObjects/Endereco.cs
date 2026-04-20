namespace GarageOS.Domain.ValueObjects
{
    public class Endereco
    {
        public string Logradouro { get; private set; }
        public string Numero { get; private set; }
        public string? Complemento { get; private set; }
        public string Bairro { get; private set; }
        public string Cidade { get; private set; }
        public string Estado { get; private set; }
        public string Cep { get; private set; } 

        protected Endereco() { }

        public Endereco(string logradouro, string numero, string bairro,
                        string cidade, string estado, string cep, string? complemento = null)
        {
            Logradouro = logradouro;
            Numero = numero;
            Complemento = complemento;
            Bairro = bairro;
            Cidade = cidade;
            Estado = estado;
            Cep = new string(cep.Where(char.IsDigit).ToArray());
        }
    }
}

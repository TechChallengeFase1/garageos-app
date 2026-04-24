using GarageOS.Domain.Utils;
using GarageOS.Domain.ValueObjects;

namespace GarageOS.Domain.Entities
{
    public class Cliente
    {
        public Guid Id { get; private set; }
        public string Nome { get; private set; }
        public Documento Documento { get; private set; } // ← VO já validado
        public string Email { get; private set; }
        public string Telefone { get; private set; }
        public Endereco Endereco { get; private set; } 
        public bool Ativo { get; private set; }
        public DateTime CriadoEm { get; private set; }
        public DateTime AtualizadoEm { get; private set; }

        protected Cliente() { }

        public Cliente(string nome, string documento, string email, string telefone, Endereco endereco)
        {
            Id = Guid.NewGuid();
            Nome = nome;
            Documento = new Documento(documento);
            Email = email;
            Telefone = telefone;
            Endereco = endereco;
            Ativo = true;
            CriadoEm = BrasiliaTime.Agora;
            AtualizadoEm = BrasiliaTime.Agora;

            Validar();
        }

        private void Validar()
        {
            if (string.IsNullOrWhiteSpace(Nome))
                throw new ArgumentException("Nome é obrigatório.");

            if (string.IsNullOrWhiteSpace(Email) || !Email.Contains('@'))
                throw new ArgumentException("E-mail inválido.");

            if (string.IsNullOrWhiteSpace(Telefone))
                throw new ArgumentException("Telefone é obrigatório.");
        }

        public void Atualizar(string nome, string email, string telefone, Endereco endereco)
        {
            Nome = nome;
            Email = email;
            Telefone = telefone;
            Endereco = endereco;
            AtualizadoEm = BrasiliaTime.Agora;

            Validar();
        }

        
        public void Desativar()
        {
            if (!Ativo)
                throw new ArgumentException("Cliente já está inativo.");

            Ativo = false;
            AtualizadoEm = BrasiliaTime.Agora;
        }

        
        public void Ativar()
        {
            if (Ativo)
                throw new ArgumentException("Cliente já está ativo.");

            Ativo = true;
            AtualizadoEm = BrasiliaTime.Agora;
        }

    }
}

using GarageOS.Application.DTOs.Clientes;
using GarageOS.Application.DTOs.Servicos;
using GarageOS.Domain.Repositories;

namespace GarageOS.Application.UseCases.Clientes
{
    public class ListarClientesUseCase
    {
        private readonly IClienteRepository _repository;

        public ListarClientesUseCase(IClienteRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<ClienteResponse>> ExecutarAsync()
        {
            var clientes = await _repository.ListarTodosAsync();
            return clientes.Select(c => new ClienteResponse
            {
                Id = c.Id,
                Nome = c.Nome,
                Documento = c.Documento.Valor,
                TipoDocumento = c.Documento.Tipo.ToString(),
                Email = c.Email,
                Telefone = c.Telefone,
                Ativo = c.Ativo,
                Endereco = new EnderecoResponse
                {
                    Logradouro = c.Endereco.Logradouro,
                    Numero = c.Endereco.Numero,
                    Complemento = c.Endereco.Complemento,
                    Bairro = c.Endereco.Bairro,
                    Cidade = c.Endereco.Cidade,
                    Estado = c.Endereco.Estado,
                    Cep = c.Endereco.Cep
                }
            });
        }

    }
}

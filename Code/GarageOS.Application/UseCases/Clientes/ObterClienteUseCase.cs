using GarageOS.Application.DTOs.Clientes;
using GarageOS.Domain.Exceptions;
using GarageOS.Domain.Repositories;

namespace GarageOS.Application.UseCases.Clientes
{
    public class ObterClienteUseCase
    {
        private readonly IClienteRepository _repository;

        public ObterClienteUseCase(IClienteRepository repository)
        {
            _repository = repository;
        }

        public async Task<ClienteResponse> ExecutarAsync(Guid id)
        {
            var cliente = await _repository.ObterPorIdAsync(id)
                ?? throw new ClienteNaoEncontradoException(id);

            return new ClienteResponse
            {
                Id = cliente.Id,
                Nome = cliente.Nome,
                Documento = cliente.Documento.Valor,
                TipoDocumento = cliente.Documento.Tipo.ToString(),
                Email = cliente.Email,
                Telefone = cliente.Telefone,
                Ativo = cliente.Ativo,
                Endereco = new EnderecoResponse
                {
                    Logradouro = cliente.Endereco.Logradouro,
                    Numero = cliente.Endereco.Numero,
                    Complemento = cliente.Endereco.Complemento,
                    Bairro = cliente.Endereco.Bairro,
                    Cidade = cliente.Endereco.Cidade,
                    Estado = cliente.Endereco.Estado,
                    Cep = cliente.Endereco.Cep
                }

            };
        }
    }
}
using GarageOS.Application.DTOs.Clientes;
using GarageOS.Domain.Exceptions;
using GarageOS.Domain.Repositories;
using GarageOS.Domain.ValueObjects;

namespace GarageOS.Application.UseCases.Clientes;

public class AlterarClienteUseCase
{
    private readonly IClienteRepository _repository;

    public AlterarClienteUseCase(IClienteRepository repository)
    {
        _repository = repository;
    }

    public async Task<ClienteResponse> ExecutarAsync(Guid id, AtualizarClienteRequest request)
    {
        var cliente = await _repository.ObterPorIdAsync(id)
            ?? throw new ClienteNaoEncontradoException(id);

        // Valida documento — ignora se for o mesmo cliente
        var docExistente = await _repository.ObterPorDocumentoAsync(
       new string(request.Documento.Where(char.IsDigit).ToArray()));
        if (docExistente is not null && docExistente.Id != id)
            throw new ClienteJaCadastradoException("documento", request.Documento);

        // Valida email — ignora se for o mesmo cliente
        var emailExistente = await _repository.ObterPorEmailAsync(request.Email);
        if (emailExistente is not null && emailExistente.Id != id)
            throw new ClienteJaCadastradoException("e-mail", request.Email);

        // Valida telefone — ignora se for o mesmo cliente
        var telefoneExistente = await _repository.ObterPorTelefoneAsync(request.Telefone);
        if (telefoneExistente is not null && telefoneExistente.Id != id)
            throw new ClienteJaCadastradoException("telefone", request.Telefone);

        var endereco = new Endereco(
            request.Logradouro,
            request.Numero,
            request.Bairro,
            request.Cidade,
            request.Estado,
            request.Cep,
            request.Complemento);

        cliente.Atualizar(request.Nome, request.Email, request.Telefone, endereco);

        await _repository.AtualizarAsync(cliente);

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
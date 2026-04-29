using GarageOS.Application.DTOs.Clientes;
using GarageOS.Application.UseCases.Clientes;
using GarageOS.Application.Validators.Clientes;
using GarageOS.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GarageOS.Api.Controllers;

/// <summary>Controller de gerenciamento de clientes</summary>
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ClientesController : ControllerBase
{
    private readonly ListarClientesUseCase _listarClientesUseCase;
    private readonly CadastrarClienteUseCase _cadastrarClienteUseCase;
    private readonly ObterClienteUseCase _obterClienteUseCase;
    private readonly AlterarClienteUseCase _alterarClienteUseCase;
    private readonly DeletarClienteUseCase _deletarClienteUseCase;

    /// <summary>Inicializa o controller com os use cases de clientes</summary>
    public ClientesController(
        ListarClientesUseCase listarClientesUseCase,
        CadastrarClienteUseCase cadastrarClienteUseCase,
        ObterClienteUseCase obterClienteUseCase,
        AlterarClienteUseCase alterarClienteUseCase,
        DeletarClienteUseCase deletarClienteUseCase)
    {
        _listarClientesUseCase = listarClientesUseCase;
        _cadastrarClienteUseCase = cadastrarClienteUseCase;
        _obterClienteUseCase = obterClienteUseCase;
        _alterarClienteUseCase = alterarClienteUseCase;
        _deletarClienteUseCase = deletarClienteUseCase;
    }

    /// <summary>Lista todos os clientes cadastrados</summary>
    /// <remarks>
    /// Retorna uma lista de todos os clientes ativos do sistema.
    /// Clientes desativados (soft delete) não aparecem nesta lista.
    /// Cada cliente inclui: ID, nome, documento, email, telefone, endereço completo e data de criação.
    /// </remarks>
    /// <returns>Lista completa de clientes</returns>
    /// <response code="200">Lista retornada com sucesso</response>
    /// <response code="401">Não autorizado - token JWT ausente ou inválido</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ClienteResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar()
    {
        var resultado = await _listarClientesUseCase.ExecutarAsync();
        return Ok(resultado);
    }

    /// <summary>Cadastra um novo cliente</summary>
    /// <remarks>
    /// Cria um novo cliente no sistema com dados obrigatórios validados.
    /// O documento (CPF ou CNPJ) é validado e verifica se já existe um cliente com o mesmo documento.
    /// O email deve ser válido e único no sistema.
    ///
    /// Validações realizadas:
    /// - Nome: obrigatório, máximo 150 caracteres
    /// - Documento: obrigatório, CPF ou CNPJ válido (não pode duplicar)
    /// - Email: obrigatório, formato válido, único no sistema
    /// - Telefone: obrigatório
    /// - Endereço: completo com rua, número, bairro, cidade, estado, CEP
    /// </remarks>
    /// <param name="request">Dados do novo cliente (nome, documento, email, telefone, endereço)</param>
    /// <returns>Cliente criado com sucesso</returns>
    /// <response code="201">Cliente criado com sucesso</response>
    /// <response code="400">Dados inválidos (validação de campos falhou)</response>
    /// <response code="409">Conflito - cliente com este documento ou email já existe</response>
    /// <response code="401">Não autorizado - token JWT ausente ou inválido</response>
    [HttpPost]
    [ProducesResponseType(typeof(ClienteResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cadastrar([FromBody] CriarClienteRequest request)
    {
        var validator = new CriarClienteValidator();
        var validation = await validator.ValidateAsync(request);

        if (!validation.IsValid)
            return BadRequest(validation.Errors.Select(e => e.ErrorMessage));

        var resultado = await _cadastrarClienteUseCase.ExecutarAsync(request);
        return CreatedAtAction(nameof(Obter), new { id = resultado.Id }, resultado);
    }

    /// <summary>Obtém os dados de um cliente específico</summary>
    /// <remarks>
    /// Retorna os dados completos de um cliente pelo seu ID único.
    /// Inclui todas as informações pessoais, documento, endereço e datas de auditoria.
    /// Apenas clientes ativos podem ser consultados (desativados retornam 404).
    /// </remarks>
    /// <param name="id">ID único do cliente (GUID)</param>
    /// <returns>Dados completos do cliente</returns>
    /// <response code="200">Cliente encontrado - dados retornados com sucesso</response>
    /// <response code="404">Cliente não encontrado ou foi desativado</response>
    /// <response code="401">Não autorizado - token JWT ausente ou inválido</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ClienteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obter(Guid id)
    {
        try
        {
            var resultado = await _obterClienteUseCase.ExecutarAsync(id);
            return Ok(resultado);
        }
        catch (ClienteNaoEncontradoException ex)
        {
            return NotFound(new { mensagem = ex.Message });
        }
    }

    /// <summary>Atualiza os dados de um cliente existente</summary>
    /// <remarks>
    /// Permite atualizar os dados cadastrais de um cliente.
    /// O documento (CPF/CNPJ) NÃO pode ser alterado (é imutável).
    /// Email será validado quanto à unicidade (não pode duplicar com outro cliente).
    ///
    /// Campos atualizáveis:
    /// - Nome
    /// - Email (com validação de unicidade)
    /// - Telefone
    /// - Endereço completo
    ///
    /// Campo imutável:
    /// - Documento (CPF/CNPJ)
    /// </remarks>
    /// <param name="id">ID único do cliente a atualizar (GUID)</param>
    /// <param name="request">Novos dados do cliente</param>
    /// <returns>Cliente atualizado com os novos dados</returns>
    /// <response code="200">Cliente atualizado com sucesso</response>
    /// <response code="400">Dados inválidos (validação de campos falhou)</response>
    /// <response code="404">Cliente não encontrado</response>
    /// <response code="401">Não autorizado - token JWT ausente ou inválido</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ClienteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Alterar(Guid id, [FromBody] AtualizarClienteRequest request)
    {
        var validator = new AtualizarClienteValidator();
        var validation = await validator.ValidateAsync(request);

        if (!validation.IsValid)
            return BadRequest(validation.Errors.Select(e => e.ErrorMessage));

        try
        {
            var resultado = await _alterarClienteUseCase.ExecutarAsync(id, request);
            return Ok(resultado);
        }
        catch (ClienteNaoEncontradoException ex)
        {
            return NotFound(new { mensagem = ex.Message });
        }
    }

    /// <summary>Desativa um cliente (soft delete)</summary>
    /// <remarks>
    /// Marca um cliente como inativo no sistema sem deletar seus dados.
    /// Esta é uma operação de soft delete - os dados são preservados para auditoria e histórico.
    /// Um cliente desativado:
    /// - Não aparece em listagens
    /// - Não pode ter novas operações associadas
    /// - Pode ser reativado manualmente se necessário (via atualização do campo Ativo)
    /// - Mantém histórico de Ordens de Serviço e transações anteriores
    /// </remarks>
    /// <param name="id">ID único do cliente a desativar (GUID)</param>
    /// <returns>Nenhum conteúdo (operação bem-sucedida)</returns>
    /// <response code="204">Cliente desativado com sucesso - sem conteúdo na resposta</response>
    /// <response code="404">Cliente não encontrado</response>
    /// <response code="401">Não autorizado - token JWT ausente ou inválido</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deletar(Guid id)
    {
        try
        {
            await _deletarClienteUseCase.ExecutarAsync(id);
            return NoContent();
        }
        catch (ClienteNaoEncontradoException ex)
        {
            return NotFound(new { mensagem = ex.Message });
        }
    }
}
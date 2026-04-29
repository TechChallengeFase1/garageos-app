using GarageOS.Application.DTOs.Servicos;
using GarageOS.Application.UseCases.Servicos;
using GarageOS.Application.Validators.Servicos;
using GarageOS.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GarageOS.Api.Controllers;

/// <summary>Controller de gerenciamento de serviços</summary>
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ServicosController : ControllerBase
{
    private readonly ListarServicosUseCase _listarServicosUseCase;
    private readonly CadastrarServicoUseCase _cadastrarServicoUseCase;
    private readonly ObterServicoUseCase _obterServicoUseCase;
    private readonly AlterarServicoUseCase _alterarServicoUseCase;

    /// <summary>Inicializa o controller com os use cases de serviços</summary>
    public ServicosController(
        ListarServicosUseCase listarServicosUseCase,
        CadastrarServicoUseCase cadastrarServicoUseCase,
        ObterServicoUseCase obterServicoUseCase,
        AlterarServicoUseCase alterarServicoUseCase)
    {
        _listarServicosUseCase = listarServicosUseCase;
        _cadastrarServicoUseCase = cadastrarServicoUseCase;
        _obterServicoUseCase = obterServicoUseCase;
        _alterarServicoUseCase = alterarServicoUseCase;
    }

    /// <summary>Lista todos os serviços cadastrados</summary>
    /// <remarks>
    /// Retorna uma lista de todos os serviços disponíveis no sistema.
    /// Cada serviço inclui: ID, nome e valor/preço unitário.
    /// Serviços são usados em Ordens de Serviço para orçamento e execução.
    /// </remarks>
    /// <returns>Lista completa de serviços</returns>
    /// <response code="200">Lista retornada com sucesso</response>
    /// <response code="401">Não autorizado - token JWT ausente ou inválido</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ServicoResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar()
    {
        var resultado = await _listarServicosUseCase.ExecutarAsync();
        return Ok(resultado);
    }

    /// <summary>Cadastra um novo serviço</summary>
    /// <remarks>
    /// Cria um novo serviço que poderá ser adicionado às Ordens de Serviço.
    /// O serviço deve ter um nome único e um preço válido (maior que zero).
    ///
    /// O preço do serviço é usado para:
    /// - Calcular o valor total do orçamento quando adicionado a uma OS
    /// - Registrar histórico de preços praticados
    ///
    /// Validações:
    /// - Nome: obrigatório, máximo 100 caracteres, único
    /// - Preço: obrigatório, maior que zero, máximo 2 casas decimais
    /// </remarks>
    /// <param name="request">Dados do novo serviço (NomeServico, Preco)</param>
    /// <returns>Serviço criado com sucesso</returns>
    /// <response code="201">Serviço criado com sucesso</response>
    /// <response code="400">Dados inválidos ou serviço com mesmo nome já existe</response>
    /// <response code="401">Não autorizado - token JWT ausente ou inválido</response>
    [HttpPost]
    [ProducesResponseType(typeof(ServicoResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Cadastrar([FromBody] CriarServicoRequest request)
    {
        var validator = new CriarServicoValidator();
        var validation = await validator.ValidateAsync(request);

        if (!validation.IsValid)
            return BadRequest(validation.Errors.Select(e => e.ErrorMessage));

        var resultado = await _cadastrarServicoUseCase.ExecutarAsync(request);
        return CreatedAtAction(nameof(Obter), new { id = resultado.Id }, resultado);
    }

    /// <summary>Obtém um serviço específico pelo ID</summary>
    /// <remarks>
    /// Retorna os dados completos de um serviço incluindo seu nome e preço.
    /// Útil para verificar o preço atual de um serviço antes de adicionar a uma OS.
    /// </remarks>
    /// <param name="id">ID único do serviço (GUID)</param>
    /// <returns>Dados completos do serviço</returns>
    /// <response code="200">Serviço encontrado - dados retornados com sucesso</response>
    /// <response code="404">Serviço não encontrado</response>
    /// <response code="401">Não autorizado - token JWT ausente ou inválido</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ServicoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obter(Guid id)
    {
        try
        {
            var resultado = await _obterServicoUseCase.ExecutarAsync(id);
            return Ok(resultado);
        }
        catch (ServicoNaoEncontradoException ex)
        {
            return NotFound(new { mensagem = ex.Message });
        }
    }

    /// <summary>Atualiza os dados de um serviço existente</summary>
    /// <remarks>
    /// Permite alterar o nome e/ou preço de um serviço já cadastrado.
    /// Todas as Ordens de Serviço que usam este serviço considerarão o novo preço
    /// apenas para orçamentos futuros. Orçamentos já gerados não são recalculados.
    /// </remarks>
    /// <param name="id">ID único do serviço a atualizar (GUID)</param>
    /// <param name="request">Novos dados do serviço (NomeServico, Preco)</param>
    /// <returns>Serviço atualizado com os novos dados</returns>
    /// <response code="200">Serviço atualizado com sucesso</response>
    /// <response code="400">Dados inválidos</response>
    /// <response code="404">Serviço não encontrado</response>
    /// <response code="401">Não autorizado - token JWT ausente ou inválido</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ServicoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Alterar(Guid id, [FromBody] AtualizarServicoRequest request)
    {
        var validator = new AtualizarServicoValidator();
        var validation = await validator.ValidateAsync(request);

        if (!validation.IsValid)
            return BadRequest(validation.Errors.Select(e => e.ErrorMessage));

        try
        {
            var resultado = await _alterarServicoUseCase.ExecutarAsync(id, request);
            return Ok(resultado);
        }
        catch (ServicoNaoEncontradoException ex)
        {
            return NotFound(new { mensagem = ex.Message });
        }
    }
}

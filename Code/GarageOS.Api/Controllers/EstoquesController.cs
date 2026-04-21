using GarageOS.Application.DTOs.Estoques;
using GarageOS.Application.UseCases.Estoques;
using GarageOS.Application.Validators.Estoques;
using GarageOS.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GarageOS.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class EstoquesController : ControllerBase
{
    private readonly ListarEstoquesUseCase _listarEstoquesUseCase;
    private readonly CadastrarEstoqueUseCase _cadastrarEstoqueUseCase;
    private readonly ObterEstoqueUseCase _obterEstoqueUseCase;
    private readonly AlterarEstoqueUseCase _alterarEstoqueUseCase;
    private readonly DeletarEstoqueUseCase _deletarEstoqueUseCase;

    public EstoquesController(
        ListarEstoquesUseCase listarEstoquesUseCase,
        CadastrarEstoqueUseCase cadastrarEstoqueUseCase,
        ObterEstoqueUseCase obterEstoqueUseCase,
        AlterarEstoqueUseCase alterarEstoqueUseCase,
        DeletarEstoqueUseCase deletarEstoqueUseCase)
    {
        _listarEstoquesUseCase = listarEstoquesUseCase;
        _cadastrarEstoqueUseCase = cadastrarEstoqueUseCase;
        _obterEstoqueUseCase = obterEstoqueUseCase;
        _alterarEstoqueUseCase = alterarEstoqueUseCase;
        _deletarEstoqueUseCase = deletarEstoqueUseCase;
    }

    /// <summary>Lista todos os itens do estoque.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<EstoqueResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar()
    {
        var resultado = await _listarEstoquesUseCase.ExecutarAsync();
        return Ok(resultado);
    }

    /// <summary>Cadastra um novo item no estoque.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(EstoqueResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Cadastrar([FromBody] CriarEstoqueRequest request)
    {
        var validator = new CriarEstoqueValidator();
        var validation = await validator.ValidateAsync(request);

        if (!validation.IsValid)
            return BadRequest(validation.Errors.Select(e => e.ErrorMessage));

        var resultado = await _cadastrarEstoqueUseCase.ExecutarAsync(request);
        return CreatedAtAction(nameof(Obter), new { id = resultado.Id }, resultado);
    }

    /// <summary>Obtém um item do estoque pelo ID.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(EstoqueResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obter(Guid id)
    {
        try
        {
            var resultado = await _obterEstoqueUseCase.ExecutarAsync(id);
            return Ok(resultado);
        }
        catch (EstoqueNaoEncontradoException ex)
        {
            return NotFound(new { mensagem = ex.Message });
        }
    }

    /// <summary>Remove um item do estoque.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deletar(Guid id)
    {
        try
        {
            await _deletarEstoqueUseCase.ExecutarAsync(id);
            return NoContent();
        }
        catch (EstoqueNaoEncontradoException ex)
        {
            return NotFound(new { mensagem = ex.Message });
        }
    }

    /// <summary>Altera os dados de um item do estoque.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(EstoqueResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Alterar(Guid id, [FromBody] AtualizarEstoqueRequest request)
    {
        var validator = new AtualizarEstoqueValidator();
        var validation = await validator.ValidateAsync(request);

        if (!validation.IsValid)
            return BadRequest(validation.Errors.Select(e => e.ErrorMessage));

        try
        {
            var resultado = await _alterarEstoqueUseCase.ExecutarAsync(id, request);
            return Ok(resultado);
        }
        catch (EstoqueNaoEncontradoException ex)
        {
            return NotFound(new { mensagem = ex.Message });
        }
    }
}

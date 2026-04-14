using GarageOS.Application.DTOs.Servicos;
using GarageOS.Application.UseCases.Servicos;
using GarageOS.Application.Validators.Servicos;
using GarageOS.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace GarageOS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ServicosController : ControllerBase
{
    private readonly ListarServicosUseCase _listarServicosUseCase;
    private readonly CadastrarServicoUseCase _cadastrarServicoUseCase;
    private readonly ObterServicoUseCase _obterServicoUseCase;
    private readonly AlterarServicoUseCase _alterarServicoUseCase;

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

    /// <summary>Lista todos os serviços cadastrados.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ServicoResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar()
    {
        var resultado = await _listarServicosUseCase.ExecutarAsync();
        return Ok(resultado);
    }

    /// <summary>Cadastra um novo serviço.</summary>
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

    /// <summary>Obtém um serviço pelo ID.</summary>
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

    /// <summary>Altera os dados de um serviço existente.</summary>
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

using GarageOS.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GarageOS.Application.DTOs.Veiculos;
using GarageOS.Application.UseCases.Veiculos;
using GarageOS.Application.Validators.Veiculos;

namespace GarageOS.Api.Controllers;

/// <summary>Controller de gerenciamento de veículos</summary>
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class VeiculosController : ControllerBase
{
    private readonly CriarVeiculoValidator _validator;
    private readonly ListarVeiculosUseCase _listarVeiculosUseCase;
    private readonly ObterVeiculoUseCase _obterVeiculoUseCase;
    private readonly CadastrarVeiculoUseCase _cadastrarVeiculoUseCase;
    private readonly AlterarVeiculoUseCase _alterarVeiculoUseCase;
    private readonly VincularVeiculoClienteUseCase _vincularVeiculoClienteUseCase;
    private readonly DeletarVeiculoUseCase _deletarVeiculoUseCase;

    /// <summary>Inicializa o controller com os use cases de veículos</summary>
    public VeiculosController(
        CriarVeiculoValidator validator,
        ListarVeiculosUseCase listarVeiculosUseCase,
        ObterVeiculoUseCase obterVeiculoUseCase,
        CadastrarVeiculoUseCase cadastrarVeiculoUseCase,
        AlterarVeiculoUseCase alterarVeiculoUseCase,
        VincularVeiculoClienteUseCase vincularVeiculoClienteUseCase,
        DeletarVeiculoUseCase deletarVeiculoUseCase
        )
    {
        _validator = validator;
        _listarVeiculosUseCase = listarVeiculosUseCase;
        _obterVeiculoUseCase = obterVeiculoUseCase;
        _cadastrarVeiculoUseCase = cadastrarVeiculoUseCase;
        _alterarVeiculoUseCase = alterarVeiculoUseCase;
        _vincularVeiculoClienteUseCase = vincularVeiculoClienteUseCase;
        _deletarVeiculoUseCase = deletarVeiculoUseCase;
    }

    /// <summary>Lista todos os veículos cadastrados</summary>
    /// <remarks>
    /// Retorna uma lista de todos os veículos cadastrados no sistema.
    /// Cada veículo inclui: ID, placa, marca, modelo, ano, cliente vinculado e datas de auditoria.
    /// A placa é um identificador único e é validada quanto ao formato padrão brasileiro.
    /// </remarks>
    /// <returns>Lista completa de veículos</returns>
    /// <response code="200">Lista retornada com sucesso</response>
    /// <response code="401">Não autorizado - token JWT ausente ou inválido</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<VeiculoResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar()
    {
        var resultado = await _listarVeiculosUseCase.ExecutarAsync();
        return Ok(resultado);
    }

    /// <summary>Obtém um veículo específico pelo ID</summary>
    /// <remarks>
    /// Retorna os dados completos de um veículo incluindo:
    /// - Dados básicos (placa, marca, modelo, ano)
    /// - Cliente vinculado (se houver)
    /// - Histórico de datas de criação e atualização
    /// </remarks>
    /// <param name="id">ID único do veículo (GUID)</param>
    /// <returns>Dados completos do veículo</returns>
    /// <response code="200">Veículo encontrado - dados retornados com sucesso</response>
    /// <response code="404">Veículo não encontrado</response>
    /// <response code="401">Não autorizado - token JWT ausente ou inválido</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(VeiculoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obter(Guid id)
    {
        try
        {
            var resultado = await _obterVeiculoUseCase.ExecutarAsync(id);
            return Ok(resultado);
        }
        catch (VeiculoNaoEncontradoException ex)
        {
            return NotFound(new { mensagem = ex.Message });
        }
    }

    /// <summary>Cadastra um novo veículo</summary>
    /// <remarks>
    /// Cria um novo veículo no sistema com dados validados.
    /// A placa deve estar em formato válido (padrão brasileiro) e ser única no sistema.
    ///
    /// Validações:
    /// - Placa: obrigatória, formato brasileiro válido (ex: ABC-1234), única
    /// - Marca: obrigatória, máximo 50 caracteres
    /// - Modelo: obrigatório, máximo 50 caracteres
    /// - Ano: obrigatório, deve ser numérico válido
    ///
    /// Nota: Um veículo é criado sem cliente vinculado inicialmente.
    /// Use o endpoint de vincular para associar a um cliente.
    /// </remarks>
    /// <param name="request">Dados do novo veículo (Placa, Marca, Modelo, Ano)</param>
    /// <returns>Veículo criado com sucesso</returns>
    /// <response code="201">Veículo criado com sucesso</response>
    /// <response code="400">Dados inválidos ou placa já existe</response>
    /// <response code="401">Não autorizado - token JWT ausente ou inválido</response>
    [HttpPost]
    [ProducesResponseType(typeof(VeiculoResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Cadastrar([FromBody] CriarVeiculoRequest request)
    {
        var validation = await _validator.ValidateAsync(request);

        if (!validation.IsValid)
            return BadRequest(validation.Errors.Select(e => e.ErrorMessage));

        var resultado = await _cadastrarVeiculoUseCase.ExecutarAsyncCadastrarVeiculo(request);
        return CreatedAtAction(nameof(Obter), new { id = resultado.Id }, resultado);
    }

    /// <summary>Atualiza os dados de um veículo existente</summary>
    /// <remarks>
    /// Permite alterar os dados cadastrais de um veículo.
    /// A placa é um identificador único e não pode ser alterada para uma placa já existente.
    ///
    /// Campos atualizáveis:
    /// - Placa (com validação de unicidade)
    /// - Marca
    /// - Modelo
    /// - Ano
    /// </remarks>
    /// <param name="id">ID único do veículo a atualizar (GUID)</param>
    /// <param name="request">Novos dados do veículo</param>
    /// <returns>Veículo atualizado com os novos dados</returns>
    /// <response code="200">Veículo atualizado com sucesso</response>
    /// <response code="400">Dados inválidos ou nova placa já existe</response>
    /// <response code="404">Veículo não encontrado</response>
    /// <response code="401">Não autorizado - token JWT ausente ou inválido</response>
    [HttpPatch("{id:guid}")]
    [ProducesResponseType(typeof(VeiculoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Alterar(Guid id, [FromBody] AtualizarVeiculoRequest request)
    {
        try
        {
            var resultado = await _alterarVeiculoUseCase.ExecutarAsyncAlterarVeiculo(id, request);
            return Ok(resultado);
        }
        catch (VeiculoNaoEncontradoException ex)
        {
            return NotFound(new { mensagem = ex.Message });
        }
    }

    /// <summary>Vincula um veículo a um cliente</summary>
    /// <remarks>
    /// Associa um veículo a um cliente específico.
    /// Um veículo pode estar vinculado a apenas um cliente por vez.
    /// Esta vinculação é necessária para criar Ordens de Serviço.
    ///
    /// Cenários de uso:
    /// - Um novo veículo é criado sem cliente
    /// - Este endpoint vincula o veículo ao cliente proprietário
    /// - Agora o veículo pode ter Ordens de Serviço criadas
    /// </remarks>
    /// <param name="id">ID único do veículo (GUID)</param>
    /// <param name="request">ID do cliente a vincular</param>
    /// <returns>Nenhum conteúdo (operação bem-sucedida)</returns>
    /// <response code="204">Veículo vinculado com sucesso - sem conteúdo na resposta</response>
    /// <response code="404">Veículo ou cliente não encontrado</response>
    /// <response code="401">Não autorizado - token JWT ausente ou inválido</response>
    [HttpPatch("{id:guid}/vincular-cliente")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> VincularAoCliente(Guid id, [FromBody] VincularClienteRequest request)
    {
        try
        {
            await _vincularVeiculoClienteUseCase.ExecutarAsync(id, request.ClienteId);
            return NoContent();
        }
        catch (VeiculoNaoEncontradoException ex)
        {
            return NotFound(new { mensagem = ex.Message });
        }
    }

    /// <summary>Remove um veículo do sistema</summary>
    /// <remarks>
    /// Deleta um veículo permanentemente do sistema.
    /// Verifique se o veículo não possui Ordens de Serviço ativas antes de deletar.
    ///
    /// AVISO: Esta é uma operação irreversível.
    /// </remarks>
    /// <param name="id">ID único do veículo a remover (GUID)</param>
    /// <returns>Nenhum conteúdo (operação bem-sucedida)</returns>
    /// <response code="204">Veículo removido com sucesso - sem conteúdo na resposta</response>
    /// <response code="404">Veículo não encontrado</response>
    /// <response code="401">Não autorizado - token JWT ausente ou inválido</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deletar(Guid id)
    {
        var sucesso = await _deletarVeiculoUseCase.ExecutarAsync(id);

        if (!sucesso)
            return NotFound();

        return NoContent();
    }
}
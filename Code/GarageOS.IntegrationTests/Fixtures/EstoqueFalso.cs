using System.Collections.Concurrent;
using GarageOS.Application.Abstractions;

namespace GarageOS.IntegrationTests.Fixtures;

/// <summary>Substitui o Execução Service nos testes do OS</summary>
/// <remarks>
/// O OS consulta o estoque por HTTP em outro serviço. Aqui a resposta vem de
/// um dicionário que cada teste preenche com as peças de que precisa.
/// </remarks>
public class EstoqueFalso : IEstoqueClient
{
    private readonly ConcurrentDictionary<Guid, PecaDoEstoque> _pecas = new();

    public PecaDoEstoque Cadastrar(string nome, decimal valor, int disponivel)
    {
        var peca = new PecaDoEstoque(Guid.NewGuid(), nome, valor, disponivel);
        _pecas[peca.Id] = peca;
        return peca;
    }

    public Task<PecaDoEstoque?> ObterPecaAsync(Guid estoqueId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_pecas.TryGetValue(estoqueId, out var peca) ? peca : null);
}

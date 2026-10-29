using GarageOS.Application.DTOs.OrdensDeServico;
using GarageOS.Domain.Entities;

namespace GarageOS.Application.Mappers;

/// <summary>Conversão da OS em resposta da API, num lugar só</summary>
/// <remarks>
/// Até a Fase 3 o mesmo MapearParaResponse estava copiado em sete use cases, e
/// cada campo novo precisava ser lembrado em todos.
/// </remarks>
public static class OrdemDeServicoMapper
{
    public static OrdemDeServicoResponse ParaResponse(this OrdemDeServico os) => new()
    {
        Id = os.Id,
        NumeroOS = os.NumeroOS,
        Status = os.Status,
        CriadoEm = os.CriadoEm,
        FinalizadaEm = os.FinalizadaEm,
        AtualizadoEm = os.AtualizadoEm,
        ClienteId = os.ClienteId,
        VeiculoId = os.VeiculoId,
        ValorEstimado = os.ValorEstimado,
        Servicos = os.Servicos.Select(s => new ServicoItemResponse
        {
            Id = s.Id,
            ServicoId = s.ServicoId,
            ServicoNome = s.Nome,
            PrecoUnitario = s.PrecoUnitario,
            CriadoEm = s.CriadoEm
        }).ToList(),
        Pecas = os.Pecas.Select(p => new PecaItemResponse
        {
            Id = p.Id,
            EstoqueId = p.EstoqueId,
            Nome = p.Nome,
            ValorUnitario = p.ValorUnitario,
            Quantidade = p.Quantidade,
            Subtotal = p.Subtotal
        }).ToList()
    };

    public static IEnumerable<HistoricoStatusResponse> HistoricoParaResponse(this OrdemDeServico os) =>
        os.Historico
            .OrderBy(h => h.OcorridoEm)
            .Select(h => new HistoricoStatusResponse
            {
                StatusAnterior = h.StatusAnterior?.ToString(),
                StatusNovo = h.StatusNovo.ToString(),
                Origem = h.Origem,
                CorrelationId = h.CorrelationId,
                OcorridoEm = h.OcorridoEm
            });
}

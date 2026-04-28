using GarageOS.Application.DTOs.OrdensDeServico;
using GarageOS.Domain.Repositories;

namespace GarageOS.Application.UseCases.OrdensDeServico;

public class CalcularAgingServicosUseCase
{
    private readonly IOrdemDeServicoRepository _repository;

    public CalcularAgingServicosUseCase(IOrdemDeServicoRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<AgingServicoResponse>> ExecutarAsync()
    {
        var servicos = await _repository.ObterServicosFinalizadosAsync();

        return servicos
            .GroupBy(s => new { s.ServicoId, Nome = s.Servico?.NomeServico ?? string.Empty })
            .Select(g =>
            {
                var tempoMedioMinutos = g
                    .Average(s => (s.FinalizadaEm!.Value - s.IniciadaEm!.Value).TotalMinutes);

                return new AgingServicoResponse
                {
                    ServicoId = g.Key.ServicoId,
                    ServicoNome = g.Key.Nome,
                    TotalExecucoes = g.Count(),
                    TempoMedioMinutos = Math.Round(tempoMedioMinutos, 2),
                    TempoMedioFormatado = FormatarTempo(tempoMedioMinutos)
                };
            })
            .OrderBy(r => r.ServicoNome)
            .ToList();
    }

    private static string FormatarTempo(double minutos)
    {
        if (minutos < 1)
            return $"{(int)(minutos * 60)}s";

        if (minutos < 60)
            return $"{(int)minutos}min";

        var horas = (int)(minutos / 60);
        var min = (int)(minutos % 60);
        return min > 0 ? $"{horas}h {min}min" : $"{horas}h";
    }
}

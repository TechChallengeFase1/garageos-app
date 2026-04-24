namespace GarageOS.Application.DTOs.OrdensDeServico;

public class CriarOrdemDeServicoRequest
{
    public Guid ClienteId { get; set; }
    public Guid VeiculoId { get; set; }
}

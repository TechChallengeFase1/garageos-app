using GarageOS.Domain.Enums;

namespace GarageOS.Application.DTOs.OrdensDeServico;

public class AlterarStatusRequest
{
    public StatusOrdemDeServico Status { get; set; }
}

using LogisticsHub.Results;
using MediatR;

namespace LogisticsHub.ShipmentService.Application.Shipments;

public sealed record CreateShipmentCommand(
    IReadOnlyCollection<CreateShipmentItemCommand> Items,
    Guid? SenderCompanyId,
    Guid? SenderAddressId,
    Guid? ReceiverCompanyId,
    Guid? ReceiverAddressId) : IRequest<Result<CreateShipmentResult>>
{
    public bool HasCompanyAddressReferences =>
        SenderCompanyId.HasValue
        && SenderAddressId.HasValue
        && ReceiverCompanyId.HasValue
        && ReceiverAddressId.HasValue;
}

using VTCStockManagementCase.Domain.Enums;

namespace VTCStockManagementCase.Domain;

public static class OrderStatusRules
{
    public static bool CanTransition(OrderStatus from, OrderStatus to)
    {
        return (from, to) switch
        {
            (OrderStatus.Pending, OrderStatus.Approved) => true,
            (OrderStatus.Approved, OrderStatus.Completed) => true,
            (OrderStatus.Pending, OrderStatus.Failed) => true,
            (OrderStatus.Pending, OrderStatus.Cancelled) => true,
            _ => false
        };
    }
}

using VTCStockManagementCase.Domain;
using VTCStockManagementCase.Domain.Enums;

namespace VTCStockManagementCase.UnitTests;

public class OrderStatusRulesTests
{
    [Theory]
    [InlineData(OrderStatus.Pending, OrderStatus.Approved, true)]
    [InlineData(OrderStatus.Approved, OrderStatus.Completed, true)]
    [InlineData(OrderStatus.Pending, OrderStatus.Failed, true)]
    [InlineData(OrderStatus.Pending, OrderStatus.Cancelled, true)]
    [InlineData(OrderStatus.Completed, OrderStatus.Pending, false)]
    [InlineData(OrderStatus.Failed, OrderStatus.Completed, false)]
    public void CanTransition_matches_spec(OrderStatus from, OrderStatus to, bool expected)
    {
        Assert.Equal(expected, OrderStatusRules.CanTransition(from, to));
    }
}

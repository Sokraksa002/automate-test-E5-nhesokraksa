using System.Linq;
using Moq;
using SmartPark.Core.Interfaces;
using SmartPark.Core.Models;
using SmartPark.Core.Services;

namespace SmartPark.Tests.IntegrationTests;

public class ParkingFlowIntegrationTests
{
    // ────────────────────────────────────────────────────────────
    //  INTEGRATION TEST SETUP
    //  Uses REAL components for business logic, and TEST DOUBLES
    //  only for external boundaries:
    //
    //  Real objects:
    //    ParkingFeeCalculator       — real (pure logic, no side effects)
    //    InMemoryParkingRepository  — fake (working in-memory implementation)
    //
    //  Test doubles (via Moq, used as stubs here):
    //    IPaymentGateway            — stub (always returns success)
    //    INotificationService       — stub (does nothing)
    //    IDateTimeProvider          — stub (returns controlled time)
    //    IMembershipService         — stub (returns Guest for all)
    // ────────────────────────────────────────────────────────────

    private readonly ParkingFeeCalculator _feeCalculator = new();
    private readonly InMemoryParkingRepository _repository = new();  // fake
    private readonly Mock<IPaymentGateway> _paymentStub = new();
    private readonly Mock<INotificationService> _notificationStub = new();
    private readonly ParkingSessionManager _manager;

    // Fake clock — set this in each test to control time
    private DateTime _currentTime = new(2026, 3, 16, 10, 0, 0); // Monday 10 AM

    public ParkingFlowIntegrationTests()
    {
        var dateTimeStub = new Mock<IDateTimeProvider>();
        dateTimeStub.Setup(d => d.Now).Returns(() => _currentTime);

        var membershipStub = new Mock<IMembershipService>();
        membershipStub.Setup(m => m.GetMembershipTier(It.IsAny<string>())).Returns(MembershipTier.Guest);

        _paymentStub.Setup(p => p.ProcessPaymentAsync(It.IsAny<string>(), It.IsAny<decimal>()))
            .ReturnsAsync(true);

        _manager = new ParkingSessionManager(
            _feeCalculator,
            _paymentStub.Object,
            _notificationStub.Object,
            membershipStub.Object,
            _repository,          // real fake, not a Moq object
            dateTimeStub.Object);
    }

    // ────────────────────────────────────────────────────────────
    //  EXAMPLE TEST — shows how to advance time between operations.
    //  Delete or keep this; it does not count toward your grade.
    // ────────────────────────────────────────────────────────────

    [Fact]
    public async Task FullFlow_CheckInAndCheckOut_CalculatesCorrectFee()
    {
        // Arrange — check in at 10:00 AM
        _currentTime = new DateTime(2026, 3, 16, 10, 0, 0); // Monday
        var ticket = await _manager.CheckInAsync("TEST-001", VehicleType.Car);

        // Act — check out at 12:30 PM (2.5 hours later → 2 billable hours after grace)
        _currentTime = new DateTime(2026, 3, 16, 12, 30, 0);
        var result = await _manager.CheckOutAsync(ticket.TicketId, "012-345-678");

        // Assert — Car: 2 hours × 1,000 = 2,000 KHR
        Assert.Equal(2_000m, result.TotalFee);
    }

    #region Full Parking Flow
    // End-to-end scenarios from check-in through check-out

    [Fact]
	public async Task LostTicketDuringGracePeriod_Returns20000AndClosesTicket()
	{
    	var ticket = await _manager.CheckInAsync("CAR1", VehicleType.Car);

    	_currentTime = _currentTime.AddMinutes(10);

    	var result = await _manager.CheckOutAsync(ticket.TicketId, "111", true, false);

    	Assert.Equal(20000m, result.TotalFee);

    	var active = await _repository.GetAllActiveTicketsAsync();
    	Assert.Empty(active);
	}

    #endregion

    #region Multiple Vehicles
    // Test concurrent parking sessions and their lifecycle

    [Fact]
	public async Task MultipleVehicles_CheckIn3_CheckOut1_Remain2Active()
	{
    	var t1 = await _manager.CheckInAsync("CAR1", VehicleType.Car);
    	await _manager.CheckInAsync("CAR2", VehicleType.Car);
    	await _manager.CheckInAsync("CAR3", VehicleType.Car);

    	_currentTime = _currentTime.AddHours(2);

    	await _manager.CheckOutAsync(t1.TicketId, "111", false, false);

    	var active = await _repository.GetAllActiveTicketsAsync();

    	Assert.Equal(2, active.Count());
	}

    #endregion

    #region Error Recovery
    // Test system state consistency after error conditions

    [Fact]
	public async Task DuplicateCheckIn_ThrowsException_NoNewTicket()
	{
    	await _manager.CheckInAsync("CAR1", VehicleType.Car);

    	await Assert.ThrowsAsync<InvalidOperationException>(() =>
        	_manager.CheckInAsync("CAR1", VehicleType.Car)
    	);
	}

	[Fact]
	public async Task PaymentFailure_CheckOut_ThrowsException_TicketStillActive()
	{
    	_paymentStub.Setup(p =>
        	p.ProcessPaymentAsync(It.IsAny<string>(), It.IsAny<decimal>())
    	).ReturnsAsync(false);

    	var ticket = await _manager.CheckInAsync("CAR1", VehicleType.Car);

    	_currentTime = _currentTime.AddHours(2);

    	await Assert.ThrowsAsync<Exception>(() =>
        	_manager.CheckOutAsync(ticket.TicketId, "111", false, false)
    	);

    	var active = await _repository.GetAllActiveTicketsAsync();

    	Assert.Single(active);
	}

    #endregion

    #region Edge-to-Edge Scenarios
    // Test complex combinations of fee modifiers working together

    [Fact]
	public async Task NotificationFailure_CheckOutStillSucceeds()
	{
    	_notificationStub.Setup(n =>
        	n.SendReceiptAsync(It.IsAny<string>(), It.IsAny<string>())
    	).ThrowsAsync(new Exception());

    	var ticket = await _manager.CheckInAsync("CAR1", VehicleType.Car);

    	_currentTime = _currentTime.AddHours(2);

    	var result = await _manager.CheckOutAsync(ticket.TicketId, "111", false, false);

    	Assert.NotNull(result);

    	var active = await _repository.GetAllActiveTicketsAsync();

    	Assert.Empty(active);
	}


    #endregion
}

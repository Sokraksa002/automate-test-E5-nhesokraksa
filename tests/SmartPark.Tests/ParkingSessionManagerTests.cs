using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Moq;
using SmartPark.Core.Interfaces;
using SmartPark.Core.Models;
using SmartPark.Core.Services;

namespace SmartPark.Tests;

public class ParkingSessionManagerTests
{
    private readonly Mock<IPaymentGateway> _paymentStub = new();
    private readonly Mock<INotificationService> _notificationStub = new();
    private readonly Mock<IMembershipService> _membershipStub = new();
    private readonly Mock<IParkingRepository> _repoStub = new();
    private readonly Mock<IDateTimeProvider> _dateTimeStub = new();
    private readonly ParkingFeeCalculator _feeCalculator = new();
    private readonly ParkingSessionManager _manager;

    public ParkingSessionManagerTests()
    {
        _manager = new ParkingSessionManager(
            _feeCalculator,
            _paymentStub.Object,
            _notificationStub.Object,
            _membershipStub.Object,
            _repoStub.Object,
            _dateTimeStub.Object
        );
    }

    // Example (keep)
    [Fact]
    public async Task CheckInAsync_NewVehicle_LookUpMembership()
    {
        _membershipStub.Setup(m => m.GetMembershipTier("PP-9999"))
                       .Returns(MembershipTier.Guest);

        _repoStub.Setup(r => r.GetActiveTicketByPlateAsync("PP-9999"))
                 .ReturnsAsync((ParkingTicket?)null);

        _dateTimeStub.Setup(d => d.Now)
                     .Returns(new DateTime(2026, 3, 16, 10, 0, 0));

        var ticket = await _manager.CheckInAsync("PP-9999", VehicleType.Car);

        _membershipStub.Verify(m => m.GetMembershipTier("PP-9999"), Times.Once);
        Assert.Equal("PP-9999", ticket.Vehicle.LicensePlate);
    }

    // SCENARIO 1 — Duplicate check-in
    [Fact]
    public async Task CheckIn_DuplicateCheckIn_ThrowsException_SaveNotCalled()
    {
        _repoStub.Setup(r => r.GetActiveTicketByPlateAsync("ABC123"))
                 .ReturnsAsync(new ParkingTicket());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _manager.CheckInAsync("ABC123", VehicleType.Car)
        );

        _repoStub.Verify(r => r.SaveTicketAsync(It.IsAny<ParkingTicket>()), Times.Never);
    }

    // SCENARIO 2 — Successful checkout
    [Fact]
    public async Task CheckOut_Successful_Checkout_AllStepsExecuted()
    {
        var ticket = new ParkingTicket
        {
            CheckInTime = DateTime.Now.AddHours(-2),
            Vehicle = new Vehicle
            {
                LicensePlate = "ABC123"
            }
        };

        _repoStub.Setup(r => r.GetTicketByIdAsync("ABC123"))
                 .ReturnsAsync(ticket);

        // FIX: always use flexible match
        _paymentStub.Setup(p =>
            p.ProcessPaymentAsync(It.IsAny<string>(), It.IsAny<decimal>())
        ).ReturnsAsync(true);

        _dateTimeStub.Setup(d => d.Now).Returns(DateTime.Now);

        var result = await _manager.CheckOutAsync(
            "ABC123",
            "012345678",
            false,
            false
        );

        Assert.NotNull(result);

        _paymentStub.Verify(p =>
            p.ProcessPaymentAsync(It.IsAny<string>(), It.IsAny<decimal>()),
            Times.Once
        );

        _repoStub.Verify(r => r.UpdateTicketAsync(It.IsAny<ParkingTicket>()), Times.Once);

        _notificationStub.Verify(n =>
            n.SendReceiptAsync(It.IsAny<string>(), It.IsAny<string>()),
            Times.Once
        );
    }

    //SCENARIO 3 — Payment failure
    [Fact]
    public async Task CheckOut_PaymentFails_ThrowsException_UpdateNotCalled()
    {
        var ticket = new ParkingTicket
        {
            CheckInTime = DateTime.Now.AddHours(-2),
            Vehicle = new Vehicle
            {
                LicensePlate = "ABC123"
            }
        };

        _repoStub.Setup(r => r.GetTicketByIdAsync("ABC123"))
                 .ReturnsAsync(ticket);

        _paymentStub.Setup(p =>
            p.ProcessPaymentAsync(It.IsAny<string>(), It.IsAny<decimal>())
        ).ReturnsAsync(false);

        _dateTimeStub.Setup(d => d.Now).Returns(DateTime.Now);

        await Assert.ThrowsAsync<Exception>(() =>
            _manager.CheckOutAsync("ABC123", "012345678", false, false)
        );

        _repoStub.Verify(r => r.UpdateTicketAsync(It.IsAny<ParkingTicket>()), Times.Never);
    }

    // SCENARIO 4 — Notification failure
    [Fact]
    public async Task CheckOut_NotificationFails_StillSucceeds()
    {
        var ticket = new ParkingTicket
        {
            CheckInTime = DateTime.Now.AddHours(-2),
            Vehicle = new Vehicle
            {
                LicensePlate = "ABC123"
            }
        };

        _repoStub.Setup(r => r.GetTicketByIdAsync("ABC123"))
                 .ReturnsAsync(ticket);

        _paymentStub.Setup(p =>
            p.ProcessPaymentAsync(It.IsAny<string>(), It.IsAny<decimal>())
        ).ReturnsAsync(true);

        _notificationStub.Setup(n =>
            n.SendReceiptAsync(It.IsAny<string>(), It.IsAny<string>())
        ).ThrowsAsync(new Exception());

        _dateTimeStub.Setup(d => d.Now).Returns(DateTime.Now);

        var result = await _manager.CheckOutAsync(
            "ABC123",
            "012345678",
            false,
            false
        );

        Assert.NotNull(result);

        _paymentStub.Verify(p =>
            p.ProcessPaymentAsync(It.IsAny<string>(), It.IsAny<decimal>()),
            Times.Once
        );

        _repoStub.Verify(r => r.UpdateTicketAsync(It.IsAny<ParkingTicket>()), Times.Once);
    }

    // SCENARIO 5 — Ticket not found
    [Fact]
    public async Task CheckOut_TicketNotFound_ThrowsKeyNotFound()
    {
        _repoStub.Setup(r => r.GetTicketByIdAsync("X"))
                 .ReturnsAsync((ParkingTicket?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _manager.CheckOutAsync("X", "012345678", false, false)
        );

        _paymentStub.Verify(p =>
            p.ProcessPaymentAsync(It.IsAny<string>(), It.IsAny<decimal>()),
            Times.Never
        );
    }

    // SCENARIO 6 — Already checked out
    [Fact]
    public async Task CheckOut_AlreadyCheckedOut_ThrowsException_NoPayment()
    {
        var ticket = new ParkingTicket
        {
            CheckInTime = DateTime.Now.AddHours(-2),
            CheckOutTime = DateTime.Now,
            Vehicle = new Vehicle
            {
                LicensePlate = "ABC123"
            }
        };

        _repoStub.Setup(r => r.GetTicketByIdAsync("ABC123"))
                 .ReturnsAsync(ticket);

        _dateTimeStub.Setup(d => d.Now).Returns(DateTime.Now);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _manager.CheckOutAsync("ABC123", "012345678", false, false)
        );

        _paymentStub.Verify(p =>
            p.ProcessPaymentAsync(It.IsAny<string>(), It.IsAny<decimal>()),
            Times.Never
        );
    }
}
using SmartPark.Core.Models;
using SmartPark.Core.Services;
using FsCheck;
using FsCheck.Xunit;

namespace SmartPark.Tests;

public class ParkingFeeCalculatorTests
{
    private readonly ParkingFeeCalculator _calculator = new();

    // ========================================================
    #region Basic Cases
    // ========================================================

    [Fact]
    public void CalculateFee_ZeroDuration_ReturnsFree()
    {
        var checkIn = new DateTime(2026, 3, 16, 10, 0, 0);
        var checkOut = checkIn;

        var result = _calculator.CalculateFee(
            VehicleType.Car,
            MembershipTier.Guest,
            checkIn,
            checkOut
        );

        Assert.Equal(0m, result.TotalFee);
    }

    #endregion

    // ========================================================
    #region Grace Period
    // ========================================================

    [Fact]
    public void CalculateFee_GracePeriod_30Minutes_ReturnsFree()
    {
        var checkIn = new DateTime(2026, 3, 16, 10, 0, 0);
        var checkOut = checkIn.AddMinutes(30);

        var result = _calculator.CalculateFee(
            VehicleType.Car,
            MembershipTier.Guest,
            checkIn,
            checkOut
        );

        Assert.Equal(0m, result.TotalFee);
    }

    [Fact]
    public void CalculateFee_GracePeriod_31Minutes_ReturnsOneHourFee()
    {
        var checkIn = new DateTime(2026, 3, 16, 10, 0, 0);
        var checkOut = checkIn.AddMinutes(31);

        var result = _calculator.CalculateFee(
            VehicleType.Car,
            MembershipTier.Guest,
            checkIn,
            checkOut
        );

        Assert.Equal(1000m, result.TotalFee);
    }

    #endregion

    // ========================================================
    #region Duration Rounding
    // ========================================================

    [Fact]
    public void CalculateFee_RoundingUp_ReturnsTwoHours()
    {
        var checkIn = new DateTime(2026, 3, 16, 10, 0, 0);
        var checkOut = checkIn.AddMinutes(91);

        var result = _calculator.CalculateFee(
            VehicleType.Car,
            MembershipTier.Guest,
            checkIn,
            checkOut
        );

        Assert.Equal(2000m, result.TotalFee);
    }

    #endregion

    // ========================================================
    #region Daily Cap
    // ========================================================

    [Fact]
    public void CalculateFee_CarExceedsDailyCap_Returns8000()
    {
        var checkIn = new DateTime(2026, 3, 16, 6, 0, 0);
        var checkOut = checkIn.AddHours(12);

        var result = _calculator.CalculateFee(
            VehicleType.Car,
            MembershipTier.Guest,
            checkIn,
            checkOut
        );

        Assert.Equal(8000m, result.TotalFee);
    }

    [Fact]
    public void CalculateFee_Motorcycle_DailyCap_Returns4000()
    {
        var checkIn = new DateTime(2026, 3, 16, 6, 0, 0);
        var checkOut = checkIn.AddHours(10);

        var result = _calculator.CalculateFee(
            VehicleType.Motorcycle,
            MembershipTier.Guest,
            checkIn,
            checkOut
        );

        Assert.Equal(4000m, result.TotalFee);
    }

    #endregion

    // ========================================================
    #region Overnight Fee
    // ========================================================

    [Fact]
    public void CalculateFee_Past10PM_AddsOvernightFee()
    {
        var checkIn = new DateTime(2026, 3, 16, 20, 0, 0);
        var checkOut = new DateTime(2026, 3, 16, 23, 0, 0);

        var result = _calculator.CalculateFee(
            VehicleType.Car,
            MembershipTier.Guest,
            checkIn,
            checkOut
        );

        Assert.Equal(5000m, result.TotalFee);
    }

    [Fact]
    public void CalculateFee_CheckInAfter10PM_AddsOvernightFee()
    {
        var checkIn = new DateTime(2026, 3, 16, 22, 30, 0);
        var checkOut = new DateTime(2026, 3, 17, 1, 30, 0);

        var result = _calculator.CalculateFee(
            VehicleType.Car,
            MembershipTier.Guest,
            checkIn,
            checkOut
        );

        Assert.Equal(5000m, result.TotalFee);
    }

    #endregion

    // ========================================================
    #region Weekend Surcharge
    // ========================================================

    [Fact]
    public void CalculateFee_Weekend_Adds20PercentSurcharge()
    {
        var checkIn = new DateTime(2026, 3, 14, 10, 0, 0);
        var checkOut = checkIn.AddHours(2);

        var result = _calculator.CalculateFee(
            VehicleType.Car,
            MembershipTier.Guest,
            checkIn,
            checkOut
        );

        Assert.Equal(2400m, result.TotalFee);
    }

    #endregion

    // ========================================================
    #region Holiday Surcharge
    // ========================================================

    [Fact]
    public void CalculateFee_Holiday_Adds50PercentSurcharge()
    {
        var checkIn = new DateTime(2026, 3, 16, 10, 0, 0);
        var checkOut = checkIn.AddHours(2);

        var result = _calculator.CalculateFee(
            VehicleType.Car,
            MembershipTier.Guest,
            checkIn,
            checkOut,
            isHoliday: true
        );

        Assert.Equal(3000m, result.TotalFee);
    }

    #endregion

    // ========================================================
    #region Membership Discounts
    // ========================================================

    [Fact]
    public void CalculateFee_SilverMember_Gets10PercentDiscount()
    {
        var checkIn = new DateTime(2026, 3, 16, 10, 0, 0);
        var checkOut = checkIn.AddHours(2);

        var result = _calculator.CalculateFee(
            VehicleType.Car,
            MembershipTier.Silver,
            checkIn,
            checkOut
        );

        Assert.Equal(1800m, result.TotalFee);
    }

    [Fact]
    public void CalculateFee_GoldMember_Gets25PercentDiscount()
    {
        var checkIn = new DateTime(2026, 3, 16, 10, 0, 0);
        var checkOut = checkIn.AddHours(2);

        var result = _calculator.CalculateFee(
            VehicleType.Car,
            MembershipTier.Gold,
            checkIn,
            checkOut
        );

        Assert.Equal(1500m, result.TotalFee);
    }

    [Fact]
    public void CalculateFee_PlatinumMember_Gets40PercentDiscount()
    {
        var checkIn = new DateTime(2026, 3, 16, 10, 0, 0);
        var checkOut = checkIn.AddHours(2);

        var result = _calculator.CalculateFee(
            VehicleType.Car,
            MembershipTier.Platinum,
            checkIn,
            checkOut
        );

        Assert.Equal(1200m, result.TotalFee);
    }

    #endregion

    // ========================================================
    #region Lost Ticket
    // ========================================================

    [Fact]
    public void CalculateFee_LostTicket_Adds20000()
    {
        var checkIn = new DateTime(2026, 3, 16, 10, 0, 0);
        var checkOut = checkIn.AddHours(1);

        var result = _calculator.CalculateFee(
            VehicleType.Car,
            MembershipTier.Guest,
            checkIn,
            checkOut,
            isLostTicket: true
        );

        Assert.Equal(21000m, result.TotalFee);
    }

    [Fact]
    public void CalculateFee_LostTicketDuringGracePeriod_Returns20000()
    {
        var checkIn = new DateTime(2026, 3, 16, 10, 0, 0);
        var checkOut = checkIn.AddMinutes(10);

        var result = _calculator.CalculateFee(
            VehicleType.Car,
            MembershipTier.Guest,
            checkIn,
            checkOut,
            isLostTicket: true
        );

        Assert.Equal(20000m, result.TotalFee);
    }

    #endregion
}
public class ParkingFeeCalculatorPropertyTests
{
    private readonly ParkingFeeCalculator _calculator = new();

    private DateTime BaseTime => new DateTime(2026, 3, 16, 10, 0, 0);

    // ========================================================
    #region Time Behavior Properties
    // ========================================================

    // Property 1 — Longer stays should cost more
    [Property]
    public bool Fee_Increases_With_Time(int minutes1, int minutes2)
    {
        minutes1 = Math.Abs(minutes1 % 1000);
        minutes2 = Math.Abs(minutes2 % 1000);

        var fee1 = _calculator.CalculateFee(
            VehicleType.Car,
            MembershipTier.Guest,
            BaseTime,
            BaseTime.AddMinutes(minutes1)
        ).TotalFee;

        var fee2 = _calculator.CalculateFee(
            VehicleType.Car,
            MembershipTier.Guest,
            BaseTime,
            BaseTime.AddMinutes(minutes1 + minutes2)
        ).TotalFee;

        return fee2 >= fee1;
    }

    #endregion

    // ========================================================
    #region Grace Period Properties
    // ========================================================

    // Property 2 — Grace period should always be free
    [Property]
    public bool GracePeriod_IsAlwaysFree(int minutes)
    {
        minutes = Math.Abs(minutes % 30);

        var result = _calculator.CalculateFee(
            VehicleType.Car,
            MembershipTier.Guest,
            BaseTime,
            BaseTime.AddMinutes(minutes)
        );

        return result.TotalFee == 0;
    }

    #endregion

    // ========================================================
    #region Membership Discount Properties
    // ========================================================

    // Property 3 — Members always pay <= guest
    [Property]
    public bool MembersPayLessOrEqual(int minutes)
    {
        minutes = Math.Abs(minutes % 300);

        var guest = _calculator.CalculateFee(
            VehicleType.Car,
            MembershipTier.Guest,
            BaseTime,
            BaseTime.AddMinutes(minutes + 31)
        ).TotalFee;

        var silver = _calculator.CalculateFee(
            VehicleType.Car,
            MembershipTier.Silver,
            BaseTime,
            BaseTime.AddMinutes(minutes + 31)
        ).TotalFee;

        var gold = _calculator.CalculateFee(
            VehicleType.Car,
            MembershipTier.Gold,
            BaseTime,
            BaseTime.AddMinutes(minutes + 31)
        ).TotalFee;

        var platinum = _calculator.CalculateFee(
            VehicleType.Car,
            MembershipTier.Platinum,
            BaseTime,
            BaseTime.AddMinutes(minutes + 31)
        ).TotalFee;

        return platinum <= gold && gold <= silver && silver <= guest;
    }

    // Property 4 — Higher tier gets better discount
    [Property]
    public bool HigherTier_GetsBetterDiscount(int minutes)
    {
        minutes = Math.Abs(minutes % 300);

        var silver = _calculator.CalculateFee(
            VehicleType.Car,
            MembershipTier.Silver,
            BaseTime,
            BaseTime.AddMinutes(minutes + 31)
        ).TotalFee;

        var gold = _calculator.CalculateFee(
            VehicleType.Car,
            MembershipTier.Gold,
            BaseTime,
            BaseTime.AddMinutes(minutes + 31)
        ).TotalFee;

        var platinum = _calculator.CalculateFee(
            VehicleType.Car,
            MembershipTier.Platinum,
            BaseTime,
            BaseTime.AddMinutes(minutes + 31)
        ).TotalFee;

        return platinum <= gold && gold <= silver;
    }

    #endregion

    // ========================================================
    #region Lost Ticket Properties
    // ========================================================

    // Property 5 — Lost ticket adds exactly 20000
    [Property]
    public bool LostTicket_AddsExactly20000(int minutes)
    {
        minutes = Math.Abs(minutes % 300);

        var normal = _calculator.CalculateFee(
            VehicleType.Car,
            MembershipTier.Guest,
            BaseTime,
            BaseTime.AddMinutes(minutes + 31)
        ).TotalFee;

        var lost = _calculator.CalculateFee(
            VehicleType.Car,
            MembershipTier.Guest,
            BaseTime,
            BaseTime.AddMinutes(minutes + 31),
            true
        ).TotalFee;

        return lost - normal == 20000;
    }

    #endregion

    // ========================================================
    #region Cap and Limits Properties
    // ========================================================

    // Property 6 — Fee should not exceed a reasonable cap
    [Property]
    public bool Fee_NeverExceedsDailyCap(int minutes)
    {
        minutes = Math.Abs(minutes % 2000);

        var result = _calculator.CalculateFee(
            VehicleType.Car,
            MembershipTier.Guest,
            BaseTime,
            BaseTime.AddMinutes(minutes + 31)
        ).TotalFee;

        return result <= 12000;
    }

    #endregion

    // ========================================================
    #region Surcharge Interaction Properties
    // ========================================================

    // Property 7 — Holiday overrides weekend surcharge
    [Property]
    public bool Holiday_Overrides_Weekend(int minutes)
    {
        var checkIn = new DateTime(2026, 3, 14, 10, 0, 0);
        var checkOut = checkIn.AddMinutes(Math.Abs(minutes % 300) + 31);

        var weekend = _calculator.CalculateFee(
            VehicleType.Car,
            MembershipTier.Guest,
            checkIn,
            checkOut,
            false,
            false
        ).TotalFee;

        var holiday = _calculator.CalculateFee(
            VehicleType.Car,
            MembershipTier.Guest,
            checkIn,
            checkOut,
            false,
            true
        ).TotalFee;

        return holiday >= weekend;
    }

    #endregion
}

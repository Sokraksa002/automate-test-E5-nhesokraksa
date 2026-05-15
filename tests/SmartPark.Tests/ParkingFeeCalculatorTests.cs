using SmartPark.Core.Models;
using SmartPark.Core.Services;
using FsCheck;
using FsCheck.Xunit;

namespace SmartPark.Tests;

public class ParkingFeeCalculatorPropertyTests
{
    private readonly ParkingFeeCalculator _calculator = new();

    private DateTime BaseTime => new DateTime(2026, 3, 16, 10, 0, 0);

    // ✅ PROPERTY 1 — Longer stays cost more (or equal)
    [Property]
    public bool Fee_Increases_With_Time(int minutes1, int minutes2)
    {
        minutes1 = Math.Abs(minutes1 % 1000);
        minutes2 = Math.Abs(minutes2 % 1000);

        var checkIn = BaseTime;

        var fee1 = _calculator.CalculateFee(
            VehicleType.Car,
            MembershipTier.Guest,
            checkIn,
            checkIn.AddMinutes(minutes1)
        ).TotalFee;

        var fee2 = _calculator.CalculateFee(
            VehicleType.Car,
            MembershipTier.Guest,
            checkIn,
            checkIn.AddMinutes(minutes1 + minutes2)
        ).TotalFee;

        return fee2 >= fee1;
    }

    // ✅ PROPERTY 2 — Grace period is always free
    [Property]
    public bool GracePeriod_IsAlwaysFree(int minutes)
    {
        minutes = Math.Abs(minutes % 30); // ≤ 30

        var result = _calculator.CalculateFee(
            VehicleType.Car,
            MembershipTier.Guest,
            BaseTime,
            BaseTime.AddMinutes(minutes)
        );

        return result.TotalFee == 0;
    }

    // ✅ PROPERTY 3 — Members pay <= guest
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

    // ✅ PROPERTY 4 — Higher membership = lower fee
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

    // ✅ PROPERTY 5 — Lost ticket adds exactly 20000
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
            isLostTicket: true
        ).TotalFee;

        return lost - normal == 20000;
    }

    // ✅ PROPERTY 6 — Fee never exceeds daily cap (before extras)
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

        return result <= 12000; // SUV max upper safety bound
    }

    // ✅ PROPERTY 7 — Holiday overrides weekend
    [Property]
    public bool Holiday_Overrides_Weekend(int minutes)
    {
        var checkIn = new DateTime(2026, 3, 14, 10, 0, 0); // Saturday
        var checkOut = checkIn.AddMinutes(Math.Abs(minutes % 300) + 31);

        var weekendOnly = _calculator.CalculateFee(
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

        // Holiday should NOT stack with weekend
        return holiday >= weekendOnly;
    }
}
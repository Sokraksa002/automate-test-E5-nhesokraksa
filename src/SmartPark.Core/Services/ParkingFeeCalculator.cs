using SmartPark.Core.Models;

namespace SmartPark.Core.Services;

public class ParkingFeeCalculator
{
    private const int GracePeriodMinutes = 30;
    private const decimal CarRatePerHour = 1000m;
    private const decimal CarDailyCap = 8000m;

    public ParkingFeeResult CalculateFee(
        VehicleType vehicleType,
        MembershipTier membership,
        DateTime checkIn,
        DateTime checkOut,
        bool isLostTicket = false,
        bool isHoliday = false)
    {
        // 1. Validate
        if (checkOut < checkIn)
            throw new ArgumentException();

        // 2. Calculate total duration
        var totalMinutes = (checkOut - checkIn).TotalMinutes;

        // 3. Grace period (includes zero duration)
        if (totalMinutes <= GracePeriodMinutes)
        {
            return new ParkingFeeResult
            {
                TotalFee = 0m
            };
        }

        // 4. Duration rounding
        var billableMinutes = totalMinutes - GracePeriodMinutes;
        var billableHours = Math.Ceiling(billableMinutes / 60.0);

        decimal totalFee = (decimal)billableHours * CarRatePerHour;

        // 5. Daily cap
        if (totalFee > CarDailyCap)
        {
            totalFee = CarDailyCap;
        }

        return new ParkingFeeResult
        {
            TotalFee = totalFee
        };
    }
}

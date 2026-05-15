using SmartPark.Core.Models;

namespace SmartPark.Core.Services;

public class ParkingFeeCalculator
{
    private const int GracePeriodMinutes = 30;
    private const decimal CarRatePerHour = 1000m;
    private const decimal CarDailyCap = 8000m;
    private const decimal OvernightFee = 2000m;
    private const int OvernightHourThreshold = 22;
    private const decimal WeekendSurchargeRate = 0.20m;
    private const decimal HolidaySurchargeRate = 0.50m;

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

        // 2. Duration
        var totalMinutes = (checkOut - checkIn).TotalMinutes;

        // 3. Grace period
        if (totalMinutes <= GracePeriodMinutes)
        {
            return new ParkingFeeResult { TotalFee = 0m };
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

        // 6. Overnight fee
        if (checkIn.Hour >= OvernightHourThreshold ||
            checkOut.Hour >= OvernightHourThreshold)
        {
            totalFee += OvernightFee;
        }

        // 7. Surcharge (IMPORTANT: holiday overrides weekend)
        if (isHoliday)
        {
            totalFee += totalFee * HolidaySurchargeRate;
        }
        else if (checkIn.DayOfWeek == DayOfWeek.Saturday ||
                 checkIn.DayOfWeek == DayOfWeek.Sunday)
        {
            totalFee += totalFee * WeekendSurchargeRate;
        }

        return new ParkingFeeResult
        {
            TotalFee = totalFee
        };
    }
}
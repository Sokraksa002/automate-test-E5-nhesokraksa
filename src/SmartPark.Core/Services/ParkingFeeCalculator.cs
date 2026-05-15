using SmartPark.Core.Models;

namespace SmartPark.Core.Services;

public class ParkingFeeCalculator
{
    private const int GracePeriodMinutes = 30;
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

        var totalMinutes = (checkOut - checkIn).TotalMinutes;

        // 2. Grace period
        if (totalMinutes <= GracePeriodMinutes)
        {
            if (isLostTicket)
                return new ParkingFeeResult { TotalFee = 20000m };

            return new ParkingFeeResult { TotalFee = 0m };
        }

        // 3. Duration rounding
        var billableMinutes = totalMinutes - GracePeriodMinutes;
        var billableHours = Math.Ceiling(billableMinutes / 60.0);

        // Rate per vehicle
        decimal rate = vehicleType switch
        {
            VehicleType.Motorcycle => 500m,
            VehicleType.Car => 1000m,
            VehicleType.SUV => 1500m
        };

        decimal totalFee = (decimal)billableHours * rate;

        // Daily cap per vehicle
        decimal cap = vehicleType switch
        {
            VehicleType.Motorcycle => 4000m,
            VehicleType.Car => 8000m,
            VehicleType.SUV => 12000m
        };

        if (totalFee > cap)
        {
            totalFee = cap;
        }

        // 4. Overnight fee
        if (checkIn.Hour >= OvernightHourThreshold ||
            checkOut.Hour >= OvernightHourThreshold)
        {
            totalFee += OvernightFee;
        }

        // 5. Surcharge (holiday overrides weekend)
        if (isHoliday)
        {
            totalFee += totalFee * HolidaySurchargeRate;
        }
        else if (checkIn.DayOfWeek == DayOfWeek.Saturday ||
                 checkIn.DayOfWeek == DayOfWeek.Sunday)
        {
            totalFee += totalFee * WeekendSurchargeRate;
        }

        // 6. Membership discount
        decimal discountRate = membership switch
        {
            MembershipTier.Silver => 0.10m,
            MembershipTier.Gold => 0.25m,
            MembershipTier.Platinum => 0.40m,
            _ => 0m
        };

        totalFee -= totalFee * discountRate;

        // 7. Lost ticket
        if (isLostTicket)
        {
            totalFee += 20000m;
        }

        return new ParkingFeeResult
        {
            TotalFee = totalFee
        };
    }
}
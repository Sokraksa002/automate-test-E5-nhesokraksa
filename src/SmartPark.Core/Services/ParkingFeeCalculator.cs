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

        var totalMinutes = (checkOut - checkIn).TotalMinutes;

        
        // 2. Grace period
        if (totalMinutes <= GracePeriodMinutes)
        {
            if (isLostTicket)
            {
                return new ParkingFeeResult { TotalFee = 20000m };
            }

                return new ParkingFeeResult { TotalFee = 0m };
        }


        // 3. Duration rounding
        var billableMinutes = totalMinutes - GracePeriodMinutes;
        var billableHours = Math.Ceiling(billableMinutes / 60.0);
        decimal totalFee = (decimal)billableHours * CarRatePerHour;

        // 4. Daily cap
        if (totalFee > CarDailyCap)
        {
            totalFee = CarDailyCap;
        }

        // 5. Overnight fee
        if (checkIn.Hour >= OvernightHourThreshold ||
            checkOut.Hour >= OvernightHourThreshold)
        {
            totalFee += OvernightFee;
        }

        // 6. Surcharge (holiday overrides weekend)
        if (isHoliday)
        {
            totalFee += totalFee * HolidaySurchargeRate;
        }
        else if (checkIn.DayOfWeek == DayOfWeek.Saturday ||
                 checkIn.DayOfWeek == DayOfWeek.Sunday)
        {
            totalFee += totalFee * WeekendSurchargeRate;
        }

        // 7. Membership discount
        decimal discountRate = membership switch
        {
            MembershipTier.Silver => 0.10m,
            MembershipTier.Gold => 0.25m,
            MembershipTier.Platinum => 0.40m,
            _           => 0m
        };  

        decimal discount = totalFee * discountRate;
        totalFee -= discount;

        //  8. Lost ticket penalty (NOT discounted)
        decimal penalty = isLostTicket ? 20000m : 0m;
        totalFee += penalty;

        return new ParkingFeeResult
        {
            TotalFee = totalFee
        };
    }
}
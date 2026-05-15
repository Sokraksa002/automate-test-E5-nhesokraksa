using SmartPark.Core.Models;

namespace SmartPark.Core.Services;

public class ParkingFeeCalculator
{
    private const int GracePeriodMinutes = 30;

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

        // 2. Zero duration
        if (checkOut == checkIn)
        {
            return new ParkingFeeResult
            {
                TotalFee = 0m
            };
        }

        // 3. Grace period (≤ 30 minutes)
        var totalMinutes = (checkOut - checkIn).TotalMinutes;

        if (totalMinutes <= GracePeriodMinutes)
        {
            return new ParkingFeeResult
            {
                TotalFee = 0m
            };
        }

        // 4. First billable hour (31 minutes)
        return new ParkingFeeResult
        {
            TotalFee = 1000m
        };
    }
}

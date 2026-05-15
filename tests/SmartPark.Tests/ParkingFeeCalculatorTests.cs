using SmartPark.Core.Models;
using SmartPark.Core.Services;
using FsCheck;
using FsCheck.Xunit;

namespace SmartPark.Tests;

public class ParkingFeeCalculatorTests
{
    private readonly ParkingFeeCalculator _calculator = new();

    // ────────────────────────────────────────────────────────────
    //  EXAMPLE TEST — shows the naming convention and AAA pattern.
    //  Delete or keep this; it does not count toward your grade.
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void CalculateFee_ZeroDuration_ReturnsFree()
    {
        // Arrange
        var checkIn = new DateTime(2026, 3, 16, 10, 0, 0);  // Monday
        var checkOut = checkIn; // same time = 0 duration

        // Act
        var result = _calculator.CalculateFee(VehicleType.Car, MembershipTier.Guest, checkIn, checkOut);

        // Assert
        Assert.Equal(0m, result.TotalFee);
    }

    #region Basic Fee Calculation
    // Test basic hourly rates for each vehicle type
    // Consider using [Theory] with [InlineData] for multiple scenarios
    #endregion

    #region Grace Period
    // Test the free parking window and its boundaries

[Fact]
public void CalculateFee_GracePeriod_30Minutes_ReturnsFree()
{
	// Arrange
	var checkIn = new DateTime(2026, 3, 16, 10, 0, 0);
	var checkOut = checkIn.AddMinutes(30);

	// Act
	var result = _calculator.CalculateFee(
    	VehicleType.Car,
    	MembershipTier.Guest,
    	checkIn,
    	checkOut
	);

	// Assert
	Assert.Equal(0m, result.TotalFee);
}
[Fact]
public void CalculateFee_GracePeriod_31Minutes_ReturnsOneHourFee()
{
    // Arrange
    var checkIn = new DateTime(2026, 3, 16, 10, 0, 0);
    var checkOut = checkIn.AddMinutes(31);

    // Act
    var result = _calculator.CalculateFee(
        VehicleType.Car,
        MembershipTier.Guest,
        checkIn,
        checkOut
    );

    // Assert
    Assert.Equal(1000m, result.TotalFee);
}
    #endregion

    #region Duration Rounding
    // Test how partial hours are rounded for billing
    [Fact]
public void CalculateFee_RoundingUp_ReturnsTwoHours()
{
    // Arrange
    var checkIn = new DateTime(2026, 3, 16, 10, 0, 0);

    // 30 min grace + 61 min = 91 min total
    var checkOut = checkIn.AddMinutes(91);

    // Act
    var result = _calculator.CalculateFee(
        VehicleType.Car,
        MembershipTier.Guest,
        checkIn,
        checkOut
    );

    // Assert
    Assert.Equal(2000m, result.TotalFee);
}
    #endregion

    #region Daily Cap
    // Test that fees respect maximum daily limits per vehicle type
    [Fact]
public void CalculateFee_CarExceedsDailyCap_Returns8000()
{
    // Arrange
    var checkIn = new DateTime(2026, 3, 16, 6, 0, 0);
    var checkOut = checkIn.AddHours(12); // Long stay

    // Act
    var result = _calculator.CalculateFee(
        VehicleType.Car,
        MembershipTier.Guest,
        checkIn,
        checkOut
    );

    // Assert
    Assert.Equal(8000m, result.TotalFee);
}
    #endregion

    #region Overnight Fee
    // Test the flat fee applied for sessions that extend into late hours
      [Fact]
public void CalculateFee_Past10PM_AddsOvernightFee()
{
    // Arrange
    var checkIn = new DateTime(2026, 3, 16, 20, 0, 0); // 8:00 PM
    var checkOut = new DateTime(2026, 3, 16, 23, 0, 0); // 11:00 PM

    // Act
    var result = _calculator.CalculateFee(
        VehicleType.Car,
        MembershipTier.Guest,
        checkIn,
        checkOut
    );

    // Assert
    // 3 hours = 3000 + 2000 overnight
    Assert.Equal(5000m, result.TotalFee);
}
[Fact]
	public void CalculateFee_CheckInAfter10PM_AddsOvernightFee()
	{
    	// Arrange
    	var checkIn = new DateTime(2026, 3, 16, 22, 30, 0); // 10:30 PM
    	var checkOut = new DateTime(2026, 3, 17, 1, 30, 0); // 1:30 AM

    	// Act
    	var result = _calculator.CalculateFee(
        	VehicleType.Car,
        	MembershipTier.Guest,
        	checkIn,
        	checkOut
    	);

    	// Assert
    	// 3 hours = 3000 + 2000 overnight
    	Assert.Equal(5000m, result.TotalFee);
	}
    [Fact]
public void CalculateFee_CheckInAfter10PM_CheckOutNextMorning_AddsOvernightFee()
{
    // Arrange
    var checkIn = new DateTime(2026, 3, 16, 23, 30, 0); // 11:30 PM
    var checkOut = new DateTime(2026, 3, 17, 6, 0, 0); // 6:00 AM

    // Act
    var result = _calculator.CalculateFee(
        VehicleType.Car,
        MembershipTier.Guest,
        checkIn,
        checkOut
    );

    // Assert
    Assert.True(result.TotalFee >= 2000m);
}

    #endregion

    #region Weekend Surcharge
    // Test the percentage-based surcharge on specific days
    [Fact]
public void CalculateFee_Weekend_Adds20PercentSurcharge()
{
    // Arrange
    var checkIn = new DateTime(2026, 3, 14, 10, 0, 0); // Saturday
    var checkOut = checkIn.AddHours(2); // 2 hours

    // Act
    var result = _calculator.CalculateFee(
        VehicleType.Car,
        MembershipTier.Guest,
        checkIn,
        checkOut
    );

    // Assert
    // Base fee = 2h × 1000 = 2000
    // Weekend surcharge = 20% = 400
    Assert.Equal(2400m, result.TotalFee);
}
    #endregion

    #region Holiday Surcharge
    // Test holiday pricing and its interaction with weekend pricing
    #endregion

    #region Membership Discounts
    // Test discount tiers and what amounts they apply to
    #endregion

    #region Lost Ticket
    // Test the penalty and how it interacts with other fee modifiers
    #endregion

    #region Edge Cases
    // Test invalid inputs and boundary conditions
    #endregion

    #region Property-Based Tests
    // Write at least 5 FsCheck properties that must hold for ALL valid inputs
    // You may need custom Arbitrary<T> for generating valid DateTime pairs
    #endregion
}

using Libary;

[TestFixture]
public class ExtendedTests
{
    private Calculations calculations;

    [SetUp]
    public void Setup()
    {
        calculations = new Calculations();
    }

    [Test]
    public void TestNoBusyPeriods_FullDayFree()
    {
        var result = calculations.AvailablePeriods(
            Array.Empty<TimeSpan>(),
            Array.Empty<int>(),
            new TimeSpan(9, 0, 0),
            new TimeSpan(17, 0, 0),
            60);

        Assert.AreEqual(new[]
        {
            "09:00-10:00", "10:00-11:00", "11:00-12:00",
            "12:00-13:00", "13:00-14:00", "14:00-15:00",
            "15:00-16:00", "16:00-17:00"
        }, result);
    }

    [Test]
    public void TestUnsortedBusyPeriods_ShouldHandleCorrectly()
    {
        var result = calculations.AvailablePeriods(
            new TimeSpan[] { new(12, 0, 0), new(9, 30, 0), new(15, 0, 0) },
            new int[] { 60, 90, 30 },
            new TimeSpan(9, 0, 0),
            new TimeSpan(17, 0, 0),
            45);

        Assert.AreEqual(new[]
        {
            "09:00-09:45", "11:00-11:45", "11:45-12:30",
            "13:00-13:45", "13:45-14:30", "14:30-15:15",
            "15:30-16:15", "16:15-17:00"
        }, result);
    }

    [Test]
    public void TestConsultationFillsWholeDay()
    {
        var result = calculations.AvailablePeriods(
            Array.Empty<TimeSpan>(),
            Array.Empty<int>(),
            new TimeSpan(8, 0, 0),
            new TimeSpan(17, 0, 0),
            540); 

        Assert.AreEqual(1, result.Length);
        Assert.AreEqual("08:00-17:00", result[0]);
    }

    [Test]
    public void TestBackToBackBusyPeriods()
    {
        var result = calculations.AvailablePeriods(
            new TimeSpan[] { new(10, 0, 0), new(11, 0, 0) },
            new int[] { 60, 60 },
            new TimeSpan(9, 0, 0),
            new TimeSpan(12, 0, 0),
            30);

        Assert.AreEqual(new[]
        {
            "09:00-09:30", "09:30-10:00",
            "12:00-12:30"
        }, result);
    }

    [Test]
    public void TestMidnightBoundaries()
    {
        var result = calculations.AvailablePeriods(
            new TimeSpan[] { new(23, 30, 0) },
            new int[] { 30 },
            new TimeSpan(23, 0, 0),
            new TimeSpan(0, 0, 0),
            15);

        Assert.AreEqual(new[]
        {
            "23:00-23:15", "23:15-23:30"
        }, result);
    }

    [Test]
    public void TestOneMinuteConsultations()
    {
        var result = calculations.AvailablePeriods(
            new TimeSpan[] { new(10, 0, 0) },
            new int[] { 5 },
            new TimeSpan(9, 55, 0),
            new TimeSpan(10, 10, 0),
            1);

        Assert.AreEqual(14, result.Length); 
        Assert.AreEqual("09:55-09:56", result[0]);
        Assert.AreEqual("10:05-10:06", result[^1]);
    }

    [Test]
    public void TestOverlappingBusyPeriods()
    {
        var result = calculations.AvailablePeriods(
            new TimeSpan[] { new(10, 0, 0), new(10, 15, 0) },
            new int[] { 30, 30 },
            new TimeSpan(9, 0, 0),
            new TimeSpan(11, 0, 0),
            15);

        Assert.AreEqual(new[]
        {
            "09:00-09:15", "09:15-09:30", "09:30-09:45", "09:45-10:00",
            "10:45-11:00"
        }, result);
    }

    [Test]
    public void TestBusyPeriodBeforeWorkStart()
    {
        var result = calculations.AvailablePeriods(
            new TimeSpan[] { new(8, 30, 0) },
            new int[] { 60 },
            new TimeSpan(9, 0, 0),
            new TimeSpan(10, 0, 0),
            30);

        Assert.AreEqual(new[] { "09:00-09:30" }, result);
    }

    [Test]
    public void TestBusyPeriodAfterWorkEnd()
    {
        var result = calculations.AvailablePeriods(
            new TimeSpan[] { new(16, 30, 0) },
            new int[] { 120 },
            new TimeSpan(16, 0, 0),
            new TimeSpan(17, 0, 0),
            15);

        Assert.AreEqual(new[]
        {
            "16:00-16:15", "16:15-16:30"
        }, result);
    }

    [Test]
    public void TestInvalidConsultationTime()
    {
        Assert.Throws<ArgumentException>(() => 
            calculations.AvailablePeriods(
                Array.Empty<TimeSpan>(),
                Array.Empty<int>(),
                new TimeSpan(9, 0, 0),
                new TimeSpan(10, 0, 0),
                0));
    }
}
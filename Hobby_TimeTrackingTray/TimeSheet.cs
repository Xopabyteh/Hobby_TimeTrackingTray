namespace Hobby_TimeTrackingTray;

public record TimeSheet(
    DateTime[] DateColumn,
    DateTime[] StartTimeColumn,
    DateTime[] StopTimeColumn,
    TimeSpan[] WorkTimeColumn,
    string[] CommentColumn)
{
    public TimeSheet(int rowLength) 
        : this(
            new DateTime[rowLength],
            new DateTime[rowLength],
            new DateTime[rowLength],
            new TimeSpan[rowLength],
            new string[rowLength])
    {
    }
}
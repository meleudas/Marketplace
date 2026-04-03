namespace Marketplace.Domain.Companies.Enums;

/// <summary>День тижня для розкладу компанії (0 = Sunday за ISO можна змінити в Infrastructure).</summary>
public enum CompanyScheduleDay : short
{
    Sunday = 0,
    Monday = 1,
    Tuesday = 2,
    Wednesday = 3,
    Thursday = 4,
    Friday = 5,
    Saturday = 6
}

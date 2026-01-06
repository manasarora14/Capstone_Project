namespace ServiceManagementApi.Models;

public class ServiceCategory
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal BaseCharge { get; set; }
    public int SlaHours { get; set; }

    public string DisplaySla => $"{SlaHours} hours";
}
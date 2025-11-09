namespace EduvisionMvc.ViewModels;

public class RadarChartData
{
    public List<string> Labels { get; set; } = new();
    public List<decimal> Values { get; set; } = new();
    public string DatasetLabel { get; set; } = "";
}

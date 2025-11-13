namespace Class_Project_Database_Portion.ViewModels
{
    public class ChildTaskViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Contents { get; set; } = string.Empty;
        public int TaskStatus { get; set; }
    }
}

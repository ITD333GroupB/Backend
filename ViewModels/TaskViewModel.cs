namespace Class_Project_Database_Portion.ViewModels
{
    public class TaskViewModel
    {
        public int TaskId { get; set; }
        public string TaskName { get; set; } = string.Empty;
        public string Assignee { get; set; } = string.Empty;
        public int TaskStatus { get; set; }
        public List<ChildTaskViewModel> ChildTasks { get; set; } = new();
    }
}

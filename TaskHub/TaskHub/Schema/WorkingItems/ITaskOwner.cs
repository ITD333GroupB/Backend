namespace TaskHub.Schema.WorkingItems
{
    /// <summary>
    /// This is strictly for Tasks and Workspace to help logically layout parent/child relations.
    /// This -DOES NOT- apply to user IDs or profiles
    /// </summary>
    public interface ITaskOwner
    {
        public bool IsChildTask { get; set; } // indicates if this task is a child task or a parent task
    }
}

using System.Text.Json.Serialization;

namespace TaskHub.Schema.WorkingItems
{
    public class Workspace : IOwner, ITaskOwner
    {
        public int WorkspaceId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<IMessage> ChatMessageHistory { get; set; }

        private TaskList _tasks;
        public TaskList Tasks
        {
            get => _tasks ??= new TaskList(this);
            set
            {
                _tasks = value ?? new TaskList(this);
                _tasks.Owner = this;
            }
        }

        [JsonConstructor]
        public Workspace() { }


        #region ITaskOwner Implementation
        public bool IsChildTask { get; set; } // indicates if this task is a child task or not (will be either child task, or parent task/workspace)

        #endregion

        #region IOwner Implementation
        public int? OwnerId { get; set; }
        public int? ParentTaskId { get; set; }
        public int? ParentWorkspaceId { get; set; }
        public int? ParentGroupId { get; set; }
        #endregion
    }
}

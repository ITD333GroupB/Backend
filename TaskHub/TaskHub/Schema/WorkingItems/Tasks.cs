using System.Text.Json.Serialization;

namespace TaskHub.Schema.WorkingItems
{
    /// <summary>
    /// Represents an association between the status of a task and the task itself.
    /// </summary>
    public enum TaskStatus
    {
        NotStarted = 0,
        InProgress = 1,
        OnHold = 2,
        Completed = 3
    } // the numbers are what will appear in the database tables


    public class Tasks : IOwner, ITaskOwner
    {
        public int TaskId { get; set; } // unique identifier for the task, optional until persisted
        public string Title { get; set; }
        public string Description { get; set; }
        public List<IMessage> Comments { get; set; } // list of comments associated with this task
        private TaskList _tasks;
        public TaskList ChildTasks
        {
            get => _tasks ??= new TaskList(this); // lazy-init for non-JSON scenarios
            set
            {
                // when deserializing, JSON sets this property; ensure owner is bound
                _tasks = value ?? new TaskList(this);
                _tasks.Owner = this;
            }
        }
        public DateTime DueDate { get; set; }
        public TaskStatus Status { get; set; } // enums when passed to DB are typically stored as ints (0, 1, 2, 3...)

        [JsonConstructor]
        public Tasks() { }

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

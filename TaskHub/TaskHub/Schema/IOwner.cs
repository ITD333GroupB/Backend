namespace TaskHub.Schema
{
    // this interface can be implemented by any class that has an owner, such as a Group, Workspace, or Task
    // this allows us to easily check if a user or parent object is the owner or holder of an object
    public interface IOwner
    {
        /// <summary>
        /// This is the unique identifier of the user profile Id who owns this object.
        /// </summary>
        public int? OwnerId { get; set; } // unique identifier for the owner, optional until persisted

        /// <summary>
        /// This is the unique identifier of the parent task to a child task.
        /// </summary>
        /// <remarks>This ONLY is ever applied to <see cref="Tasks"/> and is meant to assist with database to backend organization.</remarks>
        public int? ParentTaskId { get; set; } // if this object is a sub-task, this will be the unique identifier of the parent task

        /// <summary>
        /// This is the unique identifier of the workspace Id that this object belongs to.
        /// </summary>
        public int? ParentWorkspaceId { get; set; }
        /// <summary>
        /// This is the unique identifier of the group Id that this object belongs to.
        /// </summary>
        public int? ParentGroupId { get; set; }
    }
}

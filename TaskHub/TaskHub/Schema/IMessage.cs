namespace TaskHub.Schema
{
    public enum MessageType
    {
        TaskComment = 0,
        WorkspaceChatMessage = 1
    }
    /// <summary>
    /// This interface will work for messages used to comment on tasks and to chat in workspaces. This way, a single DTO can be used in one or two indexed tables.
    /// </summary>
    public interface IMessage
    {
        public string Message { get; set; }
        public int AuthorUserId { get; set; } // this is the UserId of the profile that wrote/created the message
        public string AuthorUsername { get; set; } // username associated with the UserId of the author for quick access and visibility
        public DateTime CreatedAt { get; set; } // the timestamp of when the author sent the message

        /*
         The MessageType enum will be either 0 or 1 when sent to the database. So when we are searching for all the chat messages in a workspace, we can use this
         to more quickly tell the system "we're searching for workspace IDs" or "we're searching for task IDs". This will allow us to choose whether we want to
         keep all chat messages in a separate table from comment messages (and the 0/1 determines which table to search) -or- we can keep them all in a single table
         and then add the 0/1 as a query filter. Regardless of the approach for the tables, we ultimately can just use the same schema for all message types.
         */
        public MessageType Type { get; set; } // would be either a TaskComment or WorkspaceChatMessage 
        public int OwnerId { get; set; } // would be either workspace ID or a task ID, depending on the Type (NOT the user profile ID of the author)
    }
}

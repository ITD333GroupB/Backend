using System.Text.Json.Serialization;

namespace TaskHub.Schema.WorkingItems
{
    public class Group : IOwner
    {
        public string GroupId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public List<int> MemberUserIds { get; set; }
        public List<int> WorkspaceIds { get; set; }
        public DateTime CreatedAt { get; set; }

        [JsonConstructor]
        public Group() { }

        #region IOwner Implementation
        public int? OwnerId { get; set; }
        public int? ParentTaskId { get; set; }
        public int? ParentWorkspaceId { get; set; }
        public int? ParentGroupId { get; set; }
        #endregion
    }
}

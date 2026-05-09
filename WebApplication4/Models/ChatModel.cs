using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace WebApplication4.Models
{
    public class UserBlock
    {
        public int Id { get; set; }
        public string BlockerId { get; set; } = string.Empty;
        [ForeignKey("BlockerId")]
        public ApplicationUser Blocker { get; set; } = null!;

        public string BlockedUserId { get; set; } = string.Empty;
        [ForeignKey("BlockedUserId")]
        public ApplicationUser BlockedUserRef { get; set; } = null!; // Переименовано

        public DateTime BlockedAt { get; set; } = DateTime.Now;
        public string? Reason { get; set; }
    }

    public class GroupChat
    {
        [Key]
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }

        public string CreatedById { get; set; } = string.Empty;
        [ForeignKey("CreatedById")]
        public ApplicationUser Creator { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public bool IsDeleted { get; set; }

        public ICollection<GroupMember> Members { get; set; } = new List<GroupMember>();
        public ICollection<GroupMessage> Messages { get; set; } = new List<GroupMessage>();
    }

    public class GroupMember
    {
        [Key]
        public int Id { get; set; }

        public int GroupChatId { get; set; }
        [ForeignKey("GroupChatId")]
        public GroupChat GroupChat { get; set; } = null!;

        public string UserId { get; set; } = string.Empty;
        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; } = null!;

        public bool IsAdmin { get; set; }
        public DateTime JoinedAt { get; set; } = DateTime.Now;
        public bool IsMuted { get; set; }
        public bool Left { get; set; }
    }

    public class GroupMessage
    {
        [Key]
        public int Id { get; set; }

        public int GroupChatId { get; set; }
        [ForeignKey("GroupChatId")]
        public GroupChat GroupChat { get; set; } = null!;

        public string SenderId { get; set; } = string.Empty;
        [ForeignKey("SenderId")]
        public ApplicationUser Sender { get; set; } = null!;

        public string Text { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public bool IsRead { get; set; }
        public bool IsDeleted { get; set; }
        public bool IsEdited { get; set; }
    }
    public class ConversationParticipant
    {
        public int Id { get; set; }

        public int ConversationId { get; set; }
        public Conversation Conversation { get; set; } = null!;

        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!;

        public bool IsAdmin { get; set; }
        public DateTime JoinedAt { get; set; } = DateTime.Now;
    }
}
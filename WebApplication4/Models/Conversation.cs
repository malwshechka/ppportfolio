using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication4.Models
{
    public class Conversation
    {
        [Key]
        public int Id { get; set; }

        public string User1Id { get; set; } = string.Empty;
        [ForeignKey("User1Id")]
        public ApplicationUser User1 { get; set; } = null!;

        public string User2Id { get; set; } = string.Empty;
        [ForeignKey("User2Id")]
        public ApplicationUser User2 { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? LastMessageAt { get; set; }

        public ICollection<Message> Messages { get; set; } = new List<Message>();

        // 🔹 ДОБАВЬ ЭТО СВОЙСТВО:
        public ICollection<ConversationParticipant> Participants { get; set; } = new List<ConversationParticipant>();

        // 🔹 Флаги удаления и закрепления
        public bool IsDeletedByUser1 { get; set; }
        public bool IsDeletedByUser2 { get; set; }
        public bool IsPinnedByUser1 { get; set; }
        public bool IsPinnedByUser2 { get; set; }
    }
}
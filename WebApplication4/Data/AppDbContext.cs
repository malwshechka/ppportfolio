using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WebApplication4.Models;

namespace WebApplication4.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // 🔹 СУЩЕСТВУЮЩИЕ DbSet
        public DbSet<Project> Projects { get; set; }
        public DbSet<Status> Statuses { get; set; }
        public DbSet<ComplexityLevel> ComplexityLevels { get; set; }
        public DbSet<AppType> AppTypes { get; set; }
        public DbSet<Technology> Technologies { get; set; }
        public DbSet<ProjectTechnology> ProjectTechnologies { get; set; }
        public DbSet<Image> Images { get; set; }
        public DbSet<SelectedImage> SelectedImages { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Conversation> Conversations { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<Favorite> Favorites { get; set; }

        //🔹 НОВЫЕ DbSet для чата
        public DbSet<UserBlock> UserBlocks { get; set; }
        public DbSet<GroupChat> GroupChats { get; set; }
        public DbSet<GroupMember> GroupMembers { get; set; }
        public DbSet<GroupMessage> GroupMessages { get; set; }

        // 🔹 НОВЫЕ DbSet для подписок и жалоб
        public DbSet<UserSubscription> UserSubscriptions { get; set; }
        public DbSet<UserReport> UserReports { get; set; }
        public DbSet<ConversationParticipant> ConversationParticipants { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // === СУЩЕСТВУЮЩИЕ КОНФИГУРАЦИИ ===

            builder.Entity<ConversationParticipant>()
                .HasOne(cp => cp.Conversation)
                .WithMany(c => c.Participants)
                .HasForeignKey(cp => cp.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ConversationParticipant>()
                .HasOne(cp => cp.User)
                .WithMany()
                .HasForeignKey(cp => cp.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // ProjectTechnology — составной ключ
            builder.Entity<ProjectTechnology>()
                .HasKey(pt => new { pt.ProjectId, pt.TechnologyId });

            // Favorite
            builder.Entity<Favorite>()
                .HasOne(f => f.User)
                .WithMany(u => u.Favorites)
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Favorite>()
                .HasOne(f => f.Project)
                .WithMany(p => p.Favorites)
                .HasForeignKey(f => f.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            // Conversation — две связи с ApplicationUser
            builder.Entity<Conversation>()
                .HasOne(c => c.User1)
                .WithMany()
                .HasForeignKey(c => c.User1Id)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Conversation>()
                .HasOne(c => c.User2)
                .WithMany()
                .HasForeignKey(c => c.User2Id)
                .OnDelete(DeleteBehavior.Restrict);

            // Message → Conversation
            builder.Entity<Message>()
                .HasOne(m => m.Conversation)
                .WithMany(c => c.Messages)
                .HasForeignKey(m => m.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            // Message → Sender
            builder.Entity<Message>()
                .HasOne(m => m.Sender)
                .WithMany()
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);


            // === НОВЫЕ КОНФИГУРАЦИИ ДЛЯ ЧАТА ===

            // UserBlock
            builder.Entity<UserBlock>()
                .HasOne(b => b.Blocker)
                .WithMany()
                .HasForeignKey(b => b.BlockerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<UserBlock>()
                .HasOne(b => b.BlockedUserRef)
                .WithMany()
                .HasForeignKey(b => b.BlockedUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<UserBlock>()
                .HasIndex(b => new { b.BlockerId, b.BlockedUserId })
                .IsUnique();

            // GroupChat
            builder.Entity<GroupChat>()
                .HasOne(g => g.Creator)
                .WithMany()
                .HasForeignKey(g => g.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);

            // GroupMember
            builder.Entity<GroupMember>()
                .HasOne(m => m.GroupChat)
                .WithMany(g => g.Members)
                .HasForeignKey(m => m.GroupChatId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<GroupMember>()
                .HasOne(m => m.User)
                .WithMany()
                .HasForeignKey(m => m.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<GroupMember>()
                .HasIndex(m => new { m.GroupChatId, m.UserId })
                .IsUnique();

            // GroupMessage
            builder.Entity<GroupMessage>()
                .HasOne(m => m.GroupChat)
                .WithMany(g => g.Messages)
                .HasForeignKey(m => m.GroupChatId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<GroupMessage>()
                .HasOne(m => m.Sender)
                .WithMany()
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);


            // === НОВЫЕ КОНФИГУРАЦИИ ДЛЯ ПОДПИСОК И ЖАЛОБ ===

            // UserSubscription
            builder.Entity<UserSubscription>()
                .HasOne(s => s.Subscriber)
                .WithMany(u => u.Subscriptions)
                .HasForeignKey(s => s.SubscriberId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<UserSubscription>()
                .HasOne(s => s.FollowedUser)
                .WithMany(u => u.Subscribers)
                .HasForeignKey(s => s.FollowedUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<UserSubscription>()
                .HasIndex(s => new { s.SubscriberId, s.FollowedUserId })
                .IsUnique();

            // UserReport
            builder.Entity<UserReport>()
                .HasOne(r => r.Reporter)
                .WithMany()
                .HasForeignKey(r => r.ReporterId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<UserReport>()
                .HasOne(r => r.ReportedUser)
                .WithMany()
                .HasForeignKey(r => r.ReportedUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<UserReport>()
                .HasIndex(r => new { r.ReporterId, r.ReportedUserId, r.ReportedAt });
        }
    }
}
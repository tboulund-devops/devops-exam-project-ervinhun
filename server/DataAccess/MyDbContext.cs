using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace server.DataAccess;

public partial class MyDbContext : DbContext
{
    public MyDbContext(DbContextOptions<MyDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<TaskComment> TaskComments { get; set; }

    public virtual DbSet<TaskHistory> TaskHistories { get; set; }

    public virtual DbSet<TaskItem> TaskItems { get; set; }

    public virtual DbSet<TodoTaskStatus> TodoTaskStatuses { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("pgcrypto");

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("notifications_pkey");

            entity.ToTable("notifications");

            entity.HasIndex(e => e.UserId, "idx_notifications_user_id");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.IsRead).HasColumnName("is_read");
            entity.Property(e => e.Message).HasColumnName("message");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("notifications_user_id_fkey");
        });

        modelBuilder.Entity<TaskComment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("task_comments_pkey");

            entity.ToTable("task_comments");

            entity.HasIndex(e => e.TaskId, "idx_task_comments_task_id");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.Content).HasColumnName("content");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            entity.Property(e => e.TaskId).HasColumnName("task_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Task).WithMany(p => p.TaskComments)
                .HasForeignKey(d => d.TaskId)
                .HasConstraintName("task_comments_task_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.TaskComments)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("task_comments_user_id_fkey");
        });

        modelBuilder.Entity<TaskHistory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("task_history_pkey");

            entity.ToTable("task_history");

            entity.HasIndex(e => e.TaskId, "idx_task_history_task_id");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.ChangedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("changed_at");
            entity.Property(e => e.ChangedBy).HasColumnName("changed_by");
            entity.Property(e => e.FromStatusId).HasColumnName("from_status_id");
            entity.Property(e => e.TaskId).HasColumnName("task_id");
            entity.Property(e => e.ToStatusId).HasColumnName("to_status_id");

            entity.HasOne(d => d.ChangedByNavigation).WithMany(p => p.TaskHistories)
                .HasForeignKey(d => d.ChangedBy)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("task_history_changed_by_fkey");

            entity.HasOne(d => d.FromStatus).WithMany(p => p.TaskHistoryFromStatuses)
                .HasForeignKey(d => d.FromStatusId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("task_history_from_status_id_fkey");

            entity.HasOne(d => d.Task).WithMany(p => p.TaskHistories)
                .HasForeignKey(d => d.TaskId)
                .HasConstraintName("task_history_task_id_fkey");

            entity.HasOne(d => d.ToStatus).WithMany(p => p.TaskHistoryToStatuses)
                .HasForeignKey(d => d.ToStatusId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("task_history_to_status_id_fkey");
        });

        modelBuilder.Entity<TaskItem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("task_item_pkey");

            entity.ToTable("task_item");

            entity.HasIndex(e => e.AssigneeId, "idx_task_item_assignee_id");

            entity.HasIndex(e => e.CreatedAt, "idx_task_item_created_at");

            entity.HasIndex(e => e.Id, "idx_task_item_not_deleted").HasFilter("(deleted_at IS NULL)");

            entity.HasIndex(e => e.SearchVector, "idx_task_item_search_vector").HasMethod("gin");

            entity.HasIndex(e => e.StatusId, "idx_task_item_status_id");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.AssigneeId).HasColumnName("assignee_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.DueDate).HasColumnName("due_date");
            entity.Property(e => e.SearchVector)
                .HasComputedColumnSql("to_tsvector('english'::regconfig, ((COALESCE(title, ''::text) || ' '::text) || COALESCE(description, ''::text)))", true)
                .HasColumnName("search_vector");
            entity.Property(e => e.StatusId).HasColumnName("status_id");
            entity.Property(e => e.Title).HasColumnName("title");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Assignee).WithMany(p => p.TaskItems)
                .HasForeignKey(d => d.AssigneeId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("task_item_assignee_id_fkey");

            entity.HasOne(d => d.Status).WithMany(p => p.TaskItems)
                .HasForeignKey(d => d.StatusId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("task_item_status_id_fkey");
        });

        modelBuilder.Entity<TodoTaskStatus>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("todo_task_status_pkey");

            entity.ToTable("todo_task_status");

            entity.HasIndex(e => e.Name, "todo_task_status_name_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            entity.Property(e => e.Name).HasColumnName("name");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("users_pkey");

            entity.ToTable("users");

            entity.HasIndex(e => e.Id, "idx_users_not_deleted").HasFilter("(deleted_at IS NULL)");

            entity.HasIndex(e => e.Email, "users_email_key").IsUnique();

            entity.HasIndex(e => e.Username, "users_username_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            entity.Property(e => e.Email).HasColumnName("email");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.Username).HasColumnName("username");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}

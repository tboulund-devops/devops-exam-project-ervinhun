export interface UserDto {
    id: string;
    username: string;
}

export interface TaskDto {
    id: string;
    title: string;
    description?: string;
    createdAt: string;
    dueDate?: string;
    status: string;
    assignee?: UserDto;
}

export interface CreateTaskRequest {
    title: string;
    description?: string;
    assigneeId?: string;
    dueDate?: string;
}

export interface UpdateTaskRequest {
    title: string;
    description?: string;
    assigneeId?: string;
    dueDate?: string;
}

export interface MoveTaskRequest {
    taskId: string;
    newStatusId: string;
    changedByUserId: string;
}

export interface TaskCommentDto {
    id: string;
    taskId: string;
    content: string;
    createdAt: string;
    userId?: string;
    username?: string;
}

export interface CreateTaskCommentRequest {
    content: string;
    userId?: string;
}
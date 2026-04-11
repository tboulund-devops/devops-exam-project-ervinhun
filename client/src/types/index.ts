export interface UserDto {
    id: string;
    username: string;
}

export interface TaskDto {
    id: string;
    title: string;
    description?: string;
    createdAt: string;
    status: string;
    assignee?: UserDto;
}

export interface CreateTaskRequest {
    title: string;
    description?: string;
    assigneeId?: string;
}

export interface UpdateTaskRequest {
    title: string;
    description?: string;
    assigneeId?: string;
}

export interface MoveTaskRequest {
    taskId: string;
    newStatusId: string;
    changedByUserId: string;
}
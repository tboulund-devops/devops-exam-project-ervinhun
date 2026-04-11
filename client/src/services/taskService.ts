import { apiFetch } from './api';
import type { TaskDto, UserDto, CreateTaskRequest, UpdateTaskRequest } from '../types';

export interface StatusDto {
    id: string;
    name: string;
}

export const getUsers = () =>
    apiFetch<UserDto[]>('/Task/Users');

export const getTasks = () =>
    apiFetch<TaskDto[]>('/Task/GetTasks');

export const getStatuses = () =>
    apiFetch<StatusDto[]>('/Task/Statuses');

export const getTaskById = (id: string) =>
    apiFetch<TaskDto>(`/Task/GetTaskById?id=${id}`);

export const createTask = (data: CreateTaskRequest) =>
    apiFetch<TaskDto>('/Task/CreateTask', {
        method: 'POST',
        body: JSON.stringify(data),
    });

export const updateTask = (id: string, data: UpdateTaskRequest) =>
    apiFetch<TaskDto>(`/Task/UpdateTask?id=${id}`, {
        method: 'PUT',
        body: JSON.stringify(data),
    });

export const moveTask = (taskId: string, newStatusId: string, changedByUserId: string) =>
    apiFetch<TaskDto>('/Task/MoveTask', {
        method: 'POST',
        body: JSON.stringify({ taskId, newStatusId, changedByUserId }),
    });

export const deleteTask = (id: string) =>
    apiFetch<void>(`/Task/DeleteTask?id=${id}`, { method: 'DELETE' });
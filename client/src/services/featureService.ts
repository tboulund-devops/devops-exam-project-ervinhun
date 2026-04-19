import { apiFetch } from './api'

export interface FeatureFlags {
    archiveTask: boolean;
    getArchivedTasks: boolean;
    unarchiveTask: boolean;
    createTask: boolean;
    updateTask: boolean;
    deleteTask: boolean;
    moveTask: boolean;
    getTasks: boolean;
    taskExpiry: boolean;
}

export const getFeatureFlags = () => 
    apiFetch<FeatureFlags>('/Feature');
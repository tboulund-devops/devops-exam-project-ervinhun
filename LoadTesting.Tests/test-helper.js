import http from 'k6/http';
import { check, sleep } from 'k6';

export const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';
export const params = {
    headers: {
        'Accept': 'application/json',
        'Content-Type': 'application/json',
        'X-Test-Mode': 'true'
    },
};

export const endpoints = [
    { path: '/api/Task/Users', needsId: false, type: 'array', method: 'GET' },
    { path: '/api/Task/GetTasks', needsId: false, type: 'array', method: 'GET' },
    { path: '/api/Task/GetTaskById', needsId: true, type: 'object', method: 'GET' },
    { path: '/api/Task/CreateTask', needsId: false, type: 'object', method: 'POST' },
];

export function getRandom(arr) {
    return arr[Math.floor(Math.random() * arr.length)];
}

export function setupTasks() {
    const res = http.get(`${BASE_URL}/api/Task/GetTasks`, params);
    let tasks = [];
    try { tasks = res.json(); } catch(e) {}
    if (!Array.isArray(tasks)) tasks = [];
    return tasks.map(t => t.id).filter(Boolean).slice(0, 10);
}

export function callEndpoint(endpoint, existingTaskIds) {
    let url = `${BASE_URL}${endpoint.path}`;
    if (endpoint.method === 'GET' && endpoint.needsId) {
        if (!existingTaskIds || existingTaskIds.length === 0) return null;
        url += `?id=${getRandom(existingTaskIds)}`;
    }

    let res;
    if (endpoint.method === 'GET') {
        res = http.get(url, params);
    } else if (endpoint.method === 'POST' && Math.random() < 0.05) {
        const payload = JSON.stringify({
            title: `Test Task ${Math.random().toString(36).substring(2,7)}`,
            description: 'Random description',
            assigneeId: null
        });
        res = http.post(url, payload, params);
        // Delete after creation if backend allows
        http.del(`${BASE_URL}/api/Task/DeleteTask?id=${res.json().id}`, params);
    } else return null;

    let data;
    try { data = res.json(); } catch(e) { data = null; }

    check(res, {
        'status is 200 or 201': r => r.status === 200 || r.status === 201,
        'valid array': () => endpoint.type !== 'array' || Array.isArray(data),
        'valid object': () => endpoint.type !== 'object' || (data && data.id),
    });

    sleep(Math.random() * 2);
    return res;
}
import { endpoints, setupTasks, callEndpoint, getRandom } from './test-helper.js';

export const options = {
    vus: 10,
    duration: '30s',
    thresholds: {
        http_req_duration: ['p(95)<1000'],
        http_req_failed: ['rate<0.05'],
    },
};

export function setup() {
    return setupTasks();
}

export default function(existingTaskIds) {
    const endpoint = getRandom(endpoints);
    callEndpoint(endpoint, existingTaskIds);
}


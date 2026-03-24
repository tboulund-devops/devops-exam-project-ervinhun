import { endpoints, setupTasks, callEndpoint, getRandom } from './test-helper.js';

export const options = {
    stages: [
        { target: 0, duration: '30s' },      // idle
        { target: 200, duration: '30s' },    // sudden spike
        { target: 200, duration: '3m' },    // hold spike
        { target: 0, duration: '2m' },      // ramp down
    ],
};

export function setup() {
    return setupTasks();
}

export default function(existingTaskIds) {
    const endpoint = getRandom(endpoints);
    callEndpoint(endpoint, existingTaskIds);
}
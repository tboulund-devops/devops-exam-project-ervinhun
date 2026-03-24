import { endpoints, setupTasks, callEndpoint, getRandom } from './test-helper.js';

export const options = {
    stages: [
        { target: 0, duration: '1m' },      // ramp up from idle
        { target: 500, duration: '2m' },    // high load
        { target: 1000, duration: '3m' },   // peak load
        { target: 1000, duration: '5m' },   // hold peak
        { target: 0, duration: '3m' },      // ramp down
    ],
};

export function setup() {
    return setupTasks();
}

export default function(existingTaskIds) {
    const endpoint = getRandom(endpoints);
    callEndpoint(endpoint, existingTaskIds);
}
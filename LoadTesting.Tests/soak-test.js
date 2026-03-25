import { endpoints, setupTasks, callEndpoint, getRandom } from './test-helper.js';

export const options = {
    stages: [
        { target: 50, duration: '2m' },   // ramp up
        { target: 50, duration: '30m' },  // long soak
        { target: 0, duration: '2m' },    // ramp down
    ],
};

export function setup() {
    return setupTasks();
}

export default function(existingTaskIds) {
    const endpoint = getRandom(endpoints);
    callEndpoint(endpoint, existingTaskIds);
}
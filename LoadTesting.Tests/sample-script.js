// To run on Windows: cat script.js | docker run --rm -i grafana/k6 run -



import http from 'k6/http';
import { sleep, check } from 'k6';

const BASE_URL = __ENV.BASE_URL || 'http://host.docker.internal:5000';

const params = {
  headers: {
    'Accept': 'application/json',
	'X-Test-Mode': 'true'
  },
};

const endpoints = [
	{ path: '/api/Task/Users', needsId: false, type: 'array', method: 'GET' },
	{ path: '/api/Task/GetTasks', needsId: false, type: 'array', method: 'GET' },
	{ path: '/api/Task/GetTasks', needsId: false, type: 'array', method: 'GET' },
	{ path: '/api/Task/GetTasks', needsId: false, type: 'array', method: 'GET' },
	{ path: '/api/History/GetTaskHistory?pageNumber=0&pageSize=10', needsId: false, type: 'array', method: 'GET' },
	{ path: '/api/History/GetTaskDetailHistory', needsId: false, type: 'array', method: 'GET' },
	{ path: '/api/Task/GetTaskById', needsId: true, type: 'object', method: 'GET' },
	{ path: '/api/Task/CreateTask', needsId: false, type: 'object', method: 'POST' } // POST endpoint
	
];

export function setup() {
    const res = http.get(`${BASE_URL}/api/Task/GetTasks`, params);
    let existingTaskIds = [];
    try {
        existingTaskIds = res.json();
    } catch (e) {
        console.error('Failed to parse task list:', e);
    }
    // return array of IDs to be available in default function
    return existingTaskIds.map(t => t.id).slice(0, 10);
}

function getRandom(arr) {
	return arr[Math.floor(Math.random() * arr.length)];
}

export const options = {
	thresholds: {
		http_req_duration: ['p(95)<500'], // 95% of requests under 500ms
		http_req_failed: ['rate<0.01'],   // <1% errors
	},
	stages: [
		{ target: 100, duration: '1m' },  // ramp up
		{ target: 100, duration: '5m' },  // heavy usage
		{ target: 20, duration: '1m' }, // ramp up
		{ target: 20, duration: '5m' }, // stable
		{ target: 0, duration: '2m' }, //ramp-down to 0 users
	],
};



export default function(existingTaskIds) {
  const endpoint = getRandom(endpoints);

  // If GET by ID
    let url = `${BASE_URL}${endpoint.path}`;
    if (endpoint.method === 'GET' && endpoint.needsId) {
        if (existingTaskIds.length === 0) return; // skip if no IDs available
        url += `?id=${getRandom(existingTaskIds)}`;
    }

  let res;

  if (endpoint.method === 'GET') {
    res = http.get(url, params);
  } else if (endpoint.method === 'POST') {
    // occasional POST probability, e.g., 5% of iterations
    if (Math.random() < 0.05) {
      const payload = JSON.stringify({
        title: `Test Task ${Math.random().toString(36).substring(2, 7)}`,
        description: 'No description or random description',
        assigneeId: null
      });

      res = http.post(url, payload, params);

      // Optional: delete after creation if backend allows
      // http.del(`${BASE_URL}/api/Task/DeleteTask?id=${res.json().id}`, params);
    } else {
      // skip POST this iteration and return early
      return;
    }
  }

  let data;
  try {
    data = res.json();
  } catch (e) {
    data = null;
  }

  check(res, {
    'status is 200': (r) => r.status === 200 || r.status === 201,
    'valid array response': () =>
      endpoint.type !== 'array' || (Array.isArray(data) && data.length > 0),
    'valid object response': () =>
      endpoint.type !== 'object' || (data && data.id !== undefined),
    'valid paged response': () =>
      endpoint.type !== 'paged' || (data && data.items && Array.isArray(data.items)),
  });

  sleep(Math.random() * 2);
}

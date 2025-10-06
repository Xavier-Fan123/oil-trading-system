import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate } from 'k6/metrics';

// Custom metrics
export let errorRate = new Rate('errors');

// Test configuration
export let options = {
  stages: [
    // Warm up
    { duration: '2m', target: 10 },
    // Ramp up to peak load
    { duration: '5m', target: 50 },
    // Stay at peak load
    { duration: '10m', target: 50 },
    // Ramp down
    { duration: '3m', target: 0 },
  ],
  thresholds: {
    http_req_duration: ['p(95)<500'], // 95% of requests should complete within 500ms
    http_req_failed: ['rate<0.1'],    // Error rate should be less than 10%
    errors: ['rate<0.1'],
  },
};

// Base URL configuration
const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';

// Test data
const testUsers = [
  { username: 'trader1@example.com', password: 'Test123!' },
  { username: 'trader2@example.com', password: 'Test123!' },
  { username: 'manager1@example.com', password: 'Test123!' },
];

const contractData = {
  supplierId: '3fa85f64-5717-4562-b3fc-2c963f66afa6',
  productId: '3fa85f64-5717-4562-b3fc-2c963f66afa6',
  traderId: '3fa85f64-5717-4562-b3fc-2c963f66afa6',
  quantity: {
    value: 1000,
    unit: 'MT'
  },
  priceFormula: {
    formula: 'FIXED',
    method: 'Fixed',
    fixedPrice: 75.50
  },
  laycanStart: '2024-03-01T00:00:00Z',
  laycanEnd: '2024-03-31T23:59:59Z',
  deliveryTerms: 'FOB',
  settlementType: 'TT'
};

// Helper function to authenticate and get token
function authenticate() {
  const user = testUsers[Math.floor(Math.random() * testUsers.length)];
  
  const loginResponse = http.post(`${BASE_URL}/api/auth/login`, JSON.stringify({
    email: user.username,
    password: user.password
  }), {
    headers: { 'Content-Type': 'application/json' },
  });

  const authCheck = check(loginResponse, {
    'login successful': (r) => r.status === 200,
    'token received': (r) => r.json('token') !== undefined,
  });

  if (!authCheck) {
    errorRate.add(1);
    return null;
  }

  return loginResponse.json('token');
}

// Main test scenario
export default function () {
  const token = authenticate();
  
  if (!token) {
    console.log('Authentication failed, skipping user journey');
    return;
  }

  const headers = {
    'Content-Type': 'application/json',
    'Authorization': `Bearer ${token}`,
  };

  // Test 1: Health Check
  const healthResponse = http.get(`${BASE_URL}/health`);
  check(healthResponse, {
    'health check status is 200': (r) => r.status === 200,
    'health check response time < 100ms': (r) => r.timings.duration < 100,
  });

  sleep(1);

  // Test 2: Get Dashboard Data
  const dashboardResponse = http.get(`${BASE_URL}/api/dashboard`, { headers });
  check(dashboardResponse, {
    'dashboard data retrieved': (r) => r.status === 200,
    'dashboard response time < 500ms': (r) => r.timings.duration < 500,
  });

  sleep(1);

  // Test 3: Get Contracts List
  const contractsResponse = http.get(`${BASE_URL}/api/purchase-contracts`, { headers });
  check(contractsResponse, {
    'contracts list retrieved': (r) => r.status === 200,
    'contracts response time < 300ms': (r) => r.timings.duration < 300,
  });

  sleep(1);

  // Test 4: Create New Contract
  const createContractResponse = http.post(
    `${BASE_URL}/api/purchase-contracts`,
    JSON.stringify(contractData),
    { headers }
  );
  
  const contractCheck = check(createContractResponse, {
    'contract created successfully': (r) => r.status === 201,
    'contract creation time < 1000ms': (r) => r.timings.duration < 1000,
  });

  let contractId = null;
  if (contractCheck) {
    contractId = createContractResponse.json('id');
  } else {
    errorRate.add(1);
  }

  sleep(1);

  // Test 5: Get Risk Calculations
  const riskResponse = http.get(`${BASE_URL}/api/risk/calculate`, { headers });
  check(riskResponse, {
    'risk calculation completed': (r) => r.status === 200,
    'risk calculation time < 2000ms': (r) => r.timings.duration < 2000,
  });

  sleep(1);

  // Test 6: Get Price Data
  const priceResponse = http.get(`${BASE_URL}/api/prices/latest?productType=Brent`, { headers });
  check(priceResponse, {
    'price data retrieved': (r) => r.status === 200,
    'price data response time < 200ms': (r) => r.timings.duration < 200,
  });

  sleep(1);

  // Test 7: Update Contract (if created successfully)
  if (contractId) {
    const updateData = {
      ...contractData,
      quantity: {
        value: 1500,
        unit: 'MT'
      }
    };

    const updateResponse = http.put(
      `${BASE_URL}/api/purchase-contracts/${contractId}`,
      JSON.stringify(updateData),
      { headers }
    );

    check(updateResponse, {
      'contract updated successfully': (r) => r.status === 200,
      'contract update time < 800ms': (r) => r.timings.duration < 800,
    });

    sleep(1);

    // Test 8: Get Updated Contract
    const getContractResponse = http.get(
      `${BASE_URL}/api/purchase-contracts/${contractId}`,
      { headers }
    );

    check(getContractResponse, {
      'updated contract retrieved': (r) => r.status === 200,
      'get contract time < 200ms': (r) => r.timings.duration < 200,
      'contract quantity updated': (r) => {
        const contract = r.json();
        return contract.quantity && contract.quantity.value === 1500;
      },
    });
  }

  sleep(1);

  // Test 9: Get Trading Partners
  const partnersResponse = http.get(`${BASE_URL}/api/trading-partners`, { headers });
  check(partnersResponse, {
    'trading partners retrieved': (r) => r.status === 200,
    'partners response time < 300ms': (r) => r.timings.duration < 300,
  });

  sleep(1);

  // Test 10: Get Products
  const productsResponse = http.get(`${BASE_URL}/api/products`, { headers });
  check(productsResponse, {
    'products retrieved': (r) => r.status === 200,
    'products response time < 200ms': (r) => r.timings.duration < 200,
  });

  sleep(2);
}

// Stress test scenario
export function stressTest() {
  const token = authenticate();
  
  if (!token) {
    return;
  }

  const headers = {
    'Content-Type': 'application/json',
    'Authorization': `Bearer ${token}`,
  };

  // Rapid-fire requests to test system limits
  for (let i = 0; i < 10; i++) {
    const response = http.get(`${BASE_URL}/api/dashboard`, { headers });
    check(response, {
      [`stress test ${i + 1} successful`]: (r) => r.status === 200,
    });
    
    if (response.status !== 200) {
      errorRate.add(1);
    }
  }
}

// Spike test scenario  
export function spikeTest() {
  const token = authenticate();
  
  if (!token) {
    return;
  }

  const headers = {
    'Content-Type': 'application/json',
    'Authorization': `Bearer ${token}`,
  };

  // Concurrent contract creation to test spike handling
  const promises = [];
  for (let i = 0; i < 5; i++) {
    const contractDataSpike = {
      ...contractData,
      quantity: {
        value: 1000 + i * 100,
        unit: 'MT'
      }
    };

    promises.push(
      http.post(
        `${BASE_URL}/api/purchase-contracts`,
        JSON.stringify(contractDataSpike),
        { headers }
      )
    );
  }

  // Wait for all requests to complete
  const responses = Promise.all(promises);
  
  responses.forEach((response, index) => {
    check(response, {
      [`spike test contract ${index + 1} created`]: (r) => r.status === 201,
    });
  });
}

// Test configuration for different scenarios
export let scenarios = {
  // Normal load test
  normal_load: {
    executor: 'ramping-vus',
    startVUs: 0,
    stages: [
      { duration: '2m', target: 10 },
      { duration: '5m', target: 20 },
      { duration: '5m', target: 20 },
      { duration: '2m', target: 0 },
    ],
    gracefulRampDown: '30s',
  },
  
  // Stress test
  stress_test: {
    executor: 'constant-vus',
    vus: 50,
    duration: '2m',
    exec: 'stressTest',
  },
  
  // Spike test
  spike_test: {
    executor: 'ramping-vus',
    startVUs: 0,
    stages: [
      { duration: '10s', target: 0 },
      { duration: '10s', target: 100 }, // Rapid spike
      { duration: '10s', target: 0 },
    ],
    exec: 'spikeTest',
  },
};

// Performance test results summary
export function handleSummary(data) {
  return {
    'performance-summary.json': JSON.stringify(data, null, 2),
    stdout: `
=====================================
🎯 PERFORMANCE TEST RESULTS SUMMARY
=====================================

📊 Key Metrics:
• Total Requests: ${data.metrics.http_reqs.values.count}
• Failed Requests: ${data.metrics.http_req_failed.values.rate * 100}%
• Average Response Time: ${data.metrics.http_req_duration.values.avg}ms
• 95th Percentile: ${data.metrics.http_req_duration.values['p(95)']}ms
• 99th Percentile: ${data.metrics.http_req_duration.values['p(99)']}ms

🔍 Thresholds Status:
${Object.entries(data.thresholds)
  .map(([name, threshold]) => 
    `• ${name}: ${threshold.ok ? '✅ PASSED' : '❌ FAILED'}`
  )
  .join('\n')}

⚡ Throughput:
• Requests per second: ${data.metrics.http_reqs.values.rate}/s
• Data received: ${(data.metrics.data_received.values.count / 1024 / 1024).toFixed(2)} MB
• Data sent: ${(data.metrics.data_sent.values.count / 1024 / 1024).toFixed(2)} MB

📈 Performance Rating:
${getPerformanceRating(data.metrics.http_req_duration.values['p(95)'])}

=====================================
    `,
  };
}

function getPerformanceRating(p95ResponseTime) {
  if (p95ResponseTime < 200) return '🟢 EXCELLENT (< 200ms)';
  if (p95ResponseTime < 500) return '🟡 GOOD (< 500ms)';
  if (p95ResponseTime < 1000) return '🟠 ACCEPTABLE (< 1000ms)';
  return '🔴 NEEDS IMPROVEMENT (> 1000ms)';
}
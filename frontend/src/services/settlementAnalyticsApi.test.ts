/**
 * Settlement Analytics API Service - Unit Tests
 *
 * Comprehensive unit tests for the settlementAnalyticsApi service layer
 * Tests API method calls, response handling, parameter passing, and error scenarios
 *
 * Setup:
 * 1. Install testing dependencies: npm install --save-dev vitest msw @testing-library/react
 * 2. Run tests: npm run test
 */

import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import axios from 'axios';
import {
  settlementAnalyticsApi,
  SettlementDashboardSummary,
  SettlementAnalytics,
  SettlementMetrics,
} from './settlementAnalyticsApi';

// Mock axios
vi.mock('axios', () => ({
  default: {
    create: vi.fn(() => ({
      get: vi.fn(),
      post: vi.fn(),
    })),
  },
}));

/**
 * Mock data for API response testing
 */
const mockAnalyticsData: SettlementAnalytics = {
  totalSettlements: 10,
  totalAmount: 1000000,
  averageAmount: 100000,
  minimumAmount: 50000,
  maximumAmount: 200000,
  settlementsByStatus: {
    Finalized: 7,
    Approved: 2,
    Calculated: 1,
  },
  settlementsByCurrency: {
    USD: 900000,
    EUR: 100000,
  },
  settlementsByType: {
    Telegraphic: 8,
    LetterOfCredit: 2,
  },
  averageProcessingTimeDays: 5.5,
  slaComplianceRate: 95.0,
  dailyTrends: [
    {
      date: '2025-11-05',
      settlementCount: 2,
      totalAmount: 200000,
      completedCount: 2,
      pendingCount: 0,
    },
    {
      date: '2025-11-06',
      settlementCount: 3,
      totalAmount: 300000,
      completedCount: 2,
      pendingCount: 1,
    },
  ],
  currencyBreakdown: [
    {
      currency: 'USD',
      settlementCount: 8,
      totalAmount: 900000,
      percentageOfTotal: 90,
    },
    {
      currency: 'EUR',
      settlementCount: 2,
      totalAmount: 100000,
      percentageOfTotal: 10,
    },
  ],
  topPartners: [
    {
      partnerId: 'partner-1',
      partnerName: 'UNION INTERNATIONAL TRADING',
      settlementType: 'Purchase',
      settlementCount: 5,
      totalAmount: 500000,
      averageAmount: 100000,
    },
  ],
  statusDistribution: [
    {
      status: 'Finalized',
      count: 7,
      percentage: 70,
    },
  ],
};

const mockMetricsData: SettlementMetrics = {
  totalSettlementValue: 1000000,
  totalSettlementCount: 10,
  successRate: 95.0,
  slaComplianceRate: 95.0,
  settlementValueTrend: 5.2,
  settlementCountTrend: 3.1,
  settlementsWithErrors: 0,
  averageProcessingTime: 5.5,
  errorRate: 0.0,
  completionRate: 90.0,
};

const mockDashboardSummary: SettlementDashboardSummary = {
  analytics: mockAnalyticsData,
  metrics: mockMetricsData,
  generatedAt: '2025-11-08T12:00:00Z',
  analysisPeriodDays: 30,
};

describe('settlementAnalyticsApi Service', () => {
  /**
   * Test 1: getAnalytics with default parameters
   * Validates API call to /analytics endpoint with default 30-day period
   */
  it('should call getAnalytics endpoint with default parameters', async () => {
    // Arrange
    const mockGet = vi.fn().mockResolvedValue({ data: mockAnalyticsData });
    vi.mocked(axios.create).mockReturnValue({ get: mockGet } as any);

    // Act
    const result = await settlementAnalyticsApi.getAnalytics();

    // Assert
    expect(mockGet).toHaveBeenCalledWith(
      '/analytics',
      expect.objectContaining({
        params: expect.objectContaining({
          daysToAnalyze: 30,
        }),
      })
    );
    expect(result).toEqual(mockAnalyticsData);
  });

  /**
   * Test 2: getAnalytics with custom daysToAnalyze parameter
   * Validates parameter passing for date range customization
   */
  it('should pass custom daysToAnalyze parameter to API', async () => {
    // Arrange
    const mockGet = vi.fn().mockResolvedValue({ data: mockAnalyticsData });
    vi.mocked(axios.create).mockReturnValue({ get: mockGet } as any);

    // Act
    const result = await settlementAnalyticsApi.getAnalytics(7);

    // Assert
    expect(mockGet).toHaveBeenCalledWith(
      '/analytics',
      expect.objectContaining({
        params: expect.objectContaining({
          daysToAnalyze: 7,
        }),
      })
    );
  });

  /**
   * Test 3: getAnalytics with all optional parameters
   * Validates all filtering parameters are properly serialized
   */
  it('should include all filter parameters in API request', async () => {
    // Arrange
    const mockGet = vi.fn().mockResolvedValue({ data: mockAnalyticsData });
    vi.mocked(axios.create).mockReturnValue({ get: mockGet } as any);

    // Act
    const result = await settlementAnalyticsApi.getAnalytics(
      14,
      true,
      'USD',
      'Finalized'
    );

    // Assert
    expect(mockGet).toHaveBeenCalledWith(
      '/analytics',
      expect.objectContaining({
        params: expect.objectContaining({
          daysToAnalyze: 14,
          isSalesSettlement: true,
          currency: 'USD',
          status: 'Finalized',
        }),
      })
    );
  });

  /**
   * Test 4: getMetrics endpoint call
   * Validates KPI metrics endpoint returns correct data structure
   */
  it('should call getMetrics endpoint and return metrics data', async () => {
    // Arrange
    const mockGet = vi.fn().mockResolvedValue({ data: mockMetricsData });
    vi.mocked(axios.create).mockReturnValue({ get: mockGet } as any);

    // Act
    const result = await settlementAnalyticsApi.getMetrics();

    // Assert
    expect(mockGet).toHaveBeenCalledWith('/metrics', expect.any(Object));
    expect(result).toEqual(mockMetricsData);
    expect(result.totalSettlementValue).toBe(1000000);
    expect(result.successRate).toBe(95.0);
  });

  /**
   * Test 5: getMetrics with custom analysis period
   * Validates period parameter affects metrics calculation
   */
  it('should pass daysToAnalyze parameter to getMetrics endpoint', async () => {
    // Arrange
    const mockGet = vi.fn().mockResolvedValue({ data: mockMetricsData });
    vi.mocked(axios.create).mockReturnValue({ get: mockGet } as any);

    // Act
    const result = await settlementAnalyticsApi.getMetrics(7);

    // Assert
    expect(mockGet).toHaveBeenCalledWith(
      '/metrics',
      expect.objectContaining({
        params: expect.objectContaining({
          daysToAnalyze: 7,
        }),
      })
    );
  });

  /**
   * Test 6: getDashboardSummary returns complete data structure
   * Validates primary dashboard endpoint combines analytics and metrics
   */
  it('should call getDashboardSummary and return combined analytics and metrics', async () => {
    // Arrange
    const mockGet = vi.fn().mockResolvedValue({ data: mockDashboardSummary });
    vi.mocked(axios.create).mockReturnValue({ get: mockGet } as any);

    // Act
    const result = await settlementAnalyticsApi.getDashboardSummary();

    // Assert
    expect(mockGet).toHaveBeenCalledWith(
      '/summary',
      expect.objectContaining({
        params: expect.objectContaining({
          daysToAnalyze: 30,
        }),
      })
    );
    expect(result).toEqual(mockDashboardSummary);
    expect(result.analytics).toBeDefined();
    expect(result.metrics).toBeDefined();
    expect(result.analysisPeriodDays).toBe(30);
  });

  /**
   * Test 7: getDailyTrends returns array of daily data points
   * Validates trend data structure for chart visualization
   */
  it('should call getDailyTrends endpoint and return array data', async () => {
    // Arrange
    const mockTrendData = mockAnalyticsData.dailyTrends;
    const mockGet = vi.fn().mockResolvedValue({ data: mockTrendData });
    vi.mocked(axios.create).mockReturnValue({ get: mockGet } as any);

    // Act
    const result = await settlementAnalyticsApi.getDailyTrends();

    // Assert
    expect(mockGet).toHaveBeenCalledWith(
      '/daily-trends',
      expect.objectContaining({
        params: expect.objectContaining({
          daysToAnalyze: 30,
        }),
      })
    );
    expect(Array.isArray(result)).toBe(true);
    expect(result.length).toBeGreaterThan(0);
  });

  /**
   * Test 8: getCurrencyBreakdown returns currency distribution data
   * Validates currency analysis endpoint response
   */
  it('should call getCurrencyBreakdown endpoint and return currency data', async () => {
    // Arrange
    const mockCurrencyData = mockAnalyticsData.currencyBreakdown;
    const mockGet = vi.fn().mockResolvedValue({ data: mockCurrencyData });
    vi.mocked(axios.create).mockReturnValue({ get: mockGet } as any);

    // Act
    const result = await settlementAnalyticsApi.getCurrencyBreakdown();

    // Assert
    expect(mockGet).toHaveBeenCalledWith(
      '/currency-breakdown',
      expect.objectContaining({
        params: expect.objectContaining({
          daysToAnalyze: 30,
        }),
      })
    );
    expect(Array.isArray(result)).toBe(true);
    expect(result[0]).toHaveProperty('currency');
    expect(result[0]).toHaveProperty('totalAmount');
    expect(result[0]).toHaveProperty('percentageOfTotal');
  });

  /**
   * Test 9: getStatusDistribution returns status breakdown data
   * Validates settlement status distribution endpoint
   */
  it('should call getStatusDistribution endpoint and return status data', async () => {
    // Arrange
    const mockStatusData = mockAnalyticsData.statusDistribution;
    const mockGet = vi.fn().mockResolvedValue({ data: mockStatusData });
    vi.mocked(axios.create).mockReturnValue({ get: mockGet } as any);

    // Act
    const result = await settlementAnalyticsApi.getStatusDistribution();

    // Assert
    expect(mockGet).toHaveBeenCalledWith(
      '/status-distribution',
      expect.objectContaining({
        params: expect.objectContaining({
          daysToAnalyze: 30,
        }),
      })
    );
    expect(Array.isArray(result)).toBe(true);
    expect(result[0]).toHaveProperty('status');
    expect(result[0]).toHaveProperty('count');
    expect(result[0]).toHaveProperty('percentage');
  });

  /**
   * Test 10: getTopPartners returns partner ranking data
   * Validates top trading partners endpoint response
   */
  it('should call getTopPartners endpoint and return partner ranking data', async () => {
    // Arrange
    const mockPartnerData = mockAnalyticsData.topPartners;
    const mockGet = vi.fn().mockResolvedValue({ data: mockPartnerData });
    vi.mocked(axios.create).mockReturnValue({ get: mockGet } as any);

    // Act
    const result = await settlementAnalyticsApi.getTopPartners();

    // Assert
    expect(mockGet).toHaveBeenCalledWith(
      '/top-partners',
      expect.objectContaining({
        params: expect.objectContaining({
          daysToAnalyze: 30,
        }),
      })
    );
    expect(Array.isArray(result)).toBe(true);
    expect(result[0]).toHaveProperty('partnerName');
    expect(result[0]).toHaveProperty('totalAmount');
    expect(result[0]).toHaveProperty('settlementCount');
  });

  /**
   * Test 11: API error handling - network error
   * Validates error is properly propagated on network failures
   */
  it('should propagate error when API request fails', async () => {
    // Arrange
    const apiError = new Error('Network Error: Cannot reach API');
    const mockGet = vi.fn().mockRejectedValue(apiError);
    vi.mocked(axios.create).mockReturnValue({ get: mockGet } as any);

    // Act & Assert
    await expect(settlementAnalyticsApi.getAnalytics()).rejects.toThrow(
      'Network Error'
    );
  });

  /**
   * Test 12: API error handling - server error response
   * Validates error handling for 500+ status codes
   */
  it('should handle server error responses', async () => {
    // Arrange
    const serverError = {
      response: {
        status: 500,
        data: { error: 'Internal Server Error' },
      },
      message: 'Server Error',
    };
    const mockGet = vi.fn().mockRejectedValue(serverError);
    vi.mocked(axios.create).mockReturnValue({ get: mockGet } as any);

    // Act & Assert
    await expect(settlementAnalyticsApi.getAnalytics()).rejects.toBeDefined();
  });

  /**
   * Test 13: Axios configuration validation
   * Validates API service is configured with correct base URL and timeout
   */
  it('should create axios instance with correct configuration', () => {
    // Arrange & Act
    // The api is created during module initialization
    expect(axios.create).toHaveBeenCalled();

    // Assert - Verify create was called with expected config
    const createCall = vi.mocked(axios.create).mock.calls[0]?.[0];
    expect(createCall).toEqual(
      expect.objectContaining({
        baseURL: 'http://localhost:5000/api',
        timeout: 30000,
      })
    );
  });

  /**
   * Test 14: Type safety - analytics response structure
   * Validates TypeScript types match API response
   */
  it('should maintain TypeScript type safety for analytics response', async () => {
    // Arrange
    const mockGet = vi.fn().mockResolvedValue({ data: mockAnalyticsData });
    vi.mocked(axios.create).mockReturnValue({ get: mockGet } as any);

    // Act
    const result = await settlementAnalyticsApi.getAnalytics();

    // Assert - Type checking happens at compile time, but we can verify structure
    expect(result).toHaveProperty('totalSettlements');
    expect(result).toHaveProperty('dailyTrends');
    expect(result).toHaveProperty('currencyBreakdown');
    expect(typeof result.totalSettlements).toBe('number');
    expect(Array.isArray(result.dailyTrends)).toBe(true);
  });

  /**
   * Test 15: Parameter edge cases - null/undefined handling
   * Validates API handles optional parameters gracefully
   */
  it('should handle null optional parameters gracefully', async () => {
    // Arrange
    const mockGet = vi.fn().mockResolvedValue({ data: mockAnalyticsData });
    vi.mocked(axios.create).mockReturnValue({ get: mockGet } as any);

    // Act
    const result = await settlementAnalyticsApi.getAnalytics(30, null, null, null);

    // Assert
    // Should not throw and should make successful call
    expect(mockGet).toHaveBeenCalled();
    expect(result).toBeDefined();
  });

  /**
   * Test 16: Concurrent API calls
   * Validates multiple simultaneous requests complete successfully
   */
  it('should handle concurrent API requests', async () => {
    // Arrange
    const mockGet = vi.fn();
    mockGet.mockResolvedValueOnce({ data: mockAnalyticsData }); // First call
    mockGet.mockResolvedValueOnce({ data: mockMetricsData }); // Second call
    mockGet.mockResolvedValueOnce({ data: mockDashboardSummary }); // Third call

    vi.mocked(axios.create).mockReturnValue({ get: mockGet } as any);

    // Act
    const [analytics, metrics, dashboard] = await Promise.all([
      settlementAnalyticsApi.getAnalytics(),
      settlementAnalyticsApi.getMetrics(),
      settlementAnalyticsApi.getDashboardSummary(),
    ]);

    // Assert
    expect(analytics).toEqual(mockAnalyticsData);
    expect(metrics).toEqual(mockMetricsData);
    expect(dashboard).toEqual(mockDashboardSummary);
    expect(mockGet).toHaveBeenCalledTimes(3);
  });

  /**
   * Test 17: Response data validation
   * Validates returned data matches expected structure exactly
   */
  it('should return data with correct structure and types', async () => {
    // Arrange
    const mockGet = vi.fn().mockResolvedValue({ data: mockDashboardSummary });
    vi.mocked(axios.create).mockReturnValue({ get: mockGet } as any);

    // Act
    const result = await settlementAnalyticsApi.getDashboardSummary();

    // Assert
    expect(result.analytics.totalSettlements).toBe(10);
    expect(result.metrics.successRate).toBe(95.0);
    expect(result.analysisPeriodDays).toBe(30);
    expect(new Date(result.generatedAt)).toBeInstanceOf(Date);
  });

  /**
   * Test 18: API request headers
   * Validates proper headers are sent with requests
   */
  it('should include proper headers in API requests', async () => {
    // Arrange
    const mockGet = vi.fn().mockResolvedValue({ data: mockAnalyticsData });
    vi.mocked(axios.create).mockReturnValue({ get: mockGet } as any);

    // Act
    await settlementAnalyticsApi.getAnalytics();

    // Assert
    // Verify axios was created with proper headers configuration
    const createCall = vi.mocked(axios.create).mock.calls[0]?.[0];
    expect(createCall).toHaveProperty('headers');
    expect(createCall?.headers).toEqual(
      expect.objectContaining({
        'Content-Type': 'application/json',
      })
    );
  });
});

/**
 * Integration Test Suite
 * Tests API service integration with actual component usage patterns
 */
describe('settlementAnalyticsApi Integration Tests', () => {
  /**
   * Integration Test 1: Complete data fetching flow
   * Validates typical dashboard data loading workflow
   */
  it('should support complete dashboard data loading flow', async () => {
    // Arrange
    const mockGet = vi.fn().mockResolvedValue({ data: mockDashboardSummary });
    vi.mocked(axios.create).mockReturnValue({ get: mockGet } as any);

    // Act
    const summary = await settlementAnalyticsApi.getDashboardSummary(30);

    // Assert
    expect(summary).toBeDefined();
    expect(summary.analytics.dailyTrends.length).toBeGreaterThan(0);
    expect(summary.metrics.totalSettlementValue).toBeGreaterThan(0);
  });

  /**
   * Integration Test 2: Error handling with fallback
   * Validates graceful error handling in component context
   */
  it('should allow error handling with fallback in component', async () => {
    // Arrange
    const apiError = new Error('API Unavailable');
    const mockGet = vi.fn().mockRejectedValue(apiError);
    vi.mocked(axios.create).mockReturnValue({ get: mockGet } as any);

    // Act & Assert
    try {
      await settlementAnalyticsApi.getDashboardSummary();
      // Should not reach here
      expect.fail('Should have thrown an error');
    } catch (error) {
      // Expected error path
      expect(error).toBeDefined();
    }
  });
});

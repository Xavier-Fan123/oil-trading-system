import React, { useState } from 'react';
import {
  Box,
  Typography,
  Tabs,
  Tab,
  Paper,
  Alert,
  CircularProgress,
  IconButton,
  Tooltip,
} from '@mui/material';
import {
  Refresh as RefreshIcon,
  Analytics as AnalyticsIcon,
  ViewList as ViewListIcon,
} from '@mui/icons-material';
import { 
 
  usePositionSummary, 
  usePositionAnalytics,
  useFilteredPositions
} from '@/hooks/usePositions';
import { PositionSummaryCards } from '@/components/Positions/PositionSummaryCards';
import { PositionsTable } from '@/components/Positions/PositionsTable';
import { PositionFiltersComponent } from '@/components/Positions/PositionFilters';
import { PositionCharts } from '@/components/Positions/PositionCharts';
import { PositionFilters } from '@/types/positions';

interface TabPanelProps {
  children?: React.ReactNode;
  index: number;
  value: number;
}

const TabPanel: React.FC<TabPanelProps> = ({ children, value, index }) => (
  <div hidden={value !== index} style={{ marginTop: 16 }}>
    {value === index && children}
  </div>
);

export const Positions: React.FC = () => {
  const [tabValue, setTabValue] = useState(0);
  const [filters, setFilters] = useState<PositionFilters>({
    showFlatPositions: false, // Default to hiding flat positions
  });

  // Fetch data using hooks
  const { 
    data: summary, 
    isLoading: summaryLoading, 
    error: summaryError,
    refetch: refetchSummary 
  } = usePositionSummary();

  const { 
    data: analytics, 
    isLoading: analyticsLoading,
    refetch: refetchAnalytics 
  } = usePositionAnalytics();

  const {
    positions,
    isLoading: positionsLoading,
    error: positionsError,
    totalCount,
    filteredCount
  } = useFilteredPositions(filters);

  const handleTabChange = (_event: React.SyntheticEvent, newValue: number) => {
    setTabValue(newValue);
  };

  const handleFiltersChange = (newFilters: PositionFilters) => {
    setFilters(newFilters);
  };

  const handleClearFilters = () => {
    setFilters({ showFlatPositions: false });
  };

  const handleRefresh = () => {
    refetchSummary();
    refetchAnalytics();
  };

  // Show error if critical data fails to load
  if (summaryError || positionsError) {
    return (
      <Box>
        <Typography variant="h4" gutterBottom>
          Position Management
        </Typography>
        <Alert severity="error">
          Error loading position data: {summaryError?.message || positionsError?.message}
        </Alert>
      </Box>
    );
  }

  return (
    <Box sx={{ p: 3 }}>
      {/* Header */}
      <Box display="flex" justifyContent="space-between" alignItems="center" mb={3}>
        <Typography variant="h4" component="h1">
          Position Management
        </Typography>
        <Box display="flex" alignItems="center" gap={1}>
          <Tooltip title="Refresh Data">
            <span>
              <IconButton 
                onClick={handleRefresh} 
                disabled={summaryLoading || positionsLoading}
                aria-label="Refresh positions data"
              >
                {summaryLoading || positionsLoading ? (
                  <CircularProgress size={24} />
                ) : (
                  <RefreshIcon />
                )}
              </IconButton>
            </span>
          </Tooltip>
        </Box>
      </Box>

      {/* Summary Cards */}
      {summary && (
        <Box mb={3}>
          <PositionSummaryCards summary={summary} isLoading={summaryLoading} />
        </Box>
      )}

      {/* Tabs */}
      <Paper sx={{ mb: 3 }}>
        <Tabs value={tabValue} onChange={handleTabChange}>
          <Tab 
            icon={<ViewListIcon />} 
            label="Positions List" 
            iconPosition="start"
          />
          <Tab 
            icon={<AnalyticsIcon />} 
            label="Analytics" 
            iconPosition="start"
          />
        </Tabs>
      </Paper>

      {/* Tab Panels */}
      <TabPanel value={tabValue} index={0}>
        {/* Filters */}
        <PositionFiltersComponent
          filters={filters}
          onFiltersChange={handleFiltersChange}
          onClearFilters={handleClearFilters}
        />

        {/* Results Summary */}
        <Box mb={2}>
          <Typography variant="body2" color="textSecondary">
            Showing {filteredCount} of {totalCount} positions
            {filteredCount !== totalCount && (
              <span> (filtered)</span>
            )}
          </Typography>
        </Box>

        {/* Positions Table */}
        <PositionsTable 
          positions={positions} 
          isLoading={positionsLoading} 
        />
      </TabPanel>

      <TabPanel value={tabValue} index={1}>
        {analyticsLoading ? (
          <Box display="flex" justifyContent="center" py={4}>
            <CircularProgress />
          </Box>
        ) : analytics ? (
          <PositionCharts analytics={analytics} />
        ) : (
          <Alert severity="info">
            No analytics data available. Please ensure you have active positions.
          </Alert>
        )}
      </TabPanel>
    </Box>
  );
};
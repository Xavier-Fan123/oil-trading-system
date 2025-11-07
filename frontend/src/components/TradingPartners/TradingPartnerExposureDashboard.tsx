import React, { useEffect, useState } from 'react';
import {
  Box,
  Container,
  Grid,
  Paper,
  TextField,
  MenuItem,
  Button,
  CircularProgress,
  Alert,
  Stack,
  Typography,
  Tab,
  Tabs,
} from '@mui/material';
import { useQuery } from '@tanstack/react-query';
import tradingPartnerExposureApi, {
  TradingPartnerExposureDto,
  PartnerSettlementSummaryDto,
} from '@/services/tradingPartnerExposureApi';
import ExposureCard from './ExposureCard';
import SettlementDetailsDialog from './SettlementDetailsDialog';
import { formatCurrency } from '@/utils/formatting';

interface TabPanelProps {
  children?: React.ReactNode;
  index: number;
  value: number;
}

const TabPanel = (props: TabPanelProps) => {
  const { children, value, index, ...other } = props;

  return (
    <div
      role="tabpanel"
      hidden={value !== index}
      id={`exposure-tabpanel-${index}`}
      aria-labelledby={`exposure-tab-${index}`}
      {...other}
    >
      {value === index && <Box sx={{ pt: 3 }}>{children}</Box>}
    </div>
  );
};

export const TradingPartnerExposureDashboard: React.FC = () => {
  const [selectedTab, setSelectedTab] = useState(0);
  const [sortBy, setSortBy] = useState<string>('riskLevel');
  const [sortDescending, setSortDescending] = useState(true);
  const [selectedPartner, setSelectedPartner] = useState<TradingPartnerExposureDto | null>(null);
  const [showSettlementDetails, setShowSettlementDetails] = useState(false);

  // Fetch all exposures
  const {
    data: allExposures = [],
    isLoading: isLoadingAll,
    error: errorAll,
    refetch: refetchAll,
  } = useQuery({
    queryKey: ['tradingPartnerExposures', sortBy, sortDescending],
    queryFn: () =>
      tradingPartnerExposureApi.getAllExposure(sortBy, sortDescending),
  });

  // Fetch at-risk partners
  const {
    data: atRiskPartners = [],
    isLoading: isLoadingAtRisk,
    error: errorAtRisk,
  } = useQuery({
    queryKey: ['atRiskPartners'],
    queryFn: () =>
      tradingPartnerExposureApi.getAtRiskPartners(3), // Risk level >= High (3)
  });

  // Fetch settlement details for selected partner
  const {
    data: settlementDetails,
    isLoading: isLoadingDetails,
  } = useQuery({
    queryKey: ['settlementDetails', selectedPartner?.tradingPartnerId],
    queryFn: () =>
      tradingPartnerExposureApi.getSettlementDetails(selectedPartner!.tradingPartnerId),
    enabled: selectedPartner !== null && showSettlementDetails,
  });

  const handleTabChange = (event: React.SyntheticEvent, newValue: number) => {
    setSelectedTab(newValue);
  };

  const handleSortChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setSortBy(e.target.value);
  };

  const handleViewDetails = async (partner: TradingPartnerExposureDto) => {
    setSelectedPartner(partner);
    setShowSettlementDetails(true);
  };

  const handleCloseDetails = () => {
    setShowSettlementDetails(false);
    setSelectedPartner(null);
  };

  // Calculate summary statistics
  const calculateSummary = (exposures: TradingPartnerExposureDto[]) => {
    return {
      totalPartners: exposures.length,
      totalExposure: exposures.reduce((sum, e) => sum + e.currentExposure, 0),
      totalCreditLimit: exposures.reduce((sum, e) => sum + e.creditLimit, 0),
      totalOverdue: exposures.reduce((sum, e) => sum + e.overdueApAmount, 0),
      criticalRiskCount: exposures.filter((e) => e.riskLevel === 4).length,
      highRiskCount: exposures.filter((e) => e.riskLevel === 3).length,
    };
  };

  const allSummary = calculateSummary(allExposures);
  const atRiskSummary = calculateSummary(atRiskPartners);

  if (errorAll && selectedTab === 0) {
    return (
      <Container>
        <Alert severity="error">Failed to load trading partner exposures</Alert>
      </Container>
    );
  }

  if (errorAtRisk && selectedTab === 1) {
    return (
      <Container>
        <Alert severity="error">Failed to load at-risk partners</Alert>
      </Container>
    );
  }

  return (
    <Container maxWidth="lg" sx={{ py: 3 }}>
      <Typography variant="h4" fontWeight="bold" sx={{ mb: 3 }}>
        Trading Partner Credit Exposure Dashboard
      </Typography>

      {/* Main Summary Cards */}
      <Grid container spacing={2} sx={{ mb: 4 }}>
        <Grid item xs={12} sm={6} md={3}>
          <Paper sx={{ p: 2, textAlign: 'center', backgroundColor: '#E3F2FD' }}>
            <Typography variant="caption" color="textSecondary">
              Active Partners
            </Typography>
            <Typography variant="h5" fontWeight="bold">
              {allSummary.totalPartners}
            </Typography>
          </Paper>
        </Grid>
        <Grid item xs={12} sm={6} md={3}>
          <Paper sx={{ p: 2, textAlign: 'center', backgroundColor: '#E8F5E9' }}>
            <Typography variant="caption" color="textSecondary">
              Total Credit Limit
            </Typography>
            <Typography variant="h6" fontWeight="bold">
              {formatCurrency(allSummary.totalCreditLimit)}
            </Typography>
          </Paper>
        </Grid>
        <Grid item xs={12} sm={6} md={3}>
          <Paper sx={{ p: 2, textAlign: 'center', backgroundColor: '#FFF3E0' }}>
            <Typography variant="caption" color="textSecondary">
              Current Exposure
            </Typography>
            <Typography variant="h6" fontWeight="bold">
              {formatCurrency(allSummary.totalExposure)}
            </Typography>
          </Paper>
        </Grid>
        <Grid item xs={12} sm={6} md={3}>
          <Paper sx={{ p: 2, textAlign: 'center', backgroundColor: '#FFEBEE' }}>
            <Typography variant="caption" color="textSecondary">
              Overdue Amount
            </Typography>
            <Typography variant="h6" fontWeight="bold">
              {formatCurrency(allSummary.totalOverdue)}
            </Typography>
          </Paper>
        </Grid>
      </Grid>

      {/* Risk Summary Row */}
      <Paper sx={{ p: 2, mb: 3, backgroundColor: '#FFF9C4' }}>
        <Grid container spacing={2}>
          <Grid item xs={12} md={4}>
            <Box>
              <Typography variant="caption" color="textSecondary">
                Critical Risk Partners (Level 4)
              </Typography>
              <Typography variant="h6" fontWeight="bold" sx={{ color: '#F44336' }}>
                {allSummary.criticalRiskCount}
              </Typography>
            </Box>
          </Grid>
          <Grid item xs={12} md={4}>
            <Box>
              <Typography variant="caption" color="textSecondary">
                High Risk Partners (Level 3)
              </Typography>
              <Typography variant="h6" fontWeight="bold" sx={{ color: '#FF9800' }}>
                {allSummary.highRiskCount}
              </Typography>
            </Box>
          </Grid>
          <Grid item xs={12} md={4}>
            <Box>
              <Typography variant="caption" color="textSecondary">
                Partners at Risk ({atRiskSummary.totalPartners})
              </Typography>
              <Typography variant="h6" fontWeight="bold">
                {atRiskSummary.totalPartners === 0 ? 'No critical issues' : 'Review needed'}
              </Typography>
            </Box>
          </Grid>
        </Grid>
      </Paper>

      {/* Tabs */}
      <Paper sx={{ mb: 3 }}>
        <Tabs
          value={selectedTab}
          onChange={handleTabChange}
          aria-label="exposure dashboard tabs"
        >
          <Tab label={`All Partners (${allSummary.totalPartners})`} id="exposure-tab-0" />
          <Tab label={`At-Risk Partners (${atRiskSummary.totalPartners})`} id="exposure-tab-1" />
        </Tabs>
      </Paper>

      {/* All Partners Tab */}
      <TabPanel value={selectedTab} index={0}>
        <Stack spacing={2} sx={{ mb: 3 }}>
          <Box display="flex" gap={2}>
            <TextField
              select
              label="Sort By"
              value={sortBy}
              onChange={handleSortChange}
              size="small"
              sx={{ minWidth: 200 }}
            >
              <MenuItem value="riskLevel">Risk Level (Highest First)</MenuItem>
              <MenuItem value="utilizationpercentage">
                Credit Utilization (Highest First)
              </MenuItem>
              <MenuItem value="companyname">Company Name (A-Z)</MenuItem>
            </TextField>
            <Button
              variant="contained"
              size="small"
              onClick={() => refetchAll()}
              disabled={isLoadingAll}
            >
              Refresh
            </Button>
          </Box>
        </Stack>

        {isLoadingAll ? (
          <Box display="flex" justifyContent="center" sx={{ py: 4 }}>
            <CircularProgress />
          </Box>
        ) : allExposures.length === 0 ? (
          <Alert severity="info">No trading partners found</Alert>
        ) : (
          <Grid container spacing={2}>
            {allExposures.map((exposure) => (
              <Grid item xs={12} sm={6} md={4} key={exposure.tradingPartnerId}>
                <ExposureCard
                  exposure={exposure}
                  onViewDetails={() => handleViewDetails(exposure)}
                />
              </Grid>
            ))}
          </Grid>
        )}
      </TabPanel>

      {/* At-Risk Partners Tab */}
      <TabPanel value={selectedTab} index={1}>
        {isLoadingAtRisk ? (
          <Box display="flex" justifyContent="center" sx={{ py: 4 }}>
            <CircularProgress />
          </Box>
        ) : atRiskPartners.length === 0 ? (
          <Alert severity="success">No at-risk partners detected</Alert>
        ) : (
          <Grid container spacing={2}>
            {atRiskPartners.map((exposure) => (
              <Grid item xs={12} sm={6} md={4} key={exposure.tradingPartnerId}>
                <ExposureCard
                  exposure={exposure}
                  onViewDetails={() => handleViewDetails(exposure)}
                />
              </Grid>
            ))}
          </Grid>
        )}
      </TabPanel>

      {/* Settlement Details Dialog */}
      {selectedPartner && (
        <SettlementDetailsDialog
          open={showSettlementDetails}
          partner={selectedPartner}
          settlementDetails={settlementDetails}
          isLoading={isLoadingDetails}
          onClose={handleCloseDetails}
        />
      )}
    </Container>
  );
};

export default TradingPartnerExposureDashboard;

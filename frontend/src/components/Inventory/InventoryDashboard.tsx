import React, { useState, useEffect } from 'react';
import {
  Container,
  Grid,
  Card,
  CardContent,
  Typography,
  Box,
  Tabs,
  Tab,
  Button,
  Chip,
  CircularProgress,
  Alert
} from '@mui/material';
import {
  Warehouse as WarehouseIcon,
  Inventory as InventoryIcon,
  LocalShipping as ShippingIcon,
  TrendingUp as TrendingUpIcon,
} from '@mui/icons-material';
import { inventoryApi } from '@/services/inventoryApi';
import {
  InventoryLocation,
  InventoryPosition,
  InventoryMovement,
  InventorySummary
} from '@/types/inventory';
import InventoryLocationsTable from './InventoryLocationsTable';
import InventoryPositionsTable from './InventoryPositionsTable';
import InventoryMovementsTable from './InventoryMovementsTable';

interface TabPanelProps {
  children?: React.ReactNode;
  index: number;
  value: number;
}

function TabPanel(props: TabPanelProps) {
  const { children, value, index, ...other } = props;

  return (
    <div
      role="tabpanel"
      hidden={value !== index}
      id={`inventory-tabpanel-${index}`}
      aria-labelledby={`inventory-tab-${index}`}
      {...other}
    >
      {value === index && <Box sx={{ p: 3 }}>{children}</Box>}
    </div>
  );
}

function a11yProps(index: number) {
  return {
    id: `inventory-tab-${index}`,
    'aria-controls': `inventory-tabpanel-${index}`,
  };
}

export default function InventoryDashboard() {
  const [currentTab, setCurrentTab] = useState(0);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  
  // Data states
  const [summary, setSummary] = useState<InventorySummary | null>(null);
  const [locations, setLocations] = useState<InventoryLocation[]>([]);
  const [positions, setPositions] = useState<InventoryPosition[]>([]);
  const [movements, setMovements] = useState<InventoryMovement[]>([]);

  useEffect(() => {
    loadData();
  }, []);

  const loadData = async () => {
    try {
      setLoading(true);
      setError(null);

      // Load all data in parallel
      const [summaryData, locationsData, positionsData, movementsData] = await Promise.all([
        inventoryApi.summary.getOverview(),
        inventoryApi.locations.getAll(),
        inventoryApi.positions.getAll(),
        inventoryApi.movements.getAll()
      ]);

      setSummary(summaryData);
      setLocations(locationsData);
      setPositions(positionsData);
      setMovements(movementsData);
    } catch (err) {
      console.error('Error loading inventory data:', err);
      setError('Failed to load inventory data. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  const handleTabChange = (_event: React.SyntheticEvent, newValue: number) => {
    setCurrentTab(newValue);
  };

  if (loading) {
    return (
      <Container maxWidth="xl">
        <Box display="flex" justifyContent="center" alignItems="center" minHeight="400px">
          <CircularProgress />
        </Box>
      </Container>
    );
  }

  if (error) {
    return (
      <Container maxWidth="xl">
        <Alert severity="error" sx={{ mt: 2 }}>
          {error}
          <Button onClick={loadData} sx={{ ml: 2 }}>
            Retry
          </Button>
        </Alert>
      </Container>
    );
  }

  return (
    <Container maxWidth="xl">
      <Box sx={{ mb: 3 }}>
        <Typography variant="h4" component="h1" gutterBottom>
          Inventory Management
        </Typography>
        <Typography variant="subtitle1" color="text.secondary">
          Manage locations, inventory positions, and movements
        </Typography>
      </Box>

      {/* Summary Cards */}
      {summary && (
        <Grid container spacing={3} sx={{ mb: 3 }}>
          <Grid item xs={12} md={3}>
            <Card>
              <CardContent>
                <Box display="flex" alignItems="center" justifyContent="space-between">
                  <Box>
                    <Typography color="textSecondary" gutterBottom variant="body2">
                      Total Locations
                    </Typography>
                    <Typography variant="h4">
                      {summary.totalLocations}
                    </Typography>
                    <Typography variant="body2" color="text.secondary">
                      {summary.activeLocations} active
                    </Typography>
                  </Box>
                  <WarehouseIcon color="primary" sx={{ fontSize: 40 }} />
                </Box>
              </CardContent>
            </Card>
          </Grid>

          <Grid item xs={12} md={3}>
            <Card>
              <CardContent>
                <Box display="flex" alignItems="center" justifyContent="space-between">
                  <Box>
                    <Typography color="textSecondary" gutterBottom variant="body2">
                      Total Inventory Value
                    </Typography>
                    <Typography variant="h4">
                      ${summary.totalInventoryValue.toLocaleString()}
                    </Typography>
                    <Typography variant="body2" color="text.secondary">
                      {summary.currency}
                    </Typography>
                  </Box>
                  <TrendingUpIcon color="success" sx={{ fontSize: 40 }} />
                </Box>
              </CardContent>
            </Card>
          </Grid>

          <Grid item xs={12} md={3}>
            <Card>
              <CardContent>
                <Box display="flex" alignItems="center" justifyContent="space-between">
                  <Box>
                    <Typography color="textSecondary" gutterBottom variant="body2">
                      Total Quantity
                    </Typography>
                    <Typography variant="h4">
                      {summary.totalInventoryQuantity.toLocaleString()}
                    </Typography>
                    <Typography variant="body2" color="text.secondary">
                      MT
                    </Typography>
                  </Box>
                  <InventoryIcon color="info" sx={{ fontSize: 40 }} />
                </Box>
              </CardContent>
            </Card>
          </Grid>

          <Grid item xs={12} md={3}>
            <Card>
              <CardContent>
                <Box display="flex" alignItems="center" justifyContent="space-between">
                  <Box>
                    <Typography color="textSecondary" gutterBottom variant="body2">
                      Pending Movements
                    </Typography>
                    <Typography variant="h4">
                      {summary.pendingMovements}
                    </Typography>
                    <Chip 
                      label="Attention" 
                      color={summary.pendingMovements > 0 ? "warning" : "success"}
                      size="small"
                    />
                  </Box>
                  <ShippingIcon color="warning" sx={{ fontSize: 40 }} />
                </Box>
              </CardContent>
            </Card>
          </Grid>
        </Grid>
      )}

      {/* Tabs */}
      <Card>
        <Box sx={{ borderBottom: 1, borderColor: 'divider' }}>
          <Tabs value={currentTab} onChange={handleTabChange} aria-label="inventory tabs">
            <Tab label="Locations" {...a11yProps(0)} />
            <Tab label="Inventory Positions" {...a11yProps(1)} />
            <Tab label="Movements" {...a11yProps(2)} />
          </Tabs>
        </Box>

        <TabPanel value={currentTab} index={0}>
          <InventoryLocationsTable 
            locations={locations} 
            onRefresh={loadData}
          />
        </TabPanel>

        <TabPanel value={currentTab} index={1}>
          <InventoryPositionsTable 
            positions={positions}
            locations={locations}
            onRefresh={loadData}
          />
        </TabPanel>

        <TabPanel value={currentTab} index={2}>
          <InventoryMovementsTable 
            movements={movements}
            locations={locations}
            onRefresh={loadData}
          />
        </TabPanel>
      </Card>
    </Container>
  );
}
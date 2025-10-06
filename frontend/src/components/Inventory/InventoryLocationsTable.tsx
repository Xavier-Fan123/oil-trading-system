import React, { useState } from 'react';
import {
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Paper,
  Typography,
  Chip,
  Box,
  IconButton,
  Tooltip,
  Button,
  TextField,
  InputAdornment,
  Alert
} from '@mui/material';
import {
  Search as SearchIcon,
  Refresh as RefreshIcon,
  Add as AddIcon,
  Edit as EditIcon,
  Delete as DeleteIcon
} from '@mui/icons-material';
import { inventoryApi } from '@/services/inventoryApi';
import { InventoryLocation } from '@/types/inventory';
import LocationModal from './LocationModal';

interface InventoryLocationsTableProps {
  locations: InventoryLocation[];
  onRefresh: () => void;
}

const InventoryLocationsTable: React.FC<InventoryLocationsTableProps> = ({
  locations,
  onRefresh
}) => {
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [searchTerm, setSearchTerm] = useState('');
  const [modalOpen, setModalOpen] = useState(false);
  const [selectedLocation, setSelectedLocation] = useState<InventoryLocation | null>(null);

  const handleRefresh = async () => {
    setLoading(true);
    try {
      await onRefresh();
      setError(null);
    } catch (err) {
      setError('Failed to refresh locations');
      console.error('Error refreshing locations:', err);
    } finally {
      setLoading(false);
    }
  };

  const handleDelete = async (id: string, locationCode: string) => {
    if (window.confirm(`Are you sure you want to delete location ${locationCode}?`)) {
      try {
        await inventoryApi.locations.delete(id);
        onRefresh(); // Refresh the list
      } catch (err: any) {
        alert(err.response?.data || 'Failed to delete location');
        console.error('Error deleting location:', err);
      }
    }
  };

  const handleAdd = () => {
    setSelectedLocation(null);
    setModalOpen(true);
  };

  const handleEdit = (location: InventoryLocation) => {
    setSelectedLocation(location);
    setModalOpen(true);
  };

  const handleModalClose = () => {
    setModalOpen(false);
    setSelectedLocation(null);
  };

  const handleModalSuccess = () => {
    onRefresh();
    setModalOpen(false);
    setSelectedLocation(null);
  };

  const filteredLocations = locations.filter(location =>
    location.locationName.toLowerCase().includes(searchTerm.toLowerCase()) ||
    location.locationCode.toLowerCase().includes(searchTerm.toLowerCase())
  );

  const getStatusColor = (isActive: boolean): 'success' | 'error' => {
    return isActive ? 'success' : 'error';
  };

  const getUtilizationColor = (utilization: number): string => {
    if (utilization >= 90) return '#f44336'; // Red
    if (utilization >= 70) return '#ff9800'; // Orange
    if (utilization >= 50) return '#2196f3'; // Blue
    return '#4caf50'; // Green
  };

  const calculateUtilization = (location: InventoryLocation): number => {
    if (location.totalCapacity === 0) return 0;
    return Math.round(((location.totalCapacity - location.availableCapacity) / location.totalCapacity) * 100);
  };

  if (error) {
    return (
      <Alert severity="error" sx={{ mb: 2 }}>
        {error}
      </Alert>
    );
  }

  return (
    <Box>
      <Box display="flex" justifyContent="space-between" alignItems="center" mb={2}>
        <Typography variant="h6">Storage Locations</Typography>
        <Box display="flex" gap={1}>
          <TextField
            size="small"
            placeholder="Search locations..."
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            InputProps={{
              startAdornment: (
                <InputAdornment position="start">
                  <SearchIcon />
                </InputAdornment>
              ),
            }}
            sx={{ minWidth: 200 }}
          />
          <Button
            variant="outlined"
            startIcon={<RefreshIcon />}
            onClick={handleRefresh}
            disabled={loading}
          >
            Refresh
          </Button>
          <Button
            variant="contained"
            startIcon={<AddIcon />}
            onClick={handleAdd}
          >
            Add Location
          </Button>
        </Box>
      </Box>

      <TableContainer component={Paper}>
        <Table>
          <TableHead>
            <TableRow>
              <TableCell>Location Code</TableCell>
              <TableCell>Location Name</TableCell>
              <TableCell>Type</TableCell>
              <TableCell align="right">Capacity</TableCell>
              <TableCell align="right">Current Volume</TableCell>
              <TableCell align="right">Available</TableCell>
              <TableCell align="center">Utilization</TableCell>
              <TableCell align="center">Status</TableCell>
              <TableCell align="center">Actions</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {filteredLocations.map((location) => {
              const utilization = calculateUtilization(location);
              return (
                <TableRow key={location.id} hover>
                  <TableCell>
                    <Typography variant="body2" fontWeight="medium">
                      {location.locationCode}
                    </Typography>
                  </TableCell>
                  <TableCell>{location.locationName}</TableCell>
                  <TableCell>
                    <Chip 
                      label={location.locationType} 
                      size="small" 
                      variant="outlined"
                    />
                  </TableCell>
                  <TableCell align="right">
                    {location.totalCapacity.toLocaleString()} {location.capacityUnit}
                  </TableCell>
                  <TableCell align="right">
                    {(location.totalCapacity - location.availableCapacity).toLocaleString()} {location.capacityUnit}
                  </TableCell>
                  <TableCell align="right">
                    {location.availableCapacity.toLocaleString()} {location.capacityUnit}
                  </TableCell>
                  <TableCell align="center">
                    <Box display="flex" alignItems="center" justifyContent="center">
                      <Box
                        sx={{
                          width: 60,
                          height: 8,
                          backgroundColor: '#e0e0e0',
                          borderRadius: 4,
                          overflow: 'hidden',
                          mr: 1
                        }}
                      >
                        <Box
                          sx={{
                            width: `${utilization}%`,
                            height: '100%',
                            backgroundColor: getUtilizationColor(utilization),
                            transition: 'width 0.3s ease'
                          }}
                        />
                      </Box>
                      <Typography variant="body2" fontWeight="medium">
                        {utilization}%
                      </Typography>
                    </Box>
                  </TableCell>
                  <TableCell align="center">
                    <Chip
                      label={location.isActive ? 'Active' : 'Inactive'}
                      color={getStatusColor(location.isActive)}
                      size="small"
                    />
                  </TableCell>
                  <TableCell align="center">
                    <Box display="flex" justifyContent="center" gap={1}>
                      <Tooltip title="Edit Location">
                        <IconButton
                          size="small"
                          onClick={() => handleEdit(location)}
                        >
                          <EditIcon />
                        </IconButton>
                      </Tooltip>
                      <Tooltip title="Delete Location">
                        <IconButton
                          size="small"
                          color="error"
                          onClick={() => handleDelete(location.id, location.locationCode)}
                        >
                          <DeleteIcon />
                        </IconButton>
                      </Tooltip>
                    </Box>
                  </TableCell>
                </TableRow>
              );
            })}
          </TableBody>
        </Table>
      </TableContainer>

      {filteredLocations.length === 0 && (
        <Box textAlign="center" py={4}>
          <Typography variant="body1" color="text.secondary">
            No locations found matching your search criteria.
          </Typography>
        </Box>
      )}

      <LocationModal
        open={modalOpen}
        onClose={handleModalClose}
        onSuccess={handleModalSuccess}
        location={selectedLocation}
      />
    </Box>
  );
};

export default InventoryLocationsTable;
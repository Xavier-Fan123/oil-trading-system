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
import { InventoryPosition, InventoryLocation } from '@/types/inventory';
import PositionModal from './PositionModal';

interface InventoryPositionsTableProps {
  positions: InventoryPosition[];
  locations: InventoryLocation[];
  onRefresh: () => void;
}

const InventoryPositionsTable: React.FC<InventoryPositionsTableProps> = ({
  positions,
  locations,
  onRefresh
}) => {
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [searchTerm, setSearchTerm] = useState('');
  const [modalOpen, setModalOpen] = useState(false);
  const [selectedPosition, setSelectedPosition] = useState<InventoryPosition | null>(null);

  const handleRefresh = async () => {
    setLoading(true);
    try {
      await onRefresh();
      setError(null);
    } catch (err) {
      setError('Failed to refresh positions');
      console.error('Error refreshing positions:', err);
    } finally {
      setLoading(false);
    }
  };

  const handleDelete = async (id: string, productName: string) => {
    if (window.confirm(`Are you sure you want to delete position for ${productName}?`)) {
      try {
        await inventoryApi.positions.delete(id);
        onRefresh(); // Refresh the list
      } catch (err: any) {
        alert(err.response?.data || 'Failed to delete position');
        console.error('Error deleting position:', err);
      }
    }
  };

  const handleAdd = () => {
    setSelectedPosition(null);
    setModalOpen(true);
  };

  const handleEdit = (position: InventoryPosition) => {
    setSelectedPosition(position);
    setModalOpen(true);
  };

  const handleModalClose = () => {
    setModalOpen(false);
    setSelectedPosition(null);
  };

  const handleModalSuccess = () => {
    onRefresh();
    setModalOpen(false);
    setSelectedPosition(null);
  };

  const filteredPositions = positions.filter(position =>
    position.productName.toLowerCase().includes(searchTerm.toLowerCase()) ||
    position.locationName.toLowerCase().includes(searchTerm.toLowerCase())
  );

  const getStatusColor = (status: string): 'success' | 'warning' | 'error' => {
    switch (status) {
      case 'Available': return 'success';
      case 'Reserved': return 'warning';
      case 'Quality': return 'warning';
      default: return 'error';
    }
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
        <Typography variant="h6">Inventory Positions</Typography>
        <Box display="flex" gap={1}>
          <TextField
            size="small"
            placeholder="Search positions..."
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
            Add Position
          </Button>
        </Box>
      </Box>

      <TableContainer component={Paper}>
        <Table>
          <TableHead>
            <TableRow>
              <TableCell>Location</TableCell>
              <TableCell>Product</TableCell>
              <TableCell align="right">Quantity</TableCell>
              <TableCell align="right">Avg Cost</TableCell>
              <TableCell align="right">Total Value</TableCell>
              <TableCell>Grade</TableCell>
              <TableCell align="center">Status</TableCell>
              <TableCell align="center">Last Updated</TableCell>
              <TableCell align="center">Actions</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {filteredPositions.map((position) => (
              <TableRow key={position.id} hover>
                <TableCell>
                  <Box>
                    <Typography variant="body2" fontWeight="medium">
                      {position.locationCode}
                    </Typography>
                    <Typography variant="caption" color="text.secondary">
                      {position.locationName}
                    </Typography>
                  </Box>
                </TableCell>
                <TableCell>
                  <Box>
                    <Typography variant="body2" fontWeight="medium">
                      {position.productCode}
                    </Typography>
                    <Typography variant="caption" color="text.secondary">
                      {position.productName}
                    </Typography>
                  </Box>
                </TableCell>
                <TableCell align="right">
                  <Typography variant="body2" fontWeight="medium">
                    {position.quantity.toLocaleString()} {position.quantityUnit}
                  </Typography>
                </TableCell>
                <TableCell align="right">
                  <Typography variant="body2">
                    {position.currency} {position.averageCost.toLocaleString()}
                  </Typography>
                </TableCell>
                <TableCell align="right">
                  <Typography variant="body2" fontWeight="medium">
                    {position.currency} {position.totalValue.toLocaleString()}
                  </Typography>
                </TableCell>
                <TableCell>
                  <Typography variant="body2">
                    {position.grade || 'N/A'}
                  </Typography>
                </TableCell>
                <TableCell align="center">
                  <Chip
                    label={position.status}
                    color={getStatusColor(position.status)}
                    size="small"
                  />
                </TableCell>
                <TableCell align="center">
                  <Typography variant="caption">
                    {new Date(position.lastUpdated).toLocaleDateString()}
                  </Typography>
                </TableCell>
                <TableCell align="center">
                  <Box display="flex" justifyContent="center" gap={1}>
                    <Tooltip title="Edit Position">
                      <IconButton
                        size="small"
                        onClick={() => handleEdit(position)}
                      >
                        <EditIcon />
                      </IconButton>
                    </Tooltip>
                    <Tooltip title="Delete Position">
                      <IconButton
                        size="small"
                        color="error"
                        onClick={() => handleDelete(position.id, position.productName)}
                      >
                        <DeleteIcon />
                      </IconButton>
                    </Tooltip>
                  </Box>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </TableContainer>

      {filteredPositions.length === 0 && (
        <Box textAlign="center" py={4}>
          <Typography variant="body1" color="text.secondary">
            No inventory positions found matching your search criteria.
          </Typography>
        </Box>
      )}

      <PositionModal
        open={modalOpen}
        onClose={handleModalClose}
        onSuccess={handleModalSuccess}
        position={selectedPosition}
        locations={locations}
      />
    </Box>
  );
};

export default InventoryPositionsTable;
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
  Delete as DeleteIcon,
  ArrowUpward as ArrowUpwardIcon,
  ArrowDownward as ArrowDownwardIcon,
  SwapHoriz as SwapHorizIcon
} from '@mui/icons-material';
import { inventoryApi } from '@/services/inventoryApi';
import { InventoryMovement, InventoryLocation } from '@/types/inventory';
import MovementModal from './MovementModal';

interface InventoryMovementsTableProps {
  movements: InventoryMovement[];
  locations: InventoryLocation[];
  onRefresh: () => void;
}

const InventoryMovementsTable: React.FC<InventoryMovementsTableProps> = ({
  movements,
  locations,
  onRefresh
}) => {
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [searchTerm, setSearchTerm] = useState('');
  const [modalOpen, setModalOpen] = useState(false);
  const [selectedMovement, setSelectedMovement] = useState<InventoryMovement | null>(null);

  const handleRefresh = async () => {
    setLoading(true);
    try {
      await onRefresh();
      setError(null);
    } catch (err) {
      setError('Failed to refresh movements');
      console.error('Error refreshing movements:', err);
    } finally {
      setLoading(false);
    }
  };

  const handleDelete = async (id: string, movementReference: string) => {
    if (window.confirm(`Are you sure you want to delete movement ${movementReference}?`)) {
      try {
        await inventoryApi.movements.delete(id);
        onRefresh(); // Refresh the list
      } catch (err: any) {
        alert(err.response?.data || 'Failed to delete movement');
        console.error('Error deleting movement:', err);
      }
    }
  };

  const handleAdd = () => {
    setSelectedMovement(null);
    setModalOpen(true);
  };

  const handleEdit = (movement: InventoryMovement) => {
    setSelectedMovement(movement);
    setModalOpen(true);
  };

  const handleModalClose = () => {
    setModalOpen(false);
    setSelectedMovement(null);
  };

  const handleModalSuccess = () => {
    onRefresh();
    setModalOpen(false);
    setSelectedMovement(null);
  };

  const filteredMovements = movements.filter(movement =>
    movement.movementReference.toLowerCase().includes(searchTerm.toLowerCase()) ||
    movement.fromLocationCode?.toLowerCase().includes(searchTerm.toLowerCase()) ||
    movement.toLocationCode?.toLowerCase().includes(searchTerm.toLowerCase())
  );

  const getStatusColor = (status: string): 'success' | 'warning' | 'error' => {
    switch (status) {
      case 'Completed': return 'success';
      case 'InProgress': return 'warning';
      case 'Planned': return 'warning';
      default: return 'error';
    }
  };

  const getMovementTypeIcon = (type: string) => {
    switch (type) {
      case 'Receipt': return <ArrowDownwardIcon color="success" />;
      case 'Shipment': return <ArrowUpwardIcon color="error" />;
      case 'Transfer': return <SwapHorizIcon color="primary" />;
      default: return <SwapHorizIcon />;
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
        <Typography variant="h6">Inventory Movements</Typography>
        <Box display="flex" gap={1}>
          <TextField
            size="small"
            placeholder="Search movements..."
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
            Create Movement
          </Button>
        </Box>
      </Box>

      <TableContainer component={Paper}>
        <Table>
          <TableHead>
            <TableRow>
              <TableCell>Movement</TableCell>
              <TableCell>Type</TableCell>
              <TableCell>From Location</TableCell>
              <TableCell>To Location</TableCell>
              <TableCell>Product</TableCell>
              <TableCell align="right">Quantity</TableCell>
              <TableCell align="center">Status</TableCell>
              <TableCell align="center">Date</TableCell>
              <TableCell align="center">Actions</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {filteredMovements.map((movement) => (
              <TableRow key={movement.id} hover>
                <TableCell>
                  <Typography variant="body2" fontWeight="medium">
                    {movement.movementReference}
                  </Typography>
                  <Typography variant="caption" color="text.secondary">
                    {movement.transportMode || 'N/A'}
                  </Typography>
                </TableCell>
                <TableCell>
                  <Box display="flex" alignItems="center" gap={1}>
                    {getMovementTypeIcon(movement.movementType.toString())}
                    <Typography variant="body2">
                      {movement.movementType}
                    </Typography>
                  </Box>
                </TableCell>
                <TableCell>
                  <Typography variant="body2">
                    {movement.fromLocationCode || 'N/A'}
                  </Typography>
                </TableCell>
                <TableCell>
                  <Typography variant="body2">
                    {movement.toLocationCode || 'N/A'}
                  </Typography>
                </TableCell>
                <TableCell>
                  <Typography variant="body2">
                    {movement.productCode || 'N/A'}
                  </Typography>
                </TableCell>
                <TableCell align="right">
                  <Typography variant="body2" fontWeight="medium">
                    {movement.quantity.toLocaleString()} {movement.quantityUnit}
                  </Typography>
                </TableCell>
                <TableCell align="center">
                  <Chip
                    label={movement.status}
                    color={getStatusColor(movement.status.toString())}
                    size="small"
                  />
                </TableCell>
                <TableCell align="center">
                  <Typography variant="caption">
                    {new Date(movement.movementDate).toLocaleDateString()}
                  </Typography>
                </TableCell>
                <TableCell align="center">
                  <Box display="flex" justifyContent="center" gap={1}>
                    <Tooltip title="Edit Movement">
                      <IconButton
                        size="small"
                        onClick={() => handleEdit(movement)}
                      >
                        <EditIcon />
                      </IconButton>
                    </Tooltip>
                    <Tooltip title="Delete Movement">
                      <IconButton
                        size="small"
                        color="error"
                        onClick={() => handleDelete(movement.id, movement.movementReference)}
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

      {filteredMovements.length === 0 && (
        <Box textAlign="center" py={4}>
          <Typography variant="body1" color="text.secondary">
            No inventory movements found matching your search criteria.
          </Typography>
        </Box>
      )}

      <MovementModal
        open={modalOpen}
        onClose={handleModalClose}
        onSuccess={handleModalSuccess}
        movement={selectedMovement}
        locations={locations}
      />
    </Box>
  );
};

export default InventoryMovementsTable;
import React, { useState, useCallback } from 'react';
import {
  Box,
  TextField,
  Button,
  CircularProgress,
  Alert,
  Card,
  CardContent,
  Typography,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Chip,
  Grid,
} from '@mui/material';
import SearchIcon from '@mui/icons-material/Search';
import ClearIcon from '@mui/icons-material/Clear';
import contractResolutionApi, { ContractCandidateDto, ContractResolutionResultDto } from '@/services/contractResolutionApi';

interface ContractResolverProps {
  onContractSelected: (contractId: string, contract?: ContractCandidateDto) => void;
  contractType?: 'Purchase' | 'Sales';
  tradingPartnerId?: string;
  productId?: string;
  allowManualInput?: boolean;
}

type ResolutionState = 'idle' | 'searching' | 'resolved' | 'ambiguous' | 'not_found' | 'error';

/**
 * ContractResolver Component
 * Resolves external contract numbers to internal GUIDs
 * Handles disambiguation when multiple contracts match the same external number
 */
export const ContractResolver: React.FC<ContractResolverProps> = ({
  onContractSelected,
  contractType,
  tradingPartnerId,
  productId,
  allowManualInput = true,
}) => {
  const [externalContractNumber, setExternalContractNumber] = useState('');
  const [state, setState] = useState<ResolutionState>('idle');
  const [result, setResult] = useState<ContractResolutionResultDto | null>(null);
  const [selectedCandidate, setSelectedCandidate] = useState<ContractCandidateDto | null>(null);
  const [error, setError] = useState<string | null>(null);

  const handleResolve = useCallback(async () => {
    if (!externalContractNumber.trim()) {
      setError('Please enter an external contract number');
      return;
    }

    setState('searching');
    setError(null);
    setResult(null);
    setSelectedCandidate(null);

    try {
      const resolution = await contractResolutionApi.resolve(
        externalContractNumber.trim(),
        contractType,
        tradingPartnerId,
        productId
      );

      setResult(resolution);

      if (resolution.success && resolution.contractId) {
        // Single match found
        setState('resolved');
        const candidate = resolution.candidates?.[0];
        setSelectedCandidate(candidate || null);
        onContractSelected(resolution.contractId, candidate);
      } else if (resolution.candidates && resolution.candidates.length > 0) {
        // Multiple matches - need disambiguation
        setState('ambiguous');
        setError(resolution.errorMessage || 'Multiple contracts found. Please select one.');
      } else {
        // No matches found
        setState('not_found');
        setError(resolution.errorMessage || 'No contract found with this external number');
      }
    } catch (err: any) {
      setState('error');
      const errorMsg = err?.response?.data?.errorMessage || err.message || 'Failed to resolve contract';
      setError(errorMsg);
    }
  }, [externalContractNumber, contractType, tradingPartnerId, productId, onContractSelected]);

  const handleSelectCandidate = (candidate: ContractCandidateDto) => {
    setSelectedCandidate(candidate);
    onContractSelected(candidate.id, candidate);
    setState('resolved');
  };

  const handleClear = () => {
    setExternalContractNumber('');
    setState('idle');
    setResult(null);
    setSelectedCandidate(null);
    setError(null);
  };

  const handleKeyPress = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter') {
      handleResolve();
    }
  };

  return (
    <Box sx={{ width: '100%' }}>
      {/* Input Section */}
      <Card sx={{ mb: 2 }}>
        <CardContent>
          <Box sx={{ display: 'flex', gap: 1, alignItems: 'flex-start' }}>
            <TextField
              label="External Contract Number"
              placeholder="Enter contract number from trading partner"
              value={externalContractNumber}
              onChange={(e) => setExternalContractNumber(e.target.value)}
              onKeyPress={handleKeyPress}
              disabled={state === 'searching'}
              fullWidth
              size="small"
              helperText="Enter the contract number as provided by your trading partner"
            />
            <Button
              variant="contained"
              color="primary"
              onClick={handleResolve}
              disabled={state === 'searching' || !externalContractNumber.trim()}
              startIcon={state === 'searching' ? <CircularProgress size={20} /> : <SearchIcon />}
              sx={{ mt: 1 }}
            >
              Resolve
            </Button>
            {externalContractNumber && (
              <Button
                variant="outlined"
                color="inherit"
                onClick={handleClear}
                startIcon={<ClearIcon />}
                disabled={state === 'searching'}
                sx={{ mt: 1 }}
              >
                Clear
              </Button>
            )}
          </Box>
        </CardContent>
      </Card>

      {/* Error/Status Messages */}
      {error && (
        <Alert severity={state === 'error' ? 'error' : 'warning'} sx={{ mb: 2 }}>
          {error}
        </Alert>
      )}

      {/* Success Message */}
      {state === 'resolved' && selectedCandidate && (
        <Alert severity="success" sx={{ mb: 2 }}>
          Contract resolved successfully! Contract: {selectedCandidate.contractNumber} ({selectedCandidate.contractType})
        </Alert>
      )}

      {/* Resolved Contract Details */}
      {state === 'resolved' && selectedCandidate && (
        <Card sx={{ mb: 2 }}>
          <CardContent>
            <Typography variant="h6" sx={{ mb: 2 }}>
              Selected Contract Details
            </Typography>
            <Grid container spacing={2}>
              <Grid item xs={12} sm={6}>
                <Typography variant="subtitle2" color="textSecondary">
                  Contract Number
                </Typography>
                <Typography variant="body1">{selectedCandidate.contractNumber}</Typography>
              </Grid>
              <Grid item xs={12} sm={6}>
                <Typography variant="subtitle2" color="textSecondary">
                  External Contract Number
                </Typography>
                <Typography variant="body1">{selectedCandidate.externalContractNumber}</Typography>
              </Grid>
              <Grid item xs={12} sm={6}>
                <Typography variant="subtitle2" color="textSecondary">
                  Type
                </Typography>
                <Chip
                  label={selectedCandidate.contractType}
                  color={selectedCandidate.contractType === 'Purchase' ? 'primary' : 'secondary'}
                  size="small"
                />
              </Grid>
              <Grid item xs={12} sm={6}>
                <Typography variant="subtitle2" color="textSecondary">
                  Trading Partner
                </Typography>
                <Typography variant="body1">{selectedCandidate.tradingPartnerName}</Typography>
              </Grid>
              <Grid item xs={12} sm={6}>
                <Typography variant="subtitle2" color="textSecondary">
                  Product
                </Typography>
                <Typography variant="body1">{selectedCandidate.productName}</Typography>
              </Grid>
              <Grid item xs={12} sm={6}>
                <Typography variant="subtitle2" color="textSecondary">
                  Quantity
                </Typography>
                <Typography variant="body1">
                  {selectedCandidate.quantity} {selectedCandidate.quantityUnit}
                </Typography>
              </Grid>
              <Grid item xs={12} sm={6}>
                <Typography variant="subtitle2" color="textSecondary">
                  Status
                </Typography>
                <Chip label={selectedCandidate.status} size="small" />
              </Grid>
              <Grid item xs={12} sm={6}>
                <Typography variant="subtitle2" color="textSecondary">
                  Created At
                </Typography>
                <Typography variant="body1">
                  {new Date(selectedCandidate.createdAt).toLocaleDateString()}
                </Typography>
              </Grid>
            </Grid>
          </CardContent>
        </Card>
      )}

      {/* Candidates Table (for disambiguation) */}
      {state === 'ambiguous' && result?.candidates && result.candidates.length > 0 && (
        <Card>
          <CardContent>
            <Typography variant="h6" sx={{ mb: 2 }}>
              Multiple Contracts Found - Please Select One
            </Typography>
            <TableContainer>
              <Table size="small">
                <TableHead>
                  <TableRow sx={{ backgroundColor: '#f5f5f5' }}>
                    <TableCell><strong>Contract Number</strong></TableCell>
                    <TableCell><strong>Type</strong></TableCell>
                    <TableCell><strong>Trading Partner</strong></TableCell>
                    <TableCell><strong>Product</strong></TableCell>
                    <TableCell><strong>Quantity</strong></TableCell>
                    <TableCell><strong>Status</strong></TableCell>
                    <TableCell align="center"><strong>Action</strong></TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {result.candidates.map((candidate) => (
                    <TableRow key={candidate.id} hover>
                      <TableCell>{candidate.contractNumber}</TableCell>
                      <TableCell>
                        <Chip
                          label={candidate.contractType}
                          size="small"
                          color={candidate.contractType === 'Purchase' ? 'primary' : 'secondary'}
                        />
                      </TableCell>
                      <TableCell>{candidate.tradingPartnerName}</TableCell>
                      <TableCell>{candidate.productName}</TableCell>
                      <TableCell>
                        {candidate.quantity} {candidate.quantityUnit}
                      </TableCell>
                      <TableCell>
                        <Chip label={candidate.status} size="small" />
                      </TableCell>
                      <TableCell align="center">
                        <Button
                          size="small"
                          variant="outlined"
                          color="primary"
                          onClick={() => handleSelectCandidate(candidate)}
                        >
                          Select
                        </Button>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </TableContainer>
          </CardContent>
        </Card>
      )}

      {/* Manual Entry Option */}
      {allowManualInput && state === 'not_found' && (
        <Card sx={{ mt: 2 }}>
          <CardContent>
            <Alert severity="info" sx={{ mb: 2 }}>
              Cannot find contract with external number "{externalContractNumber}".
              You may manually enter the internal contract GUID if you have it.
            </Alert>
          </CardContent>
        </Card>
      )}
    </Box>
  );
};

export default ContractResolver;

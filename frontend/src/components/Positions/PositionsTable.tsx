import React, { useState, useEffect } from 'react';
import {
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TablePagination,
  Paper,
  Chip,
  Typography,
  Box,
  IconButton,
  Collapse,
  LinearProgress,
  Tooltip,
} from '@mui/material';
import {
  KeyboardArrowDown,
  KeyboardArrowRight,
  TrendingUp,
  TrendingDown,
  Remove,
} from '@mui/icons-material';
import { NetPosition, PositionType, ProductType } from '@/types/positions';
import { contractMatchingApi } from '@/services/contractMatchingApi';
import type { EnhancedNetPosition } from '@/services/contractMatchingApi';

interface PositionsTableProps {
  positions: NetPosition[];
  isLoading?: boolean;
}

const formatCurrency = (value: number): string => {
  return new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency: 'USD',
    minimumFractionDigits: 0,
    maximumFractionDigits: 0,
  }).format(value);
};

const formatNumber = (value: number): string => {
  return new Intl.NumberFormat('en-US', {
    minimumFractionDigits: 0,
    maximumFractionDigits: 0,
  }).format(value);
};

const getPositionTypeColor = (type: PositionType) => {
  switch (type) {
    case PositionType.Long:
      return 'success';
    case PositionType.Short:
      return 'error';
    case PositionType.Flat:
      return 'default';
    default:
      return 'default';
  }
};

const getPositionTypeIcon = (type: PositionType) => {
  switch (type) {
    case PositionType.Long:
      return <TrendingUp />;
    case PositionType.Short:
      return <TrendingDown />;
    case PositionType.Flat:
      return <Remove />;
    default:
      return <Remove />;
  }
};

const getHedgeColor = (ratio: number): string => {
  if (ratio >= 0.8) return '#4caf50'; // green
  if (ratio >= 0.5) return '#ff9800'; // orange
  return '#f44336'; // red
};

const PositionRow: React.FC<{ position: NetPosition; index: number; hedgeData?: EnhancedNetPosition }> = ({
  position,
  index,
  hedgeData,
}) => {
  const [expanded, setExpanded] = useState(false);

  return (
    <React.Fragment key={`position-row-${position.id}-${index}`}>
      <TableRow hover>
        <TableCell>
          <IconButton 
            size="small" 
            onClick={() => setExpanded(!expanded)}
            aria-label="expand row"
          >
            {expanded ? <KeyboardArrowDown /> : <KeyboardArrowRight />}
          </IconButton>
        </TableCell>
        <TableCell>
          <Typography variant="body2" fontWeight="medium">
            {ProductType[position.productType]}
          </Typography>
        </TableCell>
        <TableCell>
          <Typography variant="body2">
            {position.deliveryMonth}
          </Typography>
        </TableCell>
        <TableCell align="right">
          <Box display="flex" alignItems="center" justifyContent="flex-end">
            <Chip
              icon={getPositionTypeIcon(position.positionType)}
              label={`${formatNumber(position.netQuantity)} ${position.unit}`}
              color={getPositionTypeColor(position.positionType) as any}
              size="small"
              variant="outlined"
            />
          </Box>
        </TableCell>
        <TableCell align="right">
          <Typography variant="body2">
            ${position.currentPrice.toFixed(2)}
          </Typography>
        </TableCell>
        <TableCell align="right">
          <Typography variant="body2">
            {formatCurrency(position.positionValue)}
          </Typography>
        </TableCell>
        <TableCell align="right">
          <Typography 
            variant="body2"
            sx={{ 
              color: position.totalPnL >= 0 ? 'success.main' : 'error.main',
              fontWeight: 'medium'
            }}
          >
            {formatCurrency(position.totalPnL)}
          </Typography>
        </TableCell>
        <TableCell align="right">
          <Typography
            variant="body2"
            sx={{
              color: position.unrealizedPnL >= 0 ? 'success.main' : 'error.main'
            }}
          >
            {formatCurrency(position.unrealizedPnL)}
          </Typography>
        </TableCell>
        {/* Hedge columns */}
        <TableCell align="right">
          <Typography variant="body2">
            {hedgeData ? `${formatNumber(hedgeData.totalMatched)} ${position.unit}` : '-'}
          </Typography>
        </TableCell>
        <TableCell align="right">
          {hedgeData ? (
            <Tooltip title={`${(hedgeData.hedgeRatio * 100).toFixed(1)}% hedged`}>
              <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'flex-end', gap: 1 }}>
                <LinearProgress
                  variant="determinate"
                  value={Math.min(hedgeData.hedgeRatio * 100, 100)}
                  sx={{
                    width: 50,
                    height: 6,
                    borderRadius: 3,
                    '& .MuiLinearProgress-bar': { backgroundColor: getHedgeColor(hedgeData.hedgeRatio) }
                  }}
                />
                <Typography variant="body2" sx={{ color: getHedgeColor(hedgeData.hedgeRatio), minWidth: 40, textAlign: 'right' }}>
                  {(hedgeData.hedgeRatio * 100).toFixed(0)}%
                </Typography>
              </Box>
            </Tooltip>
          ) : (
            <Typography variant="body2">-</Typography>
          )}
        </TableCell>
        <TableCell align="right">
          <Typography variant="body2" sx={{ color: hedgeData && hedgeData.netExposure !== 0 ? 'warning.main' : 'text.secondary' }}>
            {hedgeData ? `${formatNumber(hedgeData.netExposure)} ${position.unit}` : '-'}
          </Typography>
        </TableCell>
      </TableRow>
      <TableRow>
        <TableCell style={{ paddingBottom: 0, paddingTop: 0 }} colSpan={11}>
          <Collapse in={expanded} timeout="auto" unmountOnExit>
            <Box sx={{ margin: 1 }}>
              <Typography variant="h6" gutterBottom component="div">
                Position Details
              </Typography>
              <Table size="small" aria-label="position details">
                <TableBody>
                  <TableRow>
                    <TableCell component="th" scope="row">
                      Long Quantity
                    </TableCell>
                    <TableCell>{formatNumber(position.longQuantity)} {position.unit}</TableCell>
                    <TableCell component="th" scope="row">
                      Short Quantity
                    </TableCell>
                    <TableCell>{formatNumber(position.shortQuantity)} {position.unit}</TableCell>
                  </TableRow>
                  <TableRow>
                    <TableCell component="th" scope="row">
                      Average Price
                    </TableCell>
                    <TableCell>${position.averagePrice.toFixed(2)}</TableCell>
                    <TableCell component="th" scope="row">
                      Realized P&L
                    </TableCell>
                    <TableCell sx={{ color: position.realizedPnL >= 0 ? 'success.main' : 'error.main' }}>
                      {formatCurrency(position.realizedPnL)}
                    </TableCell>
                  </TableRow>
                  {position.riskMetrics && (
                    <TableRow>
                      <TableCell component="th" scope="row">
                        VaR (95%)
                      </TableCell>
                      <TableCell>{formatCurrency(position.riskMetrics.var95)}</TableCell>
                      <TableCell component="th" scope="row">
                        Volatility
                      </TableCell>
                      <TableCell>{(position.riskMetrics.volatility * 100).toFixed(2)}%</TableCell>
                    </TableRow>
                  )}
                  <TableRow>
                    <TableCell component="th" scope="row">
                      Last Updated
                    </TableCell>
                    <TableCell colSpan={3}>
                      {new Date(position.lastUpdated).toLocaleString()}
                    </TableCell>
                  </TableRow>
                </TableBody>
              </Table>
            </Box>
          </Collapse>
        </TableCell>
      </TableRow>
    </React.Fragment>
  );
};

export const PositionsTable: React.FC<PositionsTableProps> = ({
  positions,
  isLoading = false
}) => {
  const [page, setPage] = useState(0);
  const [rowsPerPage, setRowsPerPage] = useState(25);
  const [hedgeMap, setHedgeMap] = useState<Record<number, EnhancedNetPosition>>({});

  useEffect(() => {
    contractMatchingApi.getEnhancedNetPosition().then(result => {
      if (result.success && result.data) {
        const map: Record<number, EnhancedNetPosition> = {};
        result.data.forEach(item => {
          map[item.productType] = item;
        });
        setHedgeMap(map);
      }
    }).catch(() => {});
  }, []);

  const handleChangePage = (_: unknown, newPage: number) => {
    setPage(newPage);
  };

  const handleChangeRowsPerPage = (event: React.ChangeEvent<HTMLInputElement>) => {
    setRowsPerPage(parseInt(event.target.value, 10));
    setPage(0);
  };

  const paginatedPositions = positions.slice(
    page * rowsPerPage,
    page * rowsPerPage + rowsPerPage
  );

  return (
    <Paper sx={{ width: '100%' }}>
      <TableContainer>
        <Table stickyHeader aria-label="positions table">
          <TableHead>
            <TableRow>
              <TableCell />
              <TableCell>Product</TableCell>
              <TableCell>Delivery Month</TableCell>
              <TableCell align="right">Net Position</TableCell>
              <TableCell align="right">Current Price</TableCell>
              <TableCell align="right">Market Value</TableCell>
              <TableCell align="right">Total P&L</TableCell>
              <TableCell align="right">Unrealized P&L</TableCell>
              <TableCell align="right">Matched Qty</TableCell>
              <TableCell align="right">Hedge Ratio</TableCell>
              <TableCell align="right">Unhedged</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {paginatedPositions.map((position, index) => (
              <PositionRow
                key={position.id}
                position={position}
                index={index}
                hedgeData={hedgeMap[position.productType]}
              />
            ))}
            {paginatedPositions.length === 0 && !isLoading && (
              <TableRow>
                <TableCell colSpan={11} align="center">
                  <Typography variant="body2" color="textSecondary" py={4}>
                    No positions found
                  </Typography>
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </TableContainer>
      <TablePagination
        rowsPerPageOptions={[10, 25, 50, 100]}
        component="div"
        count={positions.length}
        rowsPerPage={rowsPerPage}
        page={page}
        onPageChange={handleChangePage}
        onRowsPerPageChange={handleChangeRowsPerPage}
      />
    </Paper>
  );
};
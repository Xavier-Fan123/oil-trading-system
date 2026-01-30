import React from 'react';
import { Chip, Tooltip } from '@mui/material';
import WarningIcon from '@mui/icons-material/Warning';
import ErrorIcon from '@mui/icons-material/Error';
import InfoIcon from '@mui/icons-material/Info';
import CheckCircleIcon from '@mui/icons-material/CheckCircle';

interface RiskLevelBadgeProps {
  riskLevel: number;
  riskLevelDescription: string;
}

export const RiskLevelBadge: React.FC<RiskLevelBadgeProps> = ({
  riskLevel,
  riskLevelDescription,
}) => {
  const getRiskColor = (level: number) => {
    switch (level) {
      case 1:
        return '#4CAF50'; // Green for Low
      case 2:
        return '#FFC107'; // Amber for Medium
      case 3:
        return '#FF9800'; // Orange for High
      case 4:
        return '#F44336'; // Red for Critical
      default:
        return '#9E9E9E'; // Gray for Unknown
    }
  };

  const getRiskLabel = (level: number) => {
    switch (level) {
      case 1:
        return 'Low';
      case 2:
        return 'Medium';
      case 3:
        return 'High';
      case 4:
        return 'Critical';
      default:
        return 'Unknown';
    }
  };

  const getRiskIcon = (level: number) => {
    switch (level) {
      case 1:
        return <CheckCircleIcon sx={{ fontSize: 16 }} />;
      case 2:
        return <InfoIcon sx={{ fontSize: 16 }} />;
      case 3:
        return <WarningIcon sx={{ fontSize: 16 }} />;
      case 4:
        return <ErrorIcon sx={{ fontSize: 16 }} />;
      default:
        return undefined;
    }
  };

  const riskIcon = getRiskIcon(riskLevel);

  return (
    <Tooltip title={riskLevelDescription}>
      <Chip
        label={getRiskLabel(riskLevel)}
        icon={riskIcon}
        size="small"
        sx={{
          backgroundColor: getRiskColor(riskLevel),
          color: 'white',
          fontWeight: 'bold',
        }}
      />
    </Tooltip>
  );
};

export default RiskLevelBadge;

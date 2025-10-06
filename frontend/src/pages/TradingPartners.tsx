import React from 'react';
import { Container } from '@mui/material';
import { TradingPartnersList } from '../components/TradingPartners/TradingPartnersList';

const TradingPartners: React.FC = () => {
  return (
    <Container maxWidth="xl" sx={{ py: 3 }}>
      <TradingPartnersList />
    </Container>
  );
};

export default TradingPartners;
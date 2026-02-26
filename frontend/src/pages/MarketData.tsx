import React, { useState } from 'react';
import { Box } from '@mui/material';
import { MarketDataUpload } from '@/components/MarketData/MarketDataUpload';
import { MarketDataTable } from '@/components/MarketData/MarketDataTable';
import { MarketDataHistory } from '@/components/MarketData/MarketDataHistory';
import { ExposureUpload } from '@/components/MarketData/ExposureUpload';

export const MarketData: React.FC = () => {
  const [activeTab, setActiveTab] = useState<'upload' | 'latest' | 'history' | 'exposure'>('upload');

  return (
    <Box sx={{ p: 3 }}>
      {activeTab === 'upload' && (
        <MarketDataUpload
          onTabChange={setActiveTab}
        />
      )}

      {activeTab === 'latest' && (
        <MarketDataTable
          onTabChange={setActiveTab}
        />
      )}

      {activeTab === 'history' && (
        <MarketDataHistory
          onTabChange={setActiveTab}
        />
      )}

      {activeTab === 'exposure' && (
        <ExposureUpload
          onTabChange={setActiveTab}
        />
      )}
    </Box>
  );
};

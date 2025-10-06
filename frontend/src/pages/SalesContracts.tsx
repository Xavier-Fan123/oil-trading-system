import React, { useState } from 'react';
import { Box } from '@mui/material';
import { SalesContractsList } from '@/components/SalesContracts/SalesContractsList';
import { SalesContractForm } from '@/components/SalesContracts/SalesContractForm';

export const SalesContracts: React.FC = () => {
  const [view, setView] = useState<'list' | 'create' | 'edit' | 'view'>('list');
  const [selectedContractId, setSelectedContractId] = useState<string | undefined>();

  const handleCreate = () => {
    setSelectedContractId(undefined);
    setView('create');
  };

  const handleEdit = (contractId: string) => {
    setSelectedContractId(contractId);
    setView('edit');
  };

  const handleView = (contractId: string) => {
    setSelectedContractId(contractId);
    setView('view');
  };

  const handleSuccess = () => {
    setView('list');
    setSelectedContractId(undefined);
  };

  const handleCancel = () => {
    setView('list');
    setSelectedContractId(undefined);
  };

  return (
    <Box sx={{ p: 3 }}>
      {view === 'list' && (
        <SalesContractsList
          onCreate={handleCreate}
          onEdit={handleEdit}
          onView={handleView}
        />
      )}
      
      {(view === 'create' || view === 'edit') && (
        <SalesContractForm
          contractId={selectedContractId}
          onSuccess={handleSuccess}
          onCancel={handleCancel}
        />
      )}
      
      {view === 'view' && selectedContractId && (
        <Box>
          {/* Contract detail view would go here */}
          {/* For now, just show the edit form in read-only mode */}
          <SalesContractForm
            contractId={selectedContractId}
            onSuccess={handleSuccess}
            onCancel={handleCancel}
          />
        </Box>
      )}
    </Box>
  );
};
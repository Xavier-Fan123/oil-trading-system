import React, { useState } from 'react';
import { Box } from '@mui/material';
import { SalesContractsList } from '@/components/SalesContracts/SalesContractsList';
import { SalesContractForm } from '@/components/SalesContracts/SalesContractForm';
import { useActivateSalesContract } from '@/hooks/useSalesContracts';

export const SalesContracts: React.FC = () => {
  const [view, setView] = useState<'list' | 'create' | 'edit' | 'view'>('list');
  const [selectedContractId, setSelectedContractId] = useState<string | undefined>();

  const activateMutation = useActivateSalesContract();

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

  const handleActivate = async (contractId: string) => {
    try {
      await activateMutation.mutateAsync(contractId);
      // Refresh the list after activation
      setView('list');
    } catch (error) {
      console.error('Error activating contract:', error);
    }
  };

  return (
    <Box sx={{ p: 3 }}>
      {view === 'list' && (
        <SalesContractsList
          onCreate={handleCreate}
          onEdit={handleEdit}
          onView={handleView}
          onActivate={handleActivate}
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
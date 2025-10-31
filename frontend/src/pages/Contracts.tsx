import React, { useState } from 'react';
import { Box } from '@mui/material';
import { ContractsList } from '@/components/Contracts/ContractsList';
import { ContractForm } from '@/components/Contracts/ContractForm';
import { ContractDetail } from '@/components/Contracts/ContractDetail';
import { useActivatePurchaseContract } from '@/hooks/useContracts';

export const Contracts: React.FC = () => {
  const [view, setView] = useState<'list' | 'create' | 'edit' | 'view'>('list');
  const [selectedContractId, setSelectedContractId] = useState<string | undefined>();

  const activateMutation = useActivatePurchaseContract();

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
        <ContractsList
          onCreate={handleCreate}
          onEdit={handleEdit}
          onView={handleView}
          onActivate={handleActivate}
        />
      )}
      
      {(view === 'create' || view === 'edit') && (
        <ContractForm
          contractId={selectedContractId}
          onSuccess={handleSuccess}
          onCancel={handleCancel}
        />
      )}
      
      {view === 'view' && selectedContractId && (
        <ContractDetail
          contractId={selectedContractId}
          onEdit={() => handleEdit(selectedContractId)}
          onBack={handleCancel}
        />
      )}
    </Box>
  );
};
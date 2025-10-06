import React, { useState } from 'react';
import { Box } from '@mui/material';
import { ShippingOperationsList } from '@/components/Shipping/ShippingOperationsList';
import { ShippingOperationForm } from '@/components/Shipping/ShippingOperationForm';
import { ShippingOperationDetails } from '@/components/Shipping/ShippingOperationDetails';

export const Shipping: React.FC = () => {
  const [view, setView] = useState<'list' | 'create' | 'edit' | 'view'>('list');
  const [selectedOperationId, setSelectedOperationId] = useState<string | undefined>();

  const handleCreate = () => {
    setSelectedOperationId(undefined);
    setView('create');
  };

  const handleEdit = (operationId: string) => {
    setSelectedOperationId(operationId);
    setView('edit');
  };

  const handleView = (operationId: string) => {
    setSelectedOperationId(operationId);
    setView('view');
  };

  const handleSuccess = () => {
    setView('list');
    setSelectedOperationId(undefined);
  };

  const handleCancel = () => {
    setView('list');
    setSelectedOperationId(undefined);
  };

  return (
    <Box sx={{ p: 3 }}>
      {view === 'list' && (
        <ShippingOperationsList
          onCreate={handleCreate}
          onEdit={handleEdit}
          onView={handleView}
        />
      )}
      
      {(view === 'create' || view === 'edit') && (
        <ShippingOperationForm
          open={true}
          onClose={handleCancel}
          onSubmit={handleSuccess}
          initialData={selectedOperationId ? { id: selectedOperationId } : undefined}
        />
      )}
      
      {view === 'view' && selectedOperationId && (
        <ShippingOperationDetails
          open={true}
          operationId={selectedOperationId}
          onClose={handleCancel}
          onEdit={handleEdit}
        />
      )}
    </Box>
  );
};
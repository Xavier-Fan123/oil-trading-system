import React, { useState } from 'react';
import { Box } from '@mui/material';
import { useSearchParams } from 'react-router-dom';
import { SettlementSearch } from '@/components/Settlements/SettlementSearch';
import { SettlementList } from '@/components/Settlements/SettlementList';
import { SettlementDetail } from '@/components/Settlements/SettlementDetail';
import { SettlementEntry } from '@/components/Settlements/SettlementEntry';
import { ContractSettlementListDto } from '@/types/settlement';

export type SettlementView = 'search' | 'list' | 'detail' | 'create' | 'edit';

export const ContractSettlement: React.FC = () => {
  const [searchParams] = useSearchParams();

  // Default to 'list' view so finance team sees all settlements immediately
  const [view, setView] = useState<SettlementView>('list');
  const [selectedSettlementId, setSelectedSettlementId] = useState<string | undefined>();
  const [searchResults, setSearchResults] = useState<ContractSettlementListDto[]>([]);
  const [searchTerm, setSearchTerm] = useState<string>('');

  // Read initial status filter from URL params (e.g., /settlements?status=Draft)
  const initialStatusFilter = searchParams.get('status') || undefined;

  const handleSearch = (term: string, results: ContractSettlementListDto[]) => {
    setSearchTerm(term);
    setSearchResults(results);
    if (results.length > 0) {
      setView('list');
    }
  };

  const handleSettlementSelect = (settlementId: string) => {
    setSelectedSettlementId(settlementId);
    setView('detail');
  };

  const handleCreateNew = () => {
    setSelectedSettlementId(undefined);
    setView('create');
  };

  const handleEdit = (settlementId: string) => {
    setSelectedSettlementId(settlementId);
    setView('edit');
  };

  const handleBackToSearch = () => {
    setView('search');
    setSelectedSettlementId(undefined);
    setSearchResults([]);
    setSearchTerm('');
  };

  const handleBackToList = () => {
    setView('list');
    setSelectedSettlementId(undefined);
  };

  const handleSuccess = () => {
    // After successful creation or update, go back to list
    setView('list');
    setSelectedSettlementId(undefined);
    setSearchResults([]);
    setSearchTerm('');
  };

  const renderView = () => {
    switch (view) {
      case 'search':
        return (
          <SettlementSearch
            onSearch={handleSearch}
            onCreateNew={handleCreateNew}
          />
        );

      case 'list':
        return (
          <SettlementList
            settlements={searchResults}
            searchTerm={searchTerm}
            onSettlementSelect={handleSettlementSelect}
            onCreateNew={handleCreateNew}
            onBackToSearch={handleBackToSearch}
            initialStatusFilter={initialStatusFilter}
          />
        );

      case 'detail':
        return selectedSettlementId ? (
          <SettlementDetail
            settlementId={selectedSettlementId}
            onEdit={() => handleEdit(selectedSettlementId)}
            onBack={handleBackToList}
          />
        ) : (
          <Box>Error: No settlement selected</Box>
        );

      case 'create':
        return (
          <SettlementEntry
            mode="create"
            onSuccess={handleSuccess}
            onCancel={handleBackToList}
          />
        );

      case 'edit':
        return selectedSettlementId ? (
          <SettlementEntry
            mode="edit"
            settlementId={selectedSettlementId}
            onSuccess={handleSuccess}
            onCancel={handleBackToList}
          />
        ) : (
          <Box>Error: No settlement selected for editing</Box>
        );

      default:
        return (
          <SettlementList
            settlements={searchResults}
            searchTerm={searchTerm}
            onSettlementSelect={handleSettlementSelect}
            onCreateNew={handleCreateNew}
            onBackToSearch={handleBackToSearch}
            initialStatusFilter={initialStatusFilter}
          />
        );
    }
  };

  return (
    <Box sx={{ p: 3 }}>
      {renderView()}
    </Box>
  );
};

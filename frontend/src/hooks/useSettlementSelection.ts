import { useState, useCallback } from 'react';

/**
 * useSettlementSelection Hook
 * Manages selection state for bulk settlement operations
 */
export interface UseSettlementSelectionReturn {
  selectedIds: Set<string>;
  selectAll: (ids: string[]) => void;
  deselectAll: () => void;
  toggleSelection: (id: string) => void;
  toggleRowSelection: (id: string) => void;
  isSelected: (id: string) => boolean;
  isAllSelected: (totalIds: string[]) => boolean;
  getSelectedCount: () => number;
  clearSelection: () => void;
}

export const useSettlementSelection = (): UseSettlementSelectionReturn => {
  const [selectedIds, setSelectedIds] = useState<Set<string>>(new Set());

  const selectAll = useCallback((ids: string[]) => {
    setSelectedIds(new Set(ids));
  }, []);

  const deselectAll = useCallback(() => {
    setSelectedIds(new Set());
  }, []);

  const toggleSelection = useCallback((id: string) => {
    setSelectedIds((prev) => {
      const newSet = new Set(prev);
      if (newSet.has(id)) {
        newSet.delete(id);
      } else {
        newSet.add(id);
      }
      return newSet;
    });
  }, []);

  const toggleRowSelection = useCallback((id: string) => {
    toggleSelection(id);
  }, [toggleSelection]);

  const isSelected = useCallback(
    (id: string) => selectedIds.has(id),
    [selectedIds]
  );

  const isAllSelected = useCallback(
    (totalIds: string[]) => {
      if (totalIds.length === 0) return false;
      return totalIds.every((id) => selectedIds.has(id));
    },
    [selectedIds]
  );

  const getSelectedCount = useCallback(() => {
    return selectedIds.size;
  }, [selectedIds]);

  const clearSelection = useCallback(() => {
    setSelectedIds(new Set());
  }, []);

  return {
    selectedIds,
    selectAll,
    deselectAll,
    toggleSelection,
    toggleRowSelection,
    isSelected,
    isAllSelected,
    getSelectedCount,
    clearSelection,
  };
};

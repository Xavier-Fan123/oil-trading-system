import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { 
  UpdateTagDto,
  AddContractTagDto,
  TagCategory 
} from '../types/contracts';
import { tagApi } from '../services/tagApi';

// Query hooks
export const useTags = () => {
  return useQuery({
    queryKey: ['tags'],
    queryFn: tagApi.getTags,
  });
};

export const useTag = (id: string) => {
  return useQuery({
    queryKey: ['tag', id],
    queryFn: () => tagApi.getTag(id),
    enabled: !!id,
  });
};

export const useTagsByCategory = (category: TagCategory) => {
  return useQuery({
    queryKey: ['tags', 'category', category],
    queryFn: () => tagApi.getTagsByCategory(category),
  });
};

export const useContractTags = (contractId: string, contractType: string) => {
  return useQuery({
    queryKey: ['contract-tags', contractId, contractType],
    queryFn: () => tagApi.getContractTags(contractId, contractType),
    enabled: !!contractId && !!contractType,
  });
};

export const usePredefinedTagInfo = () => {
  return useQuery({
    queryKey: ['predefined-tag-info'],
    queryFn: tagApi.getPredefinedTagInfo,
  });
};

export const useTagUsageStatistics = () => {
  return useQuery({
    queryKey: ['tag-usage-statistics'],
    queryFn: tagApi.getTagUsageStatistics,
  });
};

// Mutation hooks
export const useCreateTag = () => {
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: tagApi.createTag,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['tags'] });
      queryClient.invalidateQueries({ queryKey: ['tag-usage-statistics'] });
    },
  });
};

export const useUpdateTag = () => {
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: ({ id, dto }: { id: string; dto: UpdateTagDto }) => 
      tagApi.updateTag(id, dto),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ['tags'] });
      queryClient.invalidateQueries({ queryKey: ['tag', variables.id] });
    },
  });
};

export const useDeleteTag = () => {
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: tagApi.deleteTag,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['tags'] });
      queryClient.invalidateQueries({ queryKey: ['tag-usage-statistics'] });
    },
  });
};

export const useAddTagToContract = () => {
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: ({ 
      contractId, 
      contractType, 
      dto 
    }: { 
      contractId: string; 
      contractType: string; 
      dto: AddContractTagDto;
    }) => 
      tagApi.addTagToContract(contractId, contractType, dto),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ 
        queryKey: ['contract-tags', variables.contractId, variables.contractType] 
      });
      queryClient.invalidateQueries({ queryKey: ['tags'] }); // Update usage counts
    },
  });
};

export const useRemoveTagFromContract = () => {
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: ({ 
      contractId, 
      contractType, 
      tagId 
    }: { 
      contractId: string; 
      contractType: string; 
      tagId: string;
    }) => 
      tagApi.removeTagFromContract(contractId, contractType, tagId),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ 
        queryKey: ['contract-tags', variables.contractId, variables.contractType] 
      });
      queryClient.invalidateQueries({ queryKey: ['tags'] }); // Update usage counts
    },
  });
};

export const useValidateTagForContract = () => {
  return useMutation({
    mutationFn: ({ 
      tagId, 
      contractId, 
      contractType 
    }: { 
      tagId: string; 
      contractId: string; 
      contractType: string;
    }) => 
      tagApi.validateTagForContract(tagId, contractId, contractType),
  });
};

export const useCreatePredefinedTags = () => {
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: tagApi.createPredefinedTags,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['tags'] });
      queryClient.invalidateQueries({ queryKey: ['tag-usage-statistics'] });
    },
  });
};

export const useSynchronizeTagUsageCounts = () => {
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: tagApi.synchronizeTagUsageCounts,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['tags'] });
      queryClient.invalidateQueries({ queryKey: ['tag-usage-statistics'] });
    },
  });
};
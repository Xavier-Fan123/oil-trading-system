// Product Types
export interface Product {
  id: string;
  name: string;
  code: string;
  productName: string;
  productCode: string;
  type: ProductType;
  grade: string;
  specification: string;
  unitOfMeasure: string;
  density: number;
  origin: string;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string;
}

export interface CreateProductRequest {
  code: string;
  name: string;
  type: ProductType;
  grade?: string;
  specification?: string;
  unitOfMeasure: string;
  density?: number;
  origin?: string;
}

export interface UpdateProductRequest {
  name: string;
  grade?: string;
  specification?: string;
  density?: number;
  origin?: string;
}

export enum ProductType {
  CrudeOil = 1,
  RefinedProducts = 2,
  NaturalGas = 3,
  Petrochemicals = 4
}

export const ProductTypeLabels: Record<ProductType, string> = {
  [ProductType.CrudeOil]: 'Crude Oil',
  [ProductType.RefinedProducts]: 'Refined Products',
  [ProductType.NaturalGas]: 'Natural Gas',
  [ProductType.Petrochemicals]: 'Petrochemicals'
};

export interface ProductFilters {
  type?: ProductType;
  code?: string;
  name?: string;
  isActive?: boolean;
  pageNumber?: number;
  pageSize?: number;
}
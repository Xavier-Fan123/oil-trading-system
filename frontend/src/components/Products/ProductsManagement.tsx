import { useState, useEffect } from 'react';
import {
  Container,
  Card,
  CardContent,
  Typography,
  Box,
  Button,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Paper,
  Chip,
  IconButton,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  TextField,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Alert,
  CircularProgress,
  Tooltip
} from '@mui/material';
import {
  Add as AddIcon,
  Edit as EditIcon,
  Delete as DeleteIcon,
  Visibility as ViewIcon
} from '@mui/icons-material';
import { productsApi } from '@/services/productsApi';
import {
  Product,
  CreateProductRequest,
  ProductType,
  ProductTypeLabels
} from '@/types/products';

interface ProductFormData extends Omit<CreateProductRequest, 'type'> {
  type: ProductType | '';
}

export default function ProductsManagement() {
  const [products, setProducts] = useState<Product[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  
  // Dialog states
  const [openDialog, setOpenDialog] = useState(false);
  const [dialogMode, setDialogMode] = useState<'create' | 'edit' | 'view'>('create');
  const [selectedProduct, setSelectedProduct] = useState<Product | null>(null);
  
  // Form state
  const [formData, setFormData] = useState<ProductFormData>({
    code: '',
    name: '',
    type: '',
    grade: '',
    specification: '',
    unitOfMeasure: 'BBL',
    density: undefined,
    origin: ''
  });

  useEffect(() => {
    loadProducts();
  }, []);

  const loadProducts = async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await productsApi.getAll();
      setProducts(data);
    } catch (err) {
      console.error('Error loading products:', err);
      setError('Failed to load products. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  const handleOpenDialog = (mode: 'create' | 'edit' | 'view', product?: Product) => {
    setDialogMode(mode);
    setSelectedProduct(product || null);
    
    if (mode === 'create') {
      setFormData({
        code: '',
        name: '',
        type: '',
        grade: '',
        specification: '',
        unitOfMeasure: 'BBL',
        density: undefined,
        origin: ''
      });
    } else if (product) {
      setFormData({
        code: product.code,
        name: product.name,
        type: product.type,
        grade: product.grade || '',
        specification: product.specification || '',
        unitOfMeasure: product.unitOfMeasure,
        density: product.density || undefined,
        origin: product.origin || ''
      });
    }
    
    setOpenDialog(true);
  };

  const handleCloseDialog = () => {
    setOpenDialog(false);
    setSelectedProduct(null);
    setFormData({
      code: '',
      name: '',
      type: '',
      grade: '',
      specification: '',
      unitOfMeasure: 'BBL',
      density: undefined,
      origin: ''
    });
  };

  const handleFormSubmit = async () => {
    try {
      if (formData.type === '') {
        setError('Product type is required');
        return;
      }

      if (dialogMode === 'create') {
        await productsApi.create({
          ...formData,
          type: formData.type as ProductType
        });
      } else if (dialogMode === 'edit' && selectedProduct) {
        await productsApi.update(selectedProduct.id, {
          name: formData.name,
          grade: formData.grade || undefined,
          specification: formData.specification || undefined,
          density: formData.density,
          origin: formData.origin || undefined
        });
      }
      
      handleCloseDialog();
      await loadProducts();
    } catch (err) {
      console.error(`Error ${dialogMode === 'create' ? 'creating' : 'updating'} product:`, err);
      setError(`Failed to ${dialogMode === 'create' ? 'create' : 'update'} product. Please try again.`);
    }
  };

  const handleDelete = async (product: Product) => {
    if (window.confirm(`Are you sure you want to delete ${product.name}?`)) {
      try {
        await productsApi.delete(product.id);
        await loadProducts();
      } catch (err) {
        console.error('Error deleting product:', err);
        setError('Failed to delete product. Please try again.');
      }
    }
  };

  const getProductTypeColor = (type: ProductType): "default" | "primary" | "secondary" | "error" | "info" | "success" | "warning" => {
    switch (type) {
      case ProductType.CrudeOil: return 'primary';
      case ProductType.RefinedProducts: return 'success';
      case ProductType.NaturalGas: return 'info';
      case ProductType.Petrochemicals: return 'warning';
      default: return 'default';
    }
  };

  if (loading) {
    return (
      <Container maxWidth="xl">
        <Box display="flex" justifyContent="center" alignItems="center" minHeight="400px">
          <CircularProgress />
        </Box>
      </Container>
    );
  }

  return (
    <Container maxWidth="xl">
      <Box sx={{ mb: 3 }}>
        <Typography variant="h4" component="h1" gutterBottom>
          Products Management
        </Typography>
        <Typography variant="subtitle1" color="text.secondary">
          Manage oil products, grades, and specifications
        </Typography>
      </Box>

      {error && (
        <Alert severity="error" sx={{ mb: 3 }} onClose={() => setError(null)}>
          {error}
        </Alert>
      )}

      <Card>
        <CardContent>
          <Box display="flex" justifyContent="space-between" alignItems="center" mb={2}>
            <Typography variant="h6">Products ({products.length})</Typography>
            <Button
              variant="contained"
              startIcon={<AddIcon />}
              onClick={() => handleOpenDialog('create')}
            >
              Add Product
            </Button>
          </Box>

          <TableContainer component={Paper} variant="outlined">
            <Table>
              <TableHead>
                <TableRow>
                  <TableCell>Code</TableCell>
                  <TableCell>Name</TableCell>
                  <TableCell>Type</TableCell>
                  <TableCell>Grade</TableCell>
                  <TableCell>Unit</TableCell>
                  <TableCell>Density</TableCell>
                  <TableCell>Origin</TableCell>
                  <TableCell>Status</TableCell>
                  <TableCell align="right">Actions</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {products.map((product) => (
                  <TableRow key={product.id} hover>
                    <TableCell>
                      <Typography variant="body2" fontFamily="monospace">
                        {product.code}
                      </Typography>
                    </TableCell>
                    <TableCell>
                      <Typography variant="body2" fontWeight="medium">
                        {product.name}
                      </Typography>
                    </TableCell>
                    <TableCell>
                      <Chip
                        label={ProductTypeLabels[product.type]}
                        color={getProductTypeColor(product.type)}
                        size="small"
                      />
                    </TableCell>
                    <TableCell>
                      <Typography variant="body2">
                        {product.grade || '-'}
                      </Typography>
                    </TableCell>
                    <TableCell>
                      <Typography variant="body2">
                        {product.unitOfMeasure}
                      </Typography>
                    </TableCell>
                    <TableCell>
                      <Typography variant="body2">
                        {product.density ? `${product.density.toFixed(3)}` : '-'}
                      </Typography>
                    </TableCell>
                    <TableCell>
                      <Typography variant="body2">
                        {product.origin || '-'}
                      </Typography>
                    </TableCell>
                    <TableCell>
                      <Chip
                        label={product.isActive ? 'Active' : 'Inactive'}
                        color={product.isActive ? 'success' : 'default'}
                        size="small"
                      />
                    </TableCell>
                    <TableCell align="right">
                      <Tooltip title="View Details">
                        <IconButton
                          size="small"
                          onClick={() => handleOpenDialog('view', product)}
                        >
                          <ViewIcon />
                        </IconButton>
                      </Tooltip>
                      <Tooltip title="Edit Product">
                        <IconButton
                          size="small"
                          onClick={() => handleOpenDialog('edit', product)}
                        >
                          <EditIcon />
                        </IconButton>
                      </Tooltip>
                      <Tooltip title="Delete Product">
                        <IconButton
                          size="small"
                          color="error"
                          onClick={() => handleDelete(product)}
                        >
                          <DeleteIcon />
                        </IconButton>
                      </Tooltip>
                    </TableCell>
                  </TableRow>
                ))}
                {products.length === 0 && (
                  <TableRow>
                    <TableCell colSpan={9} align="center">
                      <Typography variant="body2" color="text.secondary">
                        No products found. Click "Add Product" to create one.
                      </Typography>
                    </TableCell>
                  </TableRow>
                )}
              </TableBody>
            </Table>
          </TableContainer>
        </CardContent>
      </Card>

      {/* Product Dialog */}
      <Dialog open={openDialog} onClose={handleCloseDialog} maxWidth="md" fullWidth>
        <DialogTitle>
          {dialogMode === 'create' && 'Add New Product'}
          {dialogMode === 'edit' && 'Edit Product'}
          {dialogMode === 'view' && 'Product Details'}
        </DialogTitle>
        <DialogContent>
          <Box sx={{ pt: 1 }}>
            <TextField
              fullWidth
              label="Product Code"
              value={formData.code}
              onChange={(e) => setFormData({ ...formData, code: e.target.value })}
              disabled={dialogMode === 'edit' || dialogMode === 'view'}
              required
              sx={{ mb: 2 }}
            />
            
            <TextField
              fullWidth
              label="Product Name"
              value={formData.name}
              onChange={(e) => setFormData({ ...formData, name: e.target.value })}
              disabled={dialogMode === 'view'}
              required
              sx={{ mb: 2 }}
            />

            <FormControl fullWidth sx={{ mb: 2 }}>
              <InputLabel>Product Type *</InputLabel>
              <Select
                value={formData.type}
                onChange={(e) => setFormData({ ...formData, type: e.target.value as ProductType })}
                disabled={dialogMode === 'edit' || dialogMode === 'view'}
                required
              >
                {Object.entries(ProductTypeLabels).map(([value, label]) => (
                  <MenuItem key={value} value={parseInt(value)}>
                    {label}
                  </MenuItem>
                ))}
              </Select>
            </FormControl>

            <TextField
              fullWidth
              label="Grade"
              value={formData.grade}
              onChange={(e) => setFormData({ ...formData, grade: e.target.value })}
              disabled={dialogMode === 'view'}
              sx={{ mb: 2 }}
            />

            <TextField
              fullWidth
              label="Unit of Measure"
              value={formData.unitOfMeasure}
              onChange={(e) => setFormData({ ...formData, unitOfMeasure: e.target.value })}
              disabled={dialogMode === 'view'}
              sx={{ mb: 2 }}
            />

            <TextField
              fullWidth
              label="Density"
              type="number"
              value={formData.density || ''}
              onChange={(e) => setFormData({ 
                ...formData, 
                density: e.target.value ? parseFloat(e.target.value) : undefined 
              })}
              disabled={dialogMode === 'view'}
              sx={{ mb: 2 }}
            />

            <TextField
              fullWidth
              label="Origin"
              value={formData.origin}
              onChange={(e) => setFormData({ ...formData, origin: e.target.value })}
              disabled={dialogMode === 'view'}
              sx={{ mb: 2 }}
            />

            <TextField
              fullWidth
              label="Specification"
              value={formData.specification}
              onChange={(e) => setFormData({ ...formData, specification: e.target.value })}
              disabled={dialogMode === 'view'}
              multiline
              rows={3}
            />
          </Box>
        </DialogContent>
        <DialogActions>
          <Button onClick={handleCloseDialog}>
            {dialogMode === 'view' ? 'Close' : 'Cancel'}
          </Button>
          {dialogMode !== 'view' && (
            <Button onClick={handleFormSubmit} variant="contained">
              {dialogMode === 'create' ? 'Create' : 'Update'}
            </Button>
          )}
        </DialogActions>
      </Dialog>
    </Container>
  );
}
# Oil Trading System - Frontend

Modern React/Next.js frontend for the Oil Trading System with market data upload capabilities.

## 🚀 Quick Start

### Prerequisites
- Node.js 18+ 
- npm or yarn

### Installation & Development

```bash
# Navigate to the frontend directory
cd src/OilTrading.Web

# Install dependencies
npm install

# Start development server
npm run dev
```

The application will be available at: http://localhost:3000

### Environment Configuration

Create or update `.env.local`:
```
NEXT_PUBLIC_API_URL=http://localhost:5000/api
NEXT_PUBLIC_APP_NAME=Oil Trading System
NEXT_PUBLIC_DEBUG=true
```

## 📋 Features

### ✅ Market Data Upload
- **File Upload**: Drag & drop Excel files (.xlsx, .xls)
- **File Types**: Daily Prices (MOPS) and ICE Settlement Prices
- **Real-time Processing**: Shows upload progress and results
- **Error Handling**: Comprehensive error messages and validation

### ✅ Dashboard
- **Overview**: Key trading metrics and statistics
- **Navigation**: Quick access to all system features
- **Responsive**: Works on desktop, tablet, and mobile

## 🔧 Tech Stack

- **Framework**: Next.js 14 with App Router
- **Language**: TypeScript
- **Styling**: Tailwind CSS
- **UI Components**: Custom components with Radix UI primitives
- **HTTP Client**: Axios
- **File Upload**: React Dropzone
- **State Management**: React Query (@tanstack/react-query)
- **Icons**: Lucide React

## 📁 Project Structure

```
src/
├── app/                    # Next.js App Router pages
│   ├── globals.css        # Global styles
│   ├── layout.tsx         # Root layout
│   └── page.tsx           # Home/Dashboard page
├── components/            # React components
│   ├── ui/               # Reusable UI components
│   └── MarketDataUpload.tsx  # Market data upload component
├── lib/                   # Utilities and configurations
│   ├── api.ts            # API client and endpoints
│   └── utils.ts          # Utility functions
└── types/                 # TypeScript type definitions
```

## 🔗 API Integration

The frontend connects to the .NET backend API running on `http://localhost:5000`.

### Market Data Endpoints
- `POST /api/market-data/upload` - Upload Excel files
- `GET /api/market-data/latest` - Get latest prices
- `GET /api/market-data/history/{productCode}` - Get price history

### Paper Contracts Endpoints
- `GET /api/paper-contracts` - Get all paper contracts
- `POST /api/paper-contracts` - Create new paper contract
- `POST /api/paper-contracts/update-mtm` - Update mark-to-market

## 📱 Usage Instructions

### Market Data Upload

1. **Navigate to Dashboard**: Open http://localhost:3000
2. **Access Upload**: Click "Upload Market Data" card or scroll to upload section
3. **Select File Type**: Choose between "Daily Prices" or "ICE Settlement Prices"
4. **Upload File**: Drag & drop or click to select Excel file
5. **Review Results**: View upload statistics and any errors

### Supported File Formats

#### Daily Prices (MOPS)
- File should contain "Origin" worksheet
- Columns C-J contain different product prices
- Row 4+ contains price data
- Column B contains timestamps

#### ICE Settlement Prices
- File should contain "Settlement Price" worksheet  
- Columns B,D,F contain contract names
- Columns C,E,G contain settlement prices
- Rows 4-20 contain contract months

## 🔧 Development

### Available Scripts

```bash
npm run dev          # Start development server
npm run build        # Build for production
npm run start        # Start production server
npm run lint         # Run ESLint
npm run type-check   # Run TypeScript compiler check
```

### Adding New Features

1. Create components in `src/components/`
2. Add API functions to `src/lib/api.ts`
3. Update types in appropriate type files
4. Add new pages in `src/app/`

## 🚨 Troubleshooting

### Common Issues

**Connection Refused**
- Ensure backend API is running on http://localhost:5000
- Check CORS configuration in backend
- Verify environment variables in `.env.local`

**File Upload Errors**
- Check file format (.xlsx or .xls only)
- Verify file size (max 10MB)
- Ensure correct worksheet names ("Origin" or "Settlement Price")

**TypeScript Errors**
- Run `npm run type-check` to identify issues
- Check import paths and type definitions
- Ensure all dependencies are installed

## 📦 Deployment

### Production Build

```bash
npm run build
npm run start
```

### Environment Variables for Production

```
NEXT_PUBLIC_API_URL=https://your-api-domain.com/api
NEXT_PUBLIC_APP_NAME=Oil Trading System
NEXT_PUBLIC_DEBUG=false
```

## 🤝 Integration with Backend

The frontend is designed to work seamlessly with the .NET Core backend:

1. **Start Backend**: Run the .NET API on http://localhost:5000
2. **Start Frontend**: Run the Next.js app on http://localhost:3000
3. **Test Integration**: Use the upload feature to verify connectivity

Both applications should be running simultaneously for full functionality.
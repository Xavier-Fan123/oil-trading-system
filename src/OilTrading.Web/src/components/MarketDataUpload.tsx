'use client'

import React, { useState, useCallback } from 'react'
import { useDropzone } from 'react-dropzone'
import { Upload, FileSpreadsheet, Check, X, AlertCircle, Loader2 } from 'lucide-react'
import { marketDataApi } from '@/lib/api'

interface UploadResult {
  success: boolean
  recordsProcessed: number
  recordsCreated: number
  recordsUpdated: number
  recordsSkipped: number
  messages: string[]
  errors: string[]
  importedPrices?: Array<{
    id: string
    priceDate: string
    productCode: string
    productName: string
    price: number
    currency: string
    priceType: string
  }>
}

export default function MarketDataUpload() {
  const [uploading, setUploading] = useState(false)
  const [result, setResult] = useState<UploadResult | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [fileType, setFileType] = useState<'DailyPrices' | 'ICESettlement'>('DailyPrices')

  const onDrop = useCallback(async (acceptedFiles: File[]) => {
    if (acceptedFiles.length === 0) return
    
    const file = acceptedFiles[0]
    setUploading(true)
    setError(null)
    setResult(null)
    
    try {
      const uploadResult = await marketDataApi.uploadFile(file, fileType)
      setResult(uploadResult)
      
      if (!uploadResult.success && uploadResult.errors?.length > 0) {
        setError(uploadResult.errors.join(', '))
      }
    } catch (err: any) {
      console.error('Upload error:', err)
      console.error('Error response data:', err.response?.data)
      
      let errorMessage = 'Upload failed. Please check your file format and try again.'
      
      if (err.response?.data) {
        const data = err.response.data
        if (data.details && Array.isArray(data.details)) {
          errorMessage = data.details.join(', ')
        } else if (data.error) {
          errorMessage = data.error
        } else if (typeof data === 'string') {
          errorMessage = data
        }
      } else if (err.message) {
        errorMessage = err.message
      }
      
      setError(errorMessage)
    } finally {
      setUploading(false)
    }
  }, [fileType])

  const { getRootProps, getInputProps, isDragActive } = useDropzone({
    onDrop,
    accept: {
      'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet': ['.xlsx'],
      'application/vnd.ms-excel': ['.xls'],
    },
    maxFiles: 1,
    disabled: uploading,
  })

  return (
    <div className="max-w-4xl mx-auto p-6 space-y-6">
      <div className="bg-white rounded-lg shadow-sm border">
        <div className="px-6 py-4 border-b border-gray-200">
          <h2 className="text-xl font-semibold text-gray-900">Market Data Upload</h2>
          <p className="text-sm text-gray-600 mt-1">
            Upload Excel files containing daily MOPS prices or ICE settlement prices
          </p>
        </div>
        
        <div className="px-6 py-4">
          {/* File Type Selection */}
          <div className="mb-6">
            <label className="block text-sm font-medium text-gray-700 mb-2">
              File Type
            </label>
            <div className="flex space-x-4">
              <label className="flex items-center">
                <input
                  type="radio"
                  name="fileType"
                  value="DailyPrices"
                  checked={fileType === 'DailyPrices'}
                  onChange={(e) => setFileType(e.target.value as 'DailyPrices')}
                  className="mr-2"
                  disabled={uploading}
                />
                <span className="text-sm">Daily Prices (MOPS)</span>
              </label>
              <label className="flex items-center">
                <input
                  type="radio"
                  name="fileType"
                  value="ICESettlement"
                  checked={fileType === 'ICESettlement'}
                  onChange={(e) => setFileType(e.target.value as 'ICESettlement')}
                  className="mr-2"
                  disabled={uploading}
                />
                <span className="text-sm">ICE Settlement Prices</span>
              </label>
            </div>
          </div>

          {/* Upload Zone */}
          <div
            {...getRootProps()}
            className={`
              border-2 border-dashed rounded-lg p-12 text-center cursor-pointer
              transition-all duration-200 
              ${isDragActive ? 'border-blue-500 bg-blue-50' : 'border-gray-300 hover:border-gray-400'}
              ${uploading ? 'opacity-50 cursor-not-allowed' : ''}
            `}
          >
            <input {...getInputProps()} />
            
            <div className="flex flex-col items-center space-y-4">
              {uploading ? (
                <>
                  <Loader2 className="w-12 h-12 text-blue-600 animate-spin" />
                  <p className="text-gray-600">Uploading and processing...</p>
                  <p className="text-xs text-gray-500">This may take up to 2 minutes</p>
                </>
              ) : (
                <>
                  <FileSpreadsheet className="w-12 h-12 text-gray-400" />
                  <div>
                    <p className="text-lg font-medium text-gray-700">
                      {isDragActive ? 'Drop the Excel file here' : 'Drag & drop Excel file here'}
                    </p>
                    <p className="text-sm text-gray-500 mt-1">or click to select file</p>
                  </div>
                  <div className="text-xs text-gray-400 text-center">
                    <p>Supports: .xlsx and .xls files (max 10MB)</p>
                    <p className="mt-1">
                      {fileType === 'DailyPrices' 
                        ? 'Upload Daily Prices template for MOPS spot prices'
                        : 'Upload ICE Settlement Price template for futures prices'
                      }
                    </p>
                  </div>
                </>
              )}
            </div>
          </div>
        </div>
      </div>

      {/* Upload Result */}
      {result && (
        <div className={`p-4 rounded-lg border ${
          result.success 
            ? 'bg-green-50 border-green-200' 
            : 'bg-yellow-50 border-yellow-200'
        }`}>
          <div className="flex items-start space-x-3">
            {result.success ? (
              <Check className="w-5 h-5 text-green-600 mt-0.5 flex-shrink-0" />
            ) : (
              <AlertCircle className="w-5 h-5 text-yellow-600 mt-0.5 flex-shrink-0" />
            )}
            <div className="flex-1 min-w-0">
              <h3 className="font-medium text-gray-900">
                {result.success ? 'Upload Successful' : 'Upload Completed with Warnings'}
              </h3>
              
              <div className="mt-2 grid grid-cols-2 md:grid-cols-4 gap-4 text-sm">
                <div className="text-gray-600">
                  <span className="font-semibold">{result.recordsProcessed}</span>
                  <br />
                  <span className="text-xs">Processed</span>
                </div>
                <div className="text-green-600">
                  <span className="font-semibold">{result.recordsCreated}</span>
                  <br />
                  <span className="text-xs">Created</span>
                </div>
                <div className="text-blue-600">
                  <span className="font-semibold">{result.recordsUpdated}</span>
                  <br />
                  <span className="text-xs">Updated</span>
                </div>
                {result.recordsSkipped > 0 && (
                  <div className="text-yellow-600">
                    <span className="font-semibold">{result.recordsSkipped}</span>
                    <br />
                    <span className="text-xs">Skipped</span>
                  </div>
                )}
              </div>
              
              {result.messages.length > 0 && (
                <div className="mt-3">
                  <p className="text-sm font-medium text-gray-700">Messages:</p>
                  <ul className="mt-1 text-sm text-gray-600 space-y-1">
                    {result.messages.map((msg, idx) => (
                      <li key={idx} className="flex items-start">
                        <span className="w-1 h-1 bg-gray-400 rounded-full mt-2 mr-2 flex-shrink-0"></span>
                        {msg}
                      </li>
                    ))}
                  </ul>
                </div>
              )}

              {result.importedPrices && result.importedPrices.length > 0 && (
                <div className="mt-4">
                  <p className="text-sm font-medium text-gray-700">
                    Sample of imported prices ({Math.min(3, result.importedPrices.length)} of {result.importedPrices.length}):
                  </p>
                  <div className="mt-2 text-xs space-y-1">
                    {result.importedPrices.slice(0, 3).map((price, idx) => (
                      <div key={idx} className="text-gray-600">
                        {price.productName}: {price.currency} {price.price.toFixed(2)} ({price.priceType})
                      </div>
                    ))}
                  </div>
                </div>
              )}
            </div>
          </div>
        </div>
      )}

      {/* Error Display */}
      {error && (
        <div className="p-4 rounded-lg bg-red-50 border border-red-200">
          <div className="flex items-start space-x-3">
            <X className="w-5 h-5 text-red-600 mt-0.5 flex-shrink-0" />
            <div className="flex-1 min-w-0">
              <h3 className="font-medium text-red-900">Upload Failed</h3>
              <p className="mt-1 text-sm text-red-700 whitespace-pre-wrap">{error}</p>
            </div>
          </div>
        </div>
      )}

      {/* Instructions */}
      <div className="bg-gray-50 rounded-lg p-4">
        <h3 className="font-medium text-gray-900 mb-2">File Format Instructions:</h3>
        <div className="text-sm text-gray-600 space-y-2">
          <div>
            <p className="font-medium">Daily Prices:</p>
            <ul className="ml-4 space-y-1 text-xs">
              <li>• Use the daily prices template with "Origin" worksheet</li>
              <li>• Contains MOPS spot prices for 380cst, 180cst, 0.5% sulfur, etc.</li>
              <li>• Prices should be in columns C-J starting from row 4</li>
            </ul>
          </div>
          <div>
            <p className="font-medium">ICE Settlement Prices:</p>
            <ul className="ml-4 space-y-1 text-xs">
              <li>• Use the ICE settlement template with "Settlement Price" worksheet</li>
              <li>• Contains futures settlement prices for 380cst, 0.5%, Gasoil contracts</li>
              <li>• Contract months and prices should start from row 4</li>
            </ul>
          </div>
        </div>
      </div>
    </div>
  )
}
'use client';
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { BarChart3, FileText, TrendingUp, Users, Upload } from 'lucide-react'
import MarketDataUpload from '@/components/MarketDataUpload'

export default function HomePage() {
  return (
    <div className="container mx-auto p-6">
      <div className="mb-8">
        <h1 className="text-4xl font-bold mb-2">Oil Trading & Risk Management</h1>
        <p className="text-lg text-muted-foreground">
          Enterprise-grade platform for managing oil trading operations and risk exposure
        </p>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6 mb-8">
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Active Contracts</CardTitle>
            <FileText className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">127</div>
            <p className="text-xs text-muted-foreground">+12% from last month</p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Total Volume</CardTitle>
            <BarChart3 className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">2.4M</div>
            <p className="text-xs text-muted-foreground">Barrels this quarter</p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Portfolio Value</CardTitle>
            <TrendingUp className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">$184.2M</div>
            <p className="text-xs text-muted-foreground">+5.2% from yesterday</p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Trading Partners</CardTitle>
            <Users className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">48</div>
            <p className="text-xs text-muted-foreground">Active relationships</p>
          </CardContent>
        </Card>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <Card>
          <CardHeader>
            <CardTitle>Purchase Contracts</CardTitle>
            <CardDescription>Manage your supply agreements</CardDescription>
          </CardHeader>
          <CardContent>
            <Button className="w-full">View Purchase Contracts</Button>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Sales Contracts</CardTitle>
            <CardDescription>Track your customer agreements</CardDescription>
          </CardHeader>
          <CardContent>
            <Button className="w-full">View Sales Contracts</Button>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Risk Dashboard</CardTitle>
            <CardDescription>Monitor exposure and risk metrics</CardDescription>
          </CardHeader>
          <CardContent>
            <Button className="w-full">View Risk Dashboard</Button>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Market Data Upload</CardTitle>
            <CardDescription>Upload Excel files with MOPS and ICE prices</CardDescription>
          </CardHeader>
          <CardContent>
            <Button className="w-full" onClick={() => document.getElementById('market-data-section')?.scrollIntoView({ behavior: 'smooth' })}>
              <Upload className="w-4 h-4 mr-2" />
              Upload Market Data
            </Button>
          </CardContent>
        </Card>
      </div>

      {/* Market Data Upload Section */}
      <div id="market-data-section" className="mt-8">
        <MarketDataUpload />
      </div>
    </div>
  )
}
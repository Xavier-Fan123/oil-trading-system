import React from 'react'
import { Container, Typography, Box, Card, CardContent } from '@mui/material'

export const DashboardSimple: React.FC = () => {
  return (
    <Container maxWidth="lg" sx={{ mt: 4, mb: 4 }}>
      <Typography variant="h4" gutterBottom>
        Oil Trading Dashboard
      </Typography>
      
      <Box sx={{ display: 'grid', gap: 3, gridTemplateColumns: 'repeat(auto-fit, minmax(300px, 1fr))' }}>
        <Card>
          <CardContent>
            <Typography variant="h6" color="primary">
              System Status
            </Typography>
            <Typography variant="h3" color="success.main">
              ✅ Online
            </Typography>
            <Typography variant="body2" color="text.secondary">
              Frontend and Backend Connected
            </Typography>
          </CardContent>
        </Card>

        <Card>
          <CardContent>
            <Typography variant="h6" color="primary">
              Quick Test
            </Typography>
            <Typography variant="body1">
              If you can see this page, the frontend is working correctly!
            </Typography>
            <Typography variant="body2" color="text.secondary">
              Frontend: http://localhost:3000
            </Typography>
            <Typography variant="body2" color="text.secondary">
              Backend API: http://localhost:5000
            </Typography>
          </CardContent>
        </Card>

        <Card>
          <CardContent>
            <Typography variant="h6" color="primary">
              Next Steps
            </Typography>
            <Typography variant="body2">
              • Navigate using the top menu
            </Typography>
            <Typography variant="body2">
              • Test Purchase Contracts
            </Typography>
            <Typography variant="body2">
              • Check Risk Management
            </Typography>
          </CardContent>
        </Card>
      </Box>
    </Container>
  )
}

export default DashboardSimple
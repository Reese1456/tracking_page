import LocalShippingIcon from '@mui/icons-material/LocalShipping'
import AppBar from '@mui/material/AppBar'
import Container from '@mui/material/Container'
import Toolbar from '@mui/material/Toolbar'
import Typography from '@mui/material/Typography'
import { Navigate, Route, Routes, Link as RouterLink } from 'react-router'
import SearchPage from './pages/SearchPage.jsx'
import TrackingPage from './pages/TrackingPage.jsx'

// The router itself is in main.jsx, so tests can render App inside a
// MemoryRouter that starts at any URL.
function App() {
  return (
    <>
      <AppBar position="static" elevation={0}>
        <Toolbar>
          <LocalShippingIcon sx={{ mr: 1 }} />
          <Typography
            component={RouterLink}
            to="/"
            variant="h6"
            color="inherit"
            sx={{ textDecoration: 'none' }}
          >
            Delivery Tracking
          </Typography>
        </Toolbar>
      </AppBar>
      <Container component="main" maxWidth="lg" sx={{ py: { xs: 2, md: 4 } }}>
        <Routes>
          <Route path="/" element={<SearchPage />} />
          <Route path="/track/:waybill" element={<TrackingPage />} />
          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </Container>
    </>
  )
}

export default App

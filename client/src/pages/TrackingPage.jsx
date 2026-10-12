import { ExpandMore as ExpandMoreIcon } from '@mui/icons-material'
import Accordion from '@mui/material/Accordion'
import AccordionDetails from '@mui/material/AccordionDetails'
import AccordionSummary from '@mui/material/AccordionSummary'
import Alert from '@mui/material/Alert'
import AlertTitle from '@mui/material/AlertTitle'
import Box from '@mui/material/Box'
import Button from '@mui/material/Button'
import Paper from '@mui/material/Paper'
import Stack from '@mui/material/Stack'
import Typography from '@mui/material/Typography'
import useMediaQuery from '@mui/material/useMediaQuery'
import { useParams } from 'react-router'
import DeliveryStepper from '../components/DeliveryStepper.jsx'
import ReceiverCard from '../components/ReceiverCard.jsx'
import ShipmentSummary from '../components/ShipmentSummary.jsx'
import TrackingHistory from '../components/TrackingHistory.jsx'
import TrackingSkeleton from '../components/TrackingSkeleton.jsx'
import WaybillSearch from '../components/WaybillSearch.jsx'
import { useTracking } from '../hooks/useTracking.js'
import { formatDate } from '../utils/format.js'

export default function TrackingPage() {
  const { waybill } = useParams()
  const { state, data, retry } = useTracking(waybill)

  return (
    <Stack spacing={3}>
      {/* A new key resets the box to the new waybill after a search. */}
      <WaybillSearch key={waybill} initialValue={waybill} showDemoWaybills={false} />
      <Typography variant="h4" component="h1" sx={{ overflowWrap: 'anywhere' }}>
        Waybill {waybill}
      </Typography>
      {state === 'loading' && <TrackingSkeleton waybill={waybill} />}
      {state === 'notFound' && (
        <Alert severity="info">
          <AlertTitle>We couldn&apos;t find that shipment</AlertTitle>
          No shipment matches {waybill}. Check the waybill number on your shipping confirmation and try again.
        </Alert>
      )}
      {state === 'error' && (
        <Alert
          severity="error"
          action={
            <Button color="inherit" onClick={retry}>
              Try again
            </Button>
          }
        >
          <AlertTitle>Something went wrong</AlertTitle>
          We couldn&apos;t load this shipment just now.
        </Alert>
      )}
      {state === 'success' && <ShipmentDetails shipment={data} />}
    </Stack>
  )
}

function ShipmentDetails({ shipment }) {
  const isDesktop = useMediaQuery((theme) => theme.breakpoints.up('md'))

  const cards = (
    <>
      <ShipmentSummary shipment={shipment} />
      <ReceiverCard receiver={shipment.receiver} />
    </>
  )

  return (
    <Stack spacing={3}>
      <Paper variant="outlined" sx={{ p: { xs: 2, md: 3 } }}>
        <DeliveryStepper shipment={shipment} />
      </Paper>
      {isDesktop ? (
        <Box sx={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 3 }}>{cards}</Box>
      ) : (
        // Collapsed by default on phones so the stepper and history come
        // first; the summary line keeps the delivery date in view.
        <Accordion variant="outlined" disableGutters>
          <AccordionSummary expandIcon={<ExpandMoreIcon />}>
            <div>
              <Typography fontWeight={500}>Shipment details</Typography>
              <Typography variant="body2" color="text.secondary">
                Est. delivery {formatDate(shipment.estimatedDelivery)}
              </Typography>
            </div>
          </AccordionSummary>
          <AccordionDetails>
            <Stack spacing={2}>{cards}</Stack>
          </AccordionDetails>
        </Accordion>
      )}
      <TrackingHistory events={shipment.events} />
    </Stack>
  )
}

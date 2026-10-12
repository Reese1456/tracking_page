import Card from '@mui/material/Card'
import CardContent from '@mui/material/CardContent'
import Typography from '@mui/material/Typography'
import DetailList from './DetailList.jsx'

export default function ReceiverCard({ receiver }) {
  return (
    <Card variant="outlined" sx={{ height: '100%' }}>
      <CardContent>
        <Typography variant="h6" component="h2" gutterBottom>
          Receiver
        </Typography>
        <DetailList
          items={[
            { label: 'Name', value: receiver.name },
            { label: 'City', value: receiver.city },
          ]}
        />
      </CardContent>
    </Card>
  )
}

import Box from '@mui/material/Box'
import Button from '@mui/material/Button'
import Chip from '@mui/material/Chip'
import Stack from '@mui/material/Stack'
import TextField from '@mui/material/TextField'
import Typography from '@mui/material/Typography'
import { useState } from 'react'
import { useNavigate } from 'react-router'
import { normalizeWaybill, WAYBILL_PATTERN } from '../constants/waybill.js'
import { useDemoWaybills } from '../hooks/useDemoWaybills.js'

const FORMAT_HINT = 'A waybill is WB followed by 8 digits, for example WB10000004.'

export default function WaybillSearch({ initialValue = '', showDemoWaybills = true }) {
  const navigate = useNavigate()
  const [value, setValue] = useState(initialValue)
  const [showHint, setShowHint] = useState(false)

  // Only a quick check so a typo doesn't cost a round trip. A URL typed by
  // hand still reaches the API, which gives the authoritative 400.
  function handleSubmit(event) {
    event.preventDefault()
    const waybill = normalizeWaybill(value)
    if (!WAYBILL_PATTERN.test(waybill)) {
      setShowHint(true)
      return
    }
    navigate(`/track/${waybill}`)
  }

  function handleChange(event) {
    setValue(event.target.value)
    setShowHint(false)
  }

  return (
    <Stack spacing={2}>
      <Box component="form" role="search" onSubmit={handleSubmit} noValidate>
        <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1} alignItems={{ sm: 'flex-start' }}>
          <TextField
            label="Waybill number"
            placeholder="WB10000004"
            value={value}
            onChange={handleChange}
            error={showHint}
            // A space keeps the hint's line reserved, so the button doesn't
            // jump when the hint appears.
            helperText={showHint ? FORMAT_HINT : ' '}
            fullWidth
            inputProps={{ autoCapitalize: 'characters', autoComplete: 'off', spellCheck: false }}
          />
          <Button type="submit" variant="contained" size="large" sx={{ minHeight: { sm: 56 }, px: 4 }}>
            Track
          </Button>
        </Stack>
      </Box>
      {showDemoWaybills && <DemoWaybillChips onSelect={(waybill) => navigate(`/track/${waybill}`)} />}
    </Stack>
  )
}

// A separate component so the demo list is only fetched where it's shown.
function DemoWaybillChips({ onSelect }) {
  const waybills = useDemoWaybills()

  if (waybills.length === 0) {
    return null
  }

  return (
    <Box>
      <Typography variant="body2" color="text.secondary" gutterBottom>
        Try a demo waybill:
      </Typography>
      <Stack direction="row" flexWrap="wrap" useFlexGap spacing={1}>
        {waybills.map((waybill) => (
          <Chip key={waybill} label={waybill} variant="outlined" onClick={() => onSelect(waybill)} />
        ))}
      </Stack>
    </Box>
  )
}

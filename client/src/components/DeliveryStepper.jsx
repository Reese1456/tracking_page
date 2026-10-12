import CheckCircleIcon from '@mui/icons-material/CheckCircle'
import Step from '@mui/material/Step'
import StepConnector from '@mui/material/StepConnector'
import StepLabel from '@mui/material/StepLabel'
import Stepper from '@mui/material/Stepper'
import Typography from '@mui/material/Typography'
import useMediaQuery from '@mui/material/useMediaQuery'
import { getSteps } from '../constants/steps.js'

// The icon's accessible name, so a screen reader announces each step's state
// along with its label.
const STATE_NAMES = {
  completed: 'Completed',
  current: 'Current step',
  exception: 'Exception',
  pending: 'Pending',
}

// Compares positions with the API's stepIndex; it never derives an index from
// the status.
function getStepState(index, { stepIndex, isException, status }) {
  if (index < stepIndex) {
    return 'completed'
  }
  if (index > stepIndex) {
    return 'pending'
  }
  if (isException) {
    return 'exception'
  }
  return status === 'Delivered' ? 'completed' : 'current'
}

function StepStateIcon({ state, icon: Icon }) {
  const name = STATE_NAMES[state]
  switch (state) {
    case 'completed':
      return <CheckCircleIcon color="success" titleAccess={name} />
    case 'exception':
      return <Icon color="error" titleAccess={name} />
    case 'current':
      return <Icon color="primary" titleAccess={name} />
    default:
      return <Icon color="pending" titleAccess={name} />
  }
}

export default function DeliveryStepper({ shipment }) {
  const isDesktop = useMediaQuery((theme) => theme.breakpoints.up('md'))
  const steps = getSteps(shipment.isException)

  return (
    <Stepper
      activeStep={shipment.stepIndex}
      orientation={isDesktop ? 'horizontal' : 'vertical'}
      alternativeLabel={isDesktop}
      // In vertical mode MUI puts each connector line between the steps, not
      // inside them, so a real <ol> would hold <div>s. ARIA roles allow the
      // decorative lines once they're hidden.
      role="list"
      connector={<StepConnector aria-hidden="true" />}
    >
      {steps.map((step, index) => {
        const state = getStepState(index, shipment)
        return (
          <Step
            key={step.label}
            role="listitem"
            completed={state === 'completed'}
            aria-current={index === shipment.stepIndex ? 'step' : undefined}
          >
            <StepLabel
              error={state === 'exception'}
              icon={<StepStateIcon state={state} icon={step.icon} />}
              optional={
                state === 'exception' && (
                  <Typography variant="body2" color="error">
                    {shipment.exceptionReason}
                  </Typography>
                )
              }
            >
              {step.label}
            </StepLabel>
          </Step>
        )
      })}
    </Stepper>
  )
}

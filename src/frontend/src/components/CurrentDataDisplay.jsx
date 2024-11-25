import * as React from 'react';
import Box from '@mui/joy/Box';
import Typography from '@mui/joy/Typography';
import SvgIcon from '@mui/joy/SvgIcon';
import InfoOutlined from '@mui/icons-material/InfoOutlined';
import MarkedDataSlider from './MarkedDataSlider';
import { Stack, Tooltip } from '@mui/joy';
import ReverseAccordion from './ReverseAccordion';

export default function CurrentDataDisplay(props)
{
    return (
        <Box
          sx={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 1, p: 5, width: "100%", backgroundColor: "white"}}
        >
          <Stack direction="row" gap={5}>
            <Stack direction="column" >
                <Typography sx={{textAlign: "center" }}>Current</Typography>
                <Typography
                    sx={{ fontSize: 'xl4', lineHeight: 1, textAlign: "center"  }}
                >
                    {!props.currentValue ? "N/A" : props.currentValue}
                </Typography>
            </Stack>
            <Stack direction="column" >
                <Typography sx={{textAlign: "center" }}>Target</Typography>
                <Typography
                    sx={{ fontSize: 'xl4', lineHeight: 1, textAlign: "center" }}
                >
                    {props.targetValue}
                </Typography>
            </Stack>
          </Stack>  
          <Typography
            level="body-sm"
            sx={{ alignItems: 'flex-start', maxWidth: 240, wordBreak: 'break-all' }}
          >
            {props.name}
          </Typography>
          
          <ReverseAccordion>
            <MarkedDataSlider currentTarget={props.value} setCurrentTarget={props.setCurrentTarget} min={props.min} max={props.max} step={props.step} marks={props.marks}  />
          </ReverseAccordion>
        </Box>
      );
}
import * as React from 'react';
import Box from '@mui/joy/Box';
import Typography from '@mui/joy/Typography';
import SvgIcon from '@mui/joy/SvgIcon';
import InfoOutlined from '@mui/icons-material/InfoOutlined';
import MarkedDataSlider from './MarkedDataSlider';
import { Stack, Tooltip } from '@mui/joy';

export default function Navbar()
{
    return (
        <Stack sx={{background: "linear-gradient(135deg, #667eea 0%, #764ba2 100%)", mb: 2}}>
            <Typography sx={{textAlign: "center", fontSize: "32px", fontFamily: "'Host Grotesk', sans-serif", color: "white", my: 1}}>
                Group 16
            </Typography>
        </Stack>
    )
}
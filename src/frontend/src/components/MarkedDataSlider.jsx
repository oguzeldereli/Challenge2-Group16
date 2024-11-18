import * as React from 'react';
import Box from '@mui/joy/Box';
import Slider from '@mui/joy/Slider';



export default function MarkedDataSlider({currentTarget, setCurrentTarget, min, max, step, marks}) {
  return (
    <Slider
        defaultValue={currentTarget}
        step={step}
        min={min}
        max={max}
        valueLabelDisplay="auto"
        marks={marks}
        sx={{
          color: '#764ba2', // Custom color for the slider
          '& .MuiSlider-thumb': {
            boxShadow: 'none',
            backgroundColor: '#764ba2', 
          },
          '& .MuiSlider-track': {
            backgroundColor: '#764ba2', 
          },  
          '& .MuiSlider-rail': {
            backgroundColor: '#b58fdb', 
          },
        }}
        onChange={(e) => setCurrentTarget(e.target.value)}
      />
  );
}
import React, { useState } from 'react';
import Box from '@mui/joy/Box';
import Button from '@mui/joy/Button';
import Typography from '@mui/joy/Typography';
import Collapse from '@mui/material/Collapse';

export default function ReverseAccordion({children}) {
  const [open, setOpen] = useState(false);

  const handleToggle = () => {
    setOpen((prev) => !prev);
  };

  return (
    <Box sx={{ width: '100%', textAlign: 'center', height: "70px"}}>
      <Collapse in={open}>
        {children}
      </Collapse>
      <Button size="sm" variant="soft" onClick={handleToggle} sx={{ transition: 'margin 0.3s', color: "#777777", backgroundColor: "#e8e4f0" }}>
        {open ? 'Hide' : 'Set Target'}
      </Button>
    </Box>
  );
}

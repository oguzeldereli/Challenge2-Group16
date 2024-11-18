
import React, { useEffect } from 'react';
import Select from '@mui/joy/Select';
import Option from '@mui/joy/Option';
import { Chip, Stack, Typography, Box, Tooltip } from '@mui/joy';
import IconButton from '@mui/joy/IconButton';
import PlayArrowIcon from '@mui/icons-material/PlayArrow';
import DocumentScannerIcon from '@mui/icons-material/DocumentScanner';
import LogoutIcon from '@mui/icons-material/Logout';
import { useNavigate } from 'react-router-dom';
import { SignOut } from '../common/authUtils';
import { ArrowBack, ArrowLeft } from '@mui/icons-material';

const ClientSelector = ({ selectedDevice, devices, setSelectedDevice, variant }) => {

    const navigate = useNavigate();
    const handleLogButtonClick = () => {
        navigate('/logs'); 
    };

    const handleGoBackButtonClick = () => {
        navigate('/'); 
    };

    const handleSignOutButtonClick = async () => {
        await SignOut();
    };

    return (
        <Stack direction="row" gap={3} alignItems={"center"}>
            <Box sx={{flexGrow: "1", display: "flex", gap: 3, alignItems: "center"}}>
                <Typography sx={{fontSize: "22px", fontFamily: "'Host Grotesk', sans-serif"}}>
                    Device: 
                </Typography>
                <Select 
                slotProps={{
                    listbox: {
                        sx: {
                            flexGrow: '1',
                        },
                    },
                }}
                sx={{ 
                    flexGrow: '1',
                    width: "100%",
                    border: "1px solid #aaaaaa"
                }} 
                placeholder="Select a device" 
                onChange={(e) => setSelectedDevice(e.target.value)}>
                    {devices && devices.map(device => (
                        <Option key={device.id} value={device.id}>{device.id}</Option>
                    ))}
                </Select >
            </Box>
            
            <Box sx={{display: "flex", gap: 3, alignItems: "center"}}>  
                <Chip
                    color="danger"
                    variant="soft"
                    sx={{p: 1, backgroundColor: "#e8cccc"}}>
                    Not Operational
                </Chip>
            </Box>

            {variant == "main" && (
            <Box sx={{display: "flex", gap: 1, alignItems: "center"}}>
                <Tooltip title="Start Device" variant="solid"> 
                    <IconButton disabled={selectedDevice !== null}  variant="soft" sx={{border: "1px solid #5da67d", color: "#5da67d"}}>
                        <PlayArrowIcon />
                    </IconButton>
                </Tooltip>
                <Tooltip title="View Device Logs" variant="solid">
                    <IconButton disabled={selectedDevice !== null} onClick={handleLogButtonClick} variant="soft" sx={{border: "1px solid #4d71a1", color: "#4d71a1"}}>
                        <DocumentScannerIcon />
                    </IconButton>
                </Tooltip>
                <Tooltip title="Sign Out" variant="solid">
                    <IconButton onClick={handleSignOutButtonClick} color="danger" variant="soft" sx={{border: "1px solid #7d1212"}}>
                        <LogoutIcon />
                    </IconButton>
                </Tooltip>
            </Box>
            )}

            {variant == "go-back" && (
            <Box sx={{display: "flex", gap: 1, alignItems: "center"}}>
                <Tooltip title="Go Back" variant="solid"> 
                    <IconButton onClick={handleGoBackButtonClick} variant="soft" sx={{border: "1px solid #4d71a1", color: "#4d71a1"}}>
                        <ArrowBack />
                    </IconButton>
                </Tooltip>
            </Box>
            )}
            
        </Stack>
    );
};

export default ClientSelector;
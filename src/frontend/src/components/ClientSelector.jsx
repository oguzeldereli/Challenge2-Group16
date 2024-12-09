
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
import { ArrowBack, ArrowLeft, Stop } from '@mui/icons-material';
import { PauseDevice, StartDevice } from '../common/apiUtils';

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

    const handlePauseStartClick = async () => {
        if(selectedDevice)
        {
            if(selectedDevice.status === 1)
            {
                await PauseDevice(selectedDevice.deviceId);
                return;
            }   
            else if(selectedDevice.status === 2)
            {
                await StartDevice(selectedDevice.deviceId);
                return;
            }
        }
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
                value={selectedDevice ? (selectedDevice.deviceId || '') : ""}
                onChange={(e, value) => {
                    setSelectedDevice(value);
                    }}>
                    {devices && typeof devices !== "undefined" && devices.length > 0 && devices.map(device => (
                        <Option key={device.deviceId} value={device.deviceId}>{device.deviceId}</Option>
                    ))}
                </Select >
            </Box>
            
            <Box sx={{display: "flex", gap: 3, alignItems: "center"}}>  
                <Chip
                    color="danger"
                    variant="soft"
                    sx={{p: 1, backgroundColor: "#e8cccc"}}>
                    <span>
                        {!selectedDevice && "No Device Selected"}
                        {selectedDevice && selectedDevice.status === 0 && "Not Ready"}
                        {selectedDevice && selectedDevice.status === 1 && "Operational"}
                        {selectedDevice && selectedDevice.status === 2 && "Paused"}
                    </span>
                </Chip>
            </Box>

            {variant == "main" && (
            <Box sx={{display: "flex", gap: 1, alignItems: "center"}}>
                <Tooltip title={selectedDevice && selectedDevice.status === 1 ? "Pause" : "Continue"} variant="solid"> 
                    <IconButton onClick={handlePauseStartClick} disabled={!selectedDevice || selectedDevice.status === 0}  variant="soft" sx={{border: `1px solid ${selectedDevice && selectedDevice.status === 1 ? "#7d1212" : "#5da67d"}`}} color={selectedDevice && selectedDevice.status === 1 ? "danger" : "success"}>
                        {selectedDevice && selectedDevice.status === 1 ? <Stop /> : <PlayArrowIcon />}
                    </IconButton>
                </Tooltip>
                <Tooltip title="View Device Logs" variant="solid">
                    <IconButton disabled={!selectedDevice} onClick={handleLogButtonClick} variant="soft" sx={{border: "1px solid #4d71a1", color: "#4d71a1"}}>
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
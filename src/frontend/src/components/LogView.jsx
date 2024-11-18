import { Box, Button, IconButton, Tooltip, Typography } from "@mui/joy";
import { Input, Stack } from "@mui/joy";
import { ArrowRightIcon, DateTimePicker } from "@mui/x-date-pickers";


export default function LogView(props)
{
    const colorMap = {
        "Error": "danger",
        "Warning": "warnings",
        "Information": "primary",
        "Success": "success"
    };
    
    return (
        <Stack sx={{width: "100%", mt: 3}}>
            <Stack direction="row" gap={2} sx={{mb: 3}} alignItems={"center"}>
                <Typography sx={{fontSize: "22px", fontFamily: "'Host Grotesk', sans-serif"}}>
                    View Logs from
                </Typography>
                <Box>
                    <DateTimePicker label="Start date/time" slotProps={{ textField: { size: 'small'} }} />
                </Box>
                <Typography sx={{fontSize: "22px", fontFamily: "'Host Grotesk', sans-serif"}}>
                    To
                </Typography>
                <Box>
                    <DateTimePicker label="End date/time" slotProps={{ textField: { size: 'small' } }} />
                </Box>
            </Stack>
            <Box sx={{
                minHeight: 500,    
                overflowY: 'auto',
                overflowX: 'none', 
                overflowWrap: "break-word",
                border: '1px solid #764ba2',
                padding: 3,
                flexGrow: "1"
            }}>
                {props.logs.map((log, index) => {
                    let color = colorMap[log.type];
                    return (
                        <Typography key={log.time + log.type + index}>
                            <Typography fontFamily={"'Geist Mono', monospace"} color="black" level="body-sm">[{log.time}]</Typography>
                            <Typography fontFamily={"'Geist Mono', monospace"} color={color} level="body-sm">[{log.type}] : </Typography>
                            <Typography fontFamily={"'Geist Mono', monospace"} color="black" level="body-sm">{log.message}</Typography>
                        </Typography>
                    )
                })}
            </Box>
            <Stack direction="row">
                <Input variant="outlined" sx={{flexGrow: "1", borderColor: "#764ba2", borderRadius: "0", borderBottomLeftRadius: "5px", fontFamily: "'Geist Mono', monospace"}} placeholder="Type command..."/>
                <Tooltip title="Execute" variant="solid"> 
                    <IconButton variant="solid" sx={{border: "1px solid #5da67d", color: "white", backgroundColor: "#5da67d", borderRadius: "0", borderBottomRightRadius: "5px"}}>
                        <ArrowRightIcon />
                    </IconButton>
                </Tooltip>  
            </Stack>
        </Stack>
    )
}
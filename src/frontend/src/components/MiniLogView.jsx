import { Box, Button, IconButton, Tooltip, Typography } from "@mui/joy";
import { Input, Stack } from "@mui/joy";
import { ArrowRightIcon } from "@mui/x-date-pickers";
import { format } from "date-fns";
import { useEffect } from "react";


export default function MiniLogView(props)
{
    const colorMap = {
        "Error": "danger",
        "Warning": "warnings",
        "Information": "primary",
        "Success": "success"
    };
    
    return (
        <Stack sx={{width: "40%",}}>
            <Typography fontSize={22} fontFamily={"'Host Grotesk', sans-serif"}>Recent Logs</Typography>
            <Box sx={{
                maxHeight: 300,    
                overflowY: 'auto',
                overflowX: 'none', 
                overflowWrap: "break-word",
                border: '1px solid #764ba2',
                padding: 3,
                flexGrow: "1"
            }}>
                {props.logs.map((log, index) => {
                    let color = colorMap[log.type];
                    if(index <= props.logs.length - 4)
                    {
                        return;
                    }
                    return (
                        <Typography key={log.timeStamp.toString() + log.type + index}>
                            <Typography fontFamily={"'Geist Mono', monospace"} color="black" level="body-sm">[{format(log.timeStamp * 1000, "pp")}]</Typography>
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
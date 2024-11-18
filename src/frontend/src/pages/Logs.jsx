import { Stack } from "@mui/joy";
import Navbar from "../components/Navbar";
import LogView from "../components/LogView";
import ClientSelector from "../components/ClientSelector";



export default function Logs(props)
{
    return (
        <>
            <Navbar />
            <Stack direction="column" sx={{maxWidth: "1200px", margin: "auto", px: 2}}>
                <ClientSelector variant="go-back" selectedDevice={props.selectedDevice} devices={props.devices} setSelectedDevice={props.setSelectedDevice} />
                <LogView logs={props.logs} />
            </Stack>
        </>
    )
}
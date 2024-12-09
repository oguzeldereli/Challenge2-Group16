import { Box, Stack, Typography } from "@mui/joy"

import ClientSelector from "../components/ClientSelector"
import CurrentDataPanel from "../components/CurrentDataPanel"
import Navbar from "../components/Navbar"
import CurrentDataTabs from "../components/CurrentDataTabs"
import MiniLogView from "../components/MiniLogView"
import PastDataTabs from "../components/PastDataTabs"
import { getRegisteredAndConnectedDevices } from "../common/apiUtils"
import { useEffect } from "react"
import { startSSEConnection } from "../common/eventHandler"

export default function Dashboard(props)
{

    useEffect(() => {

        async function setAppStart() 
        { 
          props.setDevices(await getRegisteredAndConnectedDevices());
          await startSSEConnection(props.handleDataPacket, props.handleErrorPacket, props.handleDevicePacket);
        }
  
        setAppStart();
    }, []);

    return (
        <>
            <Navbar />
            <Stack direction="column" sx={{maxWidth: "1200px", margin: "auto", px: 2}}>
                <ClientSelector variant="main" selectedDevice={props.selectedDevice} devices={props.devices} setSelectedDevice={props.setSelectedDevice} />
                <CurrentDataPanel selectedDevice={props.selectedDevice} tempdata={props.tempdata} phdata={props.phdata} rpmdata={props.rpmdata}/>
                <Stack direction="row" gap={3}>
                    <CurrentDataTabs selectedDevice={props.selectedDevice} tempdata={props.tempdata} phdata={props.phdata} rpmdata={props.rpmdata} />
                    <MiniLogView logs={props.logs}/>
                </Stack>
                <PastDataTabs selectedDevice={props.selectedDevice}/>
            </Stack>
        </>
    )
}
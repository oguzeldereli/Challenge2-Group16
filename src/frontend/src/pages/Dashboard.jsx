import { Box, Stack, Typography } from "@mui/joy"

import ClientSelector from "../components/ClientSelector"
import CurrentDataPanel from "../components/CurrentDataPanel"
import Navbar from "../components/Navbar"
import CurrentDataTabs from "../components/CurrentDataTabs"
import MiniLogView from "../components/MiniLogView"
import PastDataTabs from "../components/PastDataTabs"
import { getRegisteredAndConnectedDevices } from "../common/apiUtils"
import { useEffect } from "react"

export default function Dashboard(props)
{
    return (
        <>
            <Navbar />
            <Stack direction="column" sx={{maxWidth: "1200px", margin: "auto", px: 2}}>
                <ClientSelector variant="main" selectedDevice={props.selectedDevice} devices={props.devices} setSelectedDevice={props.setSelectedDevice} />
                <CurrentDataPanel data={props} />
                <Stack direction="row" gap={3}>
                    <CurrentDataTabs />
                    <MiniLogView logs={props.logs}/>
                </Stack>
                <PastDataTabs />
            </Stack>
        </>
    )
}
import { Stack } from "@mui/joy";
import CurrentDataDisplay from "./CurrentDataDisplay";
import { useState } from "react";



export default function CurrentDataPanel(props)
{
    const [currentTargetTemp, setCurrentTargetTemp] = useState(0)
    const [currentTargetPH, setCurrentTargetPH] = useState(0)
    const [currentTargetRPM, setCurrentTargetRPM] = useState(0)
    return (
        <Stack direction="row" sx={{justifyContent: "between", width: "100%", backgroundColor: "white"}} gap={1}>
            <CurrentDataDisplay currentValue={25} targetValue={currentTargetTemp.toFixed(1)} setCurrentTarget={setCurrentTargetTemp} name="Temperature (°C)" min={25} max={35} step={0.1} marks={[{value: 25, label: "25°C"}, {value: 35, label: "35°C"}]}/>
            <CurrentDataDisplay currentValue={8} targetValue={currentTargetPH.toFixed(1)} setCurrentTarget={setCurrentTargetPH} name="pH" min={3} max={7} step={0.1} marks={[{value: 3, label: "3"}, {value: 7, label: "7"}]} />
            <CurrentDataDisplay currentValue={956} targetValue={currentTargetRPM} setCurrentTarget={setCurrentTargetRPM} name="Stirring RPM" min={500} max={1500} step={10} marks={[{value: 500, label: "500"}, {value: 1500, label: "1500"}]} />
        </Stack>
    )
}
import { Stack } from "@mui/joy";
import CurrentDataDisplay from "./CurrentDataDisplay";
import { useCallback, useEffect, useRef, useState } from "react";
import { setDeviceTarget } from "../common/apiUtils";



export default function CurrentDataPanel(props)
{
    

    const [currentTargetTemp, setCurrentTargetTemp] = useState(0)
    const [currentTargetPH, setCurrentTargetPH] = useState(0)
    const [currentTargetRPM, setCurrentTargetRPM] = useState(0)
    const [currentTemp, setCurrentTemp] = useState(0)
    const [currentPH, setCurrentPH] = useState(0)
    const [currentRPM, setCurrentRPM] = useState(0)

    let debounceTimerTemp = useRef(null);
    let debounceTimerPH = useRef(null);
    let debounceTimerRPM = useRef(null);

    const selectedDeviceRef = useRef(props.selectedDevice);

    useEffect(() => {
        selectedDeviceRef.current = props.selectedDevice;
        if(selectedDeviceRef.current)
        {
            console.log(selectedDeviceRef.current);
            setCurrentTargetTemp(props.selectedDevice.tempTarget);
            setCurrentTargetPH(props.selectedDevice.phTarget);
            setCurrentTargetRPM(props.selectedDevice.rpmTarget);
        }
        else
        {
            setCurrentTargetTemp(0);
            setCurrentTargetPH(0);
            setCurrentTargetRPM(0);
        }
    }, [props.selectedDevice]);

    async function onTempChange(newValue)
    {
        if(selectedDeviceRef.current)
        {
            const response = await setDeviceTarget(selectedDeviceRef.current.deviceId, 0, newValue);
            if(response.success === true)
            {
                setCurrentTargetTemp(newValue);
            }
            else
            {
                console.log("device couldnt set target");
            }
        }
        else
        {
            console.log("no selected device");
        }
    }

    async function onPHChange(newValue)
    {   
        if(selectedDeviceRef.current)
        {
            const response = await setDeviceTarget(selectedDeviceRef.current.deviceId, 1, newValue);
            if(response.success === true)
            {
                setCurrentTargetPH(newValue);
            }
            else
            {
                console.log("device couldnt set target");
            }
        }
        else
        {
            console.log("no selected device");
        }
    }

    async function onRpmChange(newValue)
    {
        if(selectedDeviceRef.current)
        {
            const response = await setDeviceTarget(selectedDeviceRef.current.deviceId, 2, newValue);
            if(response.success === true)
            {
                setCurrentTargetRPM(newValue);
            }
            else
            {
                console.log("device couldnt set target");
            }
        }
        else
        {
            console.log("no selected device");
        }
    }

    const debouncedHandleTempChange = useCallback((value) => {
        if (debounceTimerTemp.current) {
            clearTimeout(debounceTimerTemp.current);
        }

        // Set a new timer
        debounceTimerTemp.current = setTimeout(async () => {
            await onTempChange(value);
        }, 500); // 500ms delay
    }, []); 

    const debouncedHandlePHChange = useCallback((value) => {
        if (debounceTimerPH.current) {
            clearTimeout(debounceTimerPH.current);
        }

        // Set a new timer
        debounceTimerPH.current = setTimeout(async () => {
            await onPHChange(value);
        }, 500); // 500ms delay
    }, []); 

    const debouncedHandleRPMChange = useCallback((value) => {
        if (debounceTimerRPM.current) {
            clearTimeout(debounceTimerRPM.current);
        }

        // Set a new timer
        debounceTimerRPM.current = setTimeout(async () => {
            await onRpmChange(value);
        }, 500); // 500ms delay
    }, []); 

    useEffect(() => {
        return () => {
            if (debounceTimerTemp.current) {
                clearTimeout(debounceTimerTemp.current);
            }
            if (debounceTimerPH.current) {
                clearTimeout(debounceTimerPH.current);
            }
            if (debounceTimerRPM.current) {
                clearTimeout(debounceTimerRPM.current);
            }
        };
    }, []);

    useEffect(() => {
        if(props.selectedDevice)
        {
            const device_temp_data = props.tempdata.filter(x => x.client_id === props.selectedDevice.deviceId);
            const last_temp_data = device_temp_data.length > 0 ? device_temp_data[device_temp_data.length - 1].data.data : undefined;
            setCurrentTemp(last_temp_data);
        }
      }, [props.tempdata, props.selectedDevice]);

      useEffect(() => {
        if(props.selectedDevice)
        {
            const device_ph_data = props.phdata.filter(x => x.client_id === props.selectedDevice.deviceId);
            const last_ph_data = device_ph_data.length > 0 ? device_ph_data[device_ph_data.length - 1].data.data : undefined;
            setCurrentPH(last_ph_data);
        }
      }, [props.phdata, props.selectedDevice]);

      useEffect(() => {
        if(props.selectedDevice)
        {
            const device_rpm_data = props.rpmdata.filter(x => x.client_id === props.selectedDevice.deviceId);
            const last_rpm_data = device_rpm_data.length > 0 ? device_rpm_data[device_rpm_data.length - 1].data.data : undefined;
            setCurrentRPM(last_rpm_data);
        }
      }, [props.rpmdata, props.selectedDevice]);

    return (
        <Stack direction="row" sx={{justifyContent: "between", width: "100%", backgroundColor: "white"}} gap={1}>
            <CurrentDataDisplay currentValue={currentTemp} targetValue={currentTargetTemp} onTargetChange={(value) => debouncedHandleTempChange(value)} name="Temperature (°C)" min={25} max={35} step={0.1} marks={[{value: 25, label: "25°C"}, {value: 35, label: "35°C"}]}/>
            <CurrentDataDisplay currentValue={currentPH} targetValue={currentTargetPH} onTargetChange={(value) => debouncedHandlePHChange(value)} name="pH" min={3} max={7} step={0.1} marks={[{value: 3, label: "3"}, {value: 7, label: "7"}]} />
            <CurrentDataDisplay currentValue={currentRPM} targetValue={currentTargetRPM} onTargetChange={(value) => debouncedHandleRPMChange(value)} name="Stirring RPM" min={500} max={1300} step={10} marks={[{value: 500, label: "500"}, {value: 1300, label: "1300"}]} />
        </Stack>
    )
}
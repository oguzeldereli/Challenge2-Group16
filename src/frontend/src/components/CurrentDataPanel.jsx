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
        if(props.selectedDevice)
        {
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

    function filterAndSortData(dataList, dataType) {
        const filteredData = dataList.filter(item => item.data && item.data.data_type === dataType);
    
        const sortedData = filteredData.sort((a, b) => {
            const timeA = new Date(a.data.time_stamp).getTime();
            const timeB = new Date(b.data.time_stamp).getTime();
            return timeA - timeB; 
        });
    
        return sortedData;
    }   

    useEffect(() => {
        
        const temp_data = filterAndSortData(props.data, "temperature");
        const ph_data = filterAndSortData(props.data, "ph");
        const rpm_data = filterAndSortData(props.data, "rpm");
        
        const last_temp_data = temp_data.length > 0 ? temp_data[temp_data.length - 1] : undefined;
        const last_ph_data = ph_data.length > 0 ? ph_data[ph_data.length - 1] : undefined;
        const last_rpm_data = rpm_data.length > 0 ? rpm_data[rpm_data.length - 1] : undefined;

        setCurrentTemp(last_temp_data);
        setCurrentPH(last_ph_data);
        setCurrentRPM(last_rpm_data);

      }, [props.data]);

    return (
        <Stack direction="row" sx={{justifyContent: "between", width: "100%", backgroundColor: "white"}} gap={1}>
            <CurrentDataDisplay currentValue={currentTemp} targetValue={currentTargetTemp} onTargetChange={(value) => debouncedHandleTempChange(value)} name="Temperature (°C)" min={25} max={35} step={0.1} marks={[{value: 25, label: "25°C"}, {value: 35, label: "35°C"}]}/>
            <CurrentDataDisplay currentValue={currentPH} targetValue={currentTargetPH} onTargetChange={(value) => debouncedHandlePHChange(value)} name="pH" min={3} max={7} step={0.1} marks={[{value: 3, label: "3"}, {value: 7, label: "7"}]} />
            <CurrentDataDisplay currentValue={currentRPM} targetValue={currentTargetRPM} onTargetChange={(value) => debouncedHandleRPMChange(value)} name="Stirring RPM" min={500} max={1300} step={10} marks={[{value: 500, label: "500"}, {value: 1300, label: "1300"}]} />
        </Stack>
    )
}
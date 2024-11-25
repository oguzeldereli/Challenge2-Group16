import { Stack } from "@mui/joy";
import CurrentDataDisplay from "./CurrentDataDisplay";
import { useEffect, useState } from "react";



export default function CurrentDataPanel(props)
{
    const [currentTargetTemp, setCurrentTargetTemp] = useState(0)
    const [currentTargetPH, setCurrentTargetPH] = useState(0)
    const [currentTargetRPM, setCurrentTargetRPM] = useState(0)
    const [currentTemp, setCurrentTemp] = useState(0)
    const [currentPH, setCurrentPH] = useState(0)
    const [currentRPM, setCurrentRPM] = useState(0)

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
        
        const temp_data = filterAndSortData(props.data.data, "temperature");
        const ph_data = filterAndSortData(props.data.data, "ph");
        const rpm_data = filterAndSortData(props.data.data, "rpm");

        const last_temp_data = temp_data.length > 0 ? temp_data[temp_data.length - 1] : undefined;
        const last_ph_data = ph_data.length > 0 ? ph_data[ph_data.length - 1] : undefined;
        const last_rpm_data = rpm_data.length > 0 ? rpm_data[rpm_data.length - 1] : undefined;

        setCurrentTemp(last_temp_data);
        setCurrentPH(last_ph_data);
        setCurrentRPM(last_rpm_data);

      }, [props.data]);
    


    return (
        <Stack direction="row" sx={{justifyContent: "between", width: "100%", backgroundColor: "white"}} gap={1}>
            <CurrentDataDisplay currentValue={currentTemp} targetValue={currentTargetTemp.toFixed(1)} setCurrentTarget={setCurrentTargetTemp} name="Temperature (°C)" min={25} max={35} step={0.1} marks={[{value: 25, label: "25°C"}, {value: 35, label: "35°C"}]}/>
            <CurrentDataDisplay currentValue={currentPH} targetValue={currentTargetPH.toFixed(1)} setCurrentTarget={setCurrentTargetPH} name="pH" min={3} max={7} step={0.1} marks={[{value: 3, label: "3"}, {value: 7, label: "7"}]} />
            <CurrentDataDisplay currentValue={currentRPM} targetValue={currentTargetRPM} setCurrentTarget={setCurrentTargetRPM} name="Stirring RPM" min={500} max={1300} step={10} marks={[{value: 500, label: "500"}, {value: 1300, label: "1300"}]} />
        </Stack>
    )
}
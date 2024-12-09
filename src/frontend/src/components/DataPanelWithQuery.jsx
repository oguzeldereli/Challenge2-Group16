import { Typography, Stack, Select, Option, Input, Box, Button } from "@mui/joy"
import { BarChart, BarPlot, ChartsXAxis, ChartsYAxis, LineChart, LinePlot, ResponsiveChartContainer } from "@mui/x-charts"
import { DatePicker, DateTimeField, DateTimePicker, DesktopDatePicker } from "@mui/x-date-pickers"
import { useEffect, useState } from "react";
import { parseISO, format, getUnixTime } from 'date-fns';
import { GetData } from "../common/apiUtils";


export default function DataPanelWithQuery(props)
{
    
    const [selectedDate1, setSelectedDate1] = useState(null);
    const [selectedDate2, setSelectedDate2] = useState(null);
    const [chartData, setChartData] = useState({ xData: [], yData: [] });

    useEffect(() => {
        setChartData([]);
    }, [props.selectedDevice]);

    const insertNullsForGaps = (data, gapThreshold = 300) => {
        if (!data || data.length === 0) return { xData: [], yData: [] };
      
        // Sort data by time in ascending order
        const sortedData = [...data].sort((a, b) => a.time - b.time);
      
        const xData = [];
        const yData = [];
      
        for (let i = 0; i < sortedData.length; i++) {
          const current = sortedData[i];
      
          if (i > 0) {
            const previous = sortedData[i - 1];
            const gap = current.time - previous.time;
      
            if (gap > gapThreshold) {
              // Insert a dummy x-value slightly after the previous time
              const dummyTime = previous.time + 1; // 1 second after the previous time
              xData.push(dummyTime);
              yData.push(null); // Insert null to break the line
            }
          }
      
          xData.push(current.time);
          yData.push(current.value);
        }
      
        return { xData, yData };
      };

    async function handleButtonClick(e)
    {
        if(props.selectedDevice)
        {
            const response = await GetData(props.selectedDevice.deviceId, props.dataType, getUnixTime(selectedDate1), getUnixTime(selectedDate2));
            console.log(response);
            const mappedData = response.map((item) => ({
                time: item.time_stamp,
                value: item.value
            }));

            const merged = insertNullsForGaps(mappedData, 300);
            setChartData(merged);
        }
    }

    const handleDate1Change = (newValue) => {
        if (newValue) {
            console.log(newValue);
            setSelectedDate1(newValue);
        } else {
            setSelectedDate1(null);
        }
    };

    const handleDate2Change = (newValue) => {
        if (newValue) {
            setSelectedDate2(newValue);
        } else {
            setSelectedDate2(null);
        }
    };

    return (    
        <Stack direction="column" gap={2} sx={{width: "100%"}}>
            <Stack direction="row" gap={2} alignItems={"center"}>
                <Typography sx={{fontSize: "22px", fontFamily: "'Host Grotesk', sans-serif"}}>
                    From
                </Typography>
                <Box>
                    <DateTimePicker value={selectedDate1} onChange={handleDate1Change} label="Start date/time" slotProps={{ textField: { size: 'small'} }} />
                </Box>
                <Typography sx={{fontSize: "22px", fontFamily: "'Host Grotesk', sans-serif"}}>
                    To
                </Typography>
                <Box>
                    <DateTimePicker value={selectedDate2} onChange={handleDate2Change} label="End date/time" slotProps={{ textField: { size: 'small' } }} />
                </Box>
                <Button onClick={handleButtonClick} sx={{backgroundColor: "#764ba2"}}>
                    Query
                </Button>
            </Stack>
            <LineChart
            xAxis={[
                { 
                    data: chartData.xData ? chartData.xData : [],
                    valueFormatter: (value) =>
                        format(new Date(value * 1000), "PPpp"),
                
            }]}
            series={[
                {
                    data: chartData.yData ? chartData.yData : [],
                    color: "#764ba2",
                    showMark: false,
                    connectNulls: false, 
                },
            ]}
            height={300}
            >

            </LineChart>
        </Stack>
    )
}


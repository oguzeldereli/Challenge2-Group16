import { Typography, Stack, Select, Option, Input, Box, Button } from "@mui/joy"
import { BarChart, BarPlot, ChartsXAxis, ChartsYAxis, LineChart, LinePlot, ResponsiveChartContainer } from "@mui/x-charts"
import { DatePicker, DateTimeField, DateTimePicker, DesktopDatePicker } from "@mui/x-date-pickers"
import { useEffect, useState } from "react";
import { parseISO, format } from 'date-fns';


export default function DataPanel(props)
{
    
    const [timeframe, setTimeframe] = useState("minute");
    const [chartData, setChartData] = useState([]);
  
    const timeframes = {
        "minute": 1,
        "five-minutes": 5,
        "thirty-minutes": 30,
        "hour": 60,
        "five-hours": 300,
        "day": 1440,
      };

    const filterDataByTimeframe = (rawData, timeframeMinutes) => {
        const now = Date.now();
        const cutoffTime = now - timeframeMinutes * 60000; // Convert minutes to milliseconds
        
        return rawData.filter(
          (item) => item.data.time_stamp * 1000 >= cutoffTime // Convert Unix seconds to milliseconds
        );
      };

    useEffect(() => {
    if (props.data) {
      const filteredData = filterDataByTimeframe(
        props.data,
        timeframes[timeframe]
      );

      const mappedData = filteredData.map((item) => ({
        time: item.data.time_stamp,
        value: item.data.data,
      }));

      setChartData(mappedData);
    }
  }, [timeframe, props.data]);

    const handleTimeframeChange = (event, value) => {
        setTimeframe(value);
    };

    return (
        <Stack direction="column" gap={2} sx={{width: "100%"}}>
            <Stack direction="row" gap={2} alignItems={"center"}>
                    <Typography sx={{fontSize: "22px", fontFamily: "'Host Grotesk', sans-serif"}}>
                        Last
                    </Typography>
                    <Select onChange={handleTimeframeChange} value={timeframe} placeholder="time frame" sx={{fontSize: "20px", fontFamily: "'Host Grotesk', sans-serif"}}>
                        <Option key="minute" value="minute">minute</Option>
                        <Option key="five-minutes" value="five-minutes">5 minutes</Option>
                        <Option key="thirty-minutes" value="thirty-minutes">30 minutes</Option>
                        <Option key="hour" value="hour">hour</Option>
                        <Option key="five-hours" value="five-hours">5 hours</Option>
                        <Option key="day" value="day">day</Option>
                    </Select>       
                </Stack>
                
                <LineChart
                xAxis={[
                    { 
                        data: chartData.map((item) => item.time) ,
                        valueFormatter: (value) =>
                            format(new Date(value * 1000), "pp"), // Format Unix time to human-readable time
                    
                }]}
                series={[
                    {
                    data: chartData.map((item) => item.value),
                    color: "#764ba2",
                    showMark: false,
                    },
                ]}
                height={300}
                >

                </LineChart>
        </Stack>
    )
}
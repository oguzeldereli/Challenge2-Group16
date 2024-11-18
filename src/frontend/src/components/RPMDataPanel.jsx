import { Typography, Stack, Select, Option, Input, Box } from "@mui/joy"
import { BarChart, BarPlot, ChartsXAxis, ChartsYAxis, LineChart, LinePlot, ResponsiveChartContainer } from "@mui/x-charts"
import { DatePicker, DateTimeField, DateTimePicker, DesktopDatePicker } from "@mui/x-date-pickers"

export default function RPMDataPanel(props)
{
    return (
        <Stack direction="column" gap={2} sx={{width: "100%"}}>
                {props.timeSetting == "current" && (
                    <Stack direction="row" gap={2} alignItems={"center"}>
                        <Typography sx={{fontSize: "22px", fontFamily: "'Host Grotesk', sans-serif"}}>
                            Last
                        </Typography>
                        <Select defaultValue="minute" placeholder="time frame" sx={{fontSize: "20px", fontFamily: "'Host Grotesk', sans-serif"}}>
                            <Option key="minute" value="minute">minute</Option>
                            <Option key="five-minutes" value="five-minutes">5 minutes</Option>
                            <Option key="thirty-minutes" value="thirty-minutes">30 minutes</Option>
                            <Option key="hour" value="hour">hour</Option>
                            <Option key="five-hours" value="five-hours">5 hours</Option>
                            <Option key="day" value="day">day</Option>
                        </Select>
                    </Stack>
                )}
                
                {props.timeSetting == "interval" && (
                    <Stack direction="row" gap={2} alignItems={"center"}>
                        <Typography sx={{fontSize: "22px", fontFamily: "'Host Grotesk', sans-serif"}}>
                            From
                        </Typography>
                        <Box>
                            <DateTimePicker label="Start date/time" slotProps={{ textField: { size: 'small'} }} />
                        </Box>
                        <Typography sx={{fontSize: "22px", fontFamily: "'Host Grotesk', sans-serif"}}>
                            To
                        </Typography>
                        <Box>
                            <DateTimePicker label="End date/time" slotProps={{ textField: { size: 'small' } }} />
                        </Box>
                    </Stack>
                )}
                <LineChart
                    xAxis={[{ data: [1, 2, 3, 5, 8, 10] }]}
                    series={[
                        {
                        data: [3, 5.5, 2, 0.5, 3.5, 5],
                        color: "#764ba2"
                        },
                    ]}
                    height={300}
                    />
        </Stack>
    )
}
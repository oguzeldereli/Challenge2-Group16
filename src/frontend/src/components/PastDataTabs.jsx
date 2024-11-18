import * as React from 'react';
import Tabs from '@mui/joy/Tabs';
import TabList from '@mui/joy/TabList';
import Tab, { tabClasses } from '@mui/joy/Tab';
import { styled } from '@mui/material/styles';
import { TabPanel } from '@mui/joy';
import { GaugeContainer, GaugeReferenceArc, GaugeValueArc } from '@mui/x-charts';
import TemperatureDataPanel from './TemperatureDataPanel';
import PHDataPanel from './PHDataPanel';
import RPMDataPanel from './RPMDataPanel';
import { Typography } from '@mui/material';

const CustomTab = styled(Tab)(({ theme }) => ({
    border: "1px solid #764ba2",
    backgroundColor: 'white',
    color: "white",
    flex: 'initial',
    '&[aria-selected="true"]': {
      backgroundColor: '#764ba2'
    },
    '&[aria-selected="false"]': {
      color: 'black'
    },
  }));

function PastDataTabs() {
  return (
    <>
    <Typography fontSize={22} fontFamily={"'Host Grotesk', sans-serif"}>
        Past Data
    </Typography>
    <Tabs aria-label="tabs" defaultValue={0} sx={{ bgcolor: 'transparent', flexGrow: "1" }}>
        <TabList
        disableUnderline
        sx={{
            justifyContent: 'start',
            py: 0.1,
            borderRadius: 'md',
            bgcolor: 'white'
        }}
      >
            <CustomTab disableIndicator sx={{width: "150px"}}>
                Temperature
            </CustomTab>
            <CustomTab disableIndicator sx={{width: "150px"}}>
                pH
            </CustomTab>
            <CustomTab disableIndicator sx={{width: "150px"}}>
                Stirring RPM
            </CustomTab>
        </TabList>
        <TabPanel value={0}>
            <TemperatureDataPanel timeSetting="interval"/>
        </TabPanel>
        <TabPanel value={1}>
           <PHDataPanel timeSetting="interval"/>
        </TabPanel>
        <TabPanel value={2}>
            <RPMDataPanel timeSetting="interval"/>
        </TabPanel>
    </Tabs>
    </>
  );
}

export default PastDataTabs;
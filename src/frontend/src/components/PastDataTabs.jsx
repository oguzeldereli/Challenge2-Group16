import * as React from 'react';
import Tabs from '@mui/joy/Tabs';
import TabList from '@mui/joy/TabList';
import Tab, { tabClasses } from '@mui/joy/Tab';
import { styled } from '@mui/material/styles';
import { TabPanel } from '@mui/joy';
import { GaugeContainer, GaugeReferenceArc, GaugeValueArc } from '@mui/x-charts';
import DataPanel from './DataPanel';
import { Typography } from '@mui/material';
import DataPanelWithQuery from './DataPanelWithQuery';
import { PropaneSharp } from '@mui/icons-material';

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

function PastDataTabs({selectedDevice}) {
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
            <DataPanelWithQuery dataType="temp" selectedDevice={selectedDevice}/>
        </TabPanel>
        <TabPanel value={1}>
            <DataPanelWithQuery dataType="ph" selectedDevice={selectedDevice}/>
        </TabPanel>
        <TabPanel value={2}>
            <DataPanelWithQuery dataType="rpm" selectedDevice={selectedDevice}/>
        </TabPanel>
    </Tabs>
    </>
  );
}

export default PastDataTabs;
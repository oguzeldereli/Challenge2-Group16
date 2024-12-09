import * as React from 'react';
import Tabs from '@mui/joy/Tabs';
import TabList from '@mui/joy/TabList';
import Tab, { tabClasses } from '@mui/joy/Tab';
import { styled } from '@mui/material/styles';
import { TabPanel } from '@mui/joy';
import { GaugeContainer, GaugeReferenceArc, GaugeValueArc } from '@mui/x-charts';
import DataPanel from './DataPanel';
import { Stack, Typography } from '@mui/material';

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

function CurrentDataTabs({selectedDevice, tempdata, phdata, rpmdata}) {

  return (
    <Stack sx={{flexGrow:"1"}}>
      <Typography fontSize={22} fontFamily={"'Host Grotesk', sans-serif"}>
        Recent Data
      </Typography>
      <Tabs aria-label="tabs" defaultValue={0} sx={{ bgcolor: 'transparent', flexGrow: "1", mb: 5 }}>
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
              <DataPanel data={selectedDevice && tempdata && tempdata.filter(x => x.client_id === selectedDevice.deviceId)}  />
          </TabPanel>
          <TabPanel value={1}>
            <DataPanel data={selectedDevice && phdata && phdata.filter(x => x.client_id === selectedDevice.deviceId)} />
          </TabPanel>
          <TabPanel value={2}>
              <DataPanel data={selectedDevice && rpmdata && rpmdata.filter(x => x.client_id === selectedDevice.deviceId)} />
          </TabPanel>
      </Tabs>
    </Stack>
  );
}

export default CurrentDataTabs;
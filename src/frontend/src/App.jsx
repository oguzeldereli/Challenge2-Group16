import { useEffect, useState } from 'react'
import './App.css'
import {
  BrowserRouter as Router,
  Route,
  Link,
  Routes,
  useAsyncError
} from "react-router-dom";
import Dashboard from "./pages/Dashboard"
import Notfound from "./pages/NotFound"
import ProtectedRoute from './components/ProtectedRoute';
import Callback from './pages/callback';
import { LocalizationProvider } from '@mui/x-date-pickers';
import { AdapterDayjs } from '@mui/x-date-pickers/AdapterDayjs';
import Logs from './pages/logs';
import { getDeviceStatus, getRegisteredAndConnectedDevices } from './common/apiUtils';
import { DataProvider } from './components/DataProvider';
import { startSSEConnection } from './common/eventHandler';


function App() {
  const [devices, setDevices] =  useState([]);
  const [errors, setErrors] =  useState([]);
  const [data, setData] =  useState([]);
  const [selectedDevice, setSelectedDevice] =  useState(null);
  const [logs, setLogs] = useState([]);

  const handleErrorPacket = async (event) => {
    const errorData = JSON.parse(event.data);
    const client_id = errorData.client_id;
    const time_stamp = errorData.time_stamp;
    const error = errorData.error;

    setLogs((prevLogs) => [...prevLogs, {clientId: client_id, timeStamp: time_stamp, type: "Error", message: error}]);
    setErrors((prevErrors) => [...prevErrors, errorData]);
  };

  const handleDataPacket = async (event) => {
    const deviceData = JSON.parse(event.data);
    const client_id = deviceData.client_id;
    const data_type = deviceData.data.data_type;
    console.log(data_type);

    if(data_type === "log")
    {
      const time_stamp = deviceData.data.time_stamp;
      const log_level = deviceData.data.log_level;
      const log_message = deviceData.data.log_message;
      setLogs((prevLogs) => [...prevLogs, {clientId: client_id, timeStamp: time_stamp, type: log_level, message: log_message}]);
    }
    else if (data_type === "status") 
    {
      const deviceId = deviceData.data.deviceId;
      const status = deviceData.data.status;
      const tempTarget = deviceData.data.tempTarget;
      const phTarget = deviceData.data.phTarget;
      const rpmTarget = deviceData.data.rpmTarget;
      setDevices(prevState => 
        prevState.map(device =>
          device.deviceId === deviceId
            ? { deviceId: deviceId, status: status, tempTarget: tempTarget, phTarget: phTarget, rpmTarget: rpmTarget  } // Update the object with new attributes
            : device // Keep the object unchanged if deviceId doesn't match
        )
      );
    }
    else if (data_type === "temperature") 
    {
      setData((prevData) => [...prevData, deviceData]);
    }
    else if (data_type === "rpm") 
    {
      setData((prevData) => [...prevData, deviceData]);
    }
    else if (data_type === "ph") 
    {
      setData((prevData) => [...prevData, deviceData]);
    }
  };
  
  const handleDevicePacket = async (event) => {
    const deviceData = JSON.parse(event.data);
    const client_id = deviceData.client_id;
    const action = deviceData.action;

    if(action === "add")
    {
      const response = await getDeviceStatus(client_id);
      setDevices((prevDevices) => {
        // Find if the device exists
        const deviceIndex = prevDevices.findIndex(
          (device) => device.deviceId === response.deviceId
        );

        if (deviceIndex !== -1) {
          // Update existing device
          const updatedDevices = [...prevDevices];
          updatedDevices[deviceIndex] = {
            ...updatedDevices[deviceIndex],
            status: response.status,
            tempTarget: response.tempTarget,
            phTarget: response.phTarget,
            rpmTarget: response.rpmTarget,
          };
          return updatedDevices;
        } else {
          // Add new device
          return [
            ...prevDevices,
            {
              deviceId: response.deviceId,
              status: response.status,
              tempTarget: response.tempTarget,
              phTarget: response.phTarget,
              rpmTarget: response.rpmTarget,
            },
          ];
        }
      });
    }
    else if (action === "remove") 
    {
      setSelectedDevice((prevDevice) => {
        if(prevDevice && prevDevice.deviceId === client_id)
        {
          setSelectedDevice(null);
        }
      });
      setDevices((prevDevices) => prevDevices.filter(device => device.deviceId !== client_id));
    }
  };

  function changeSelectedDevice(device)
  {
    if(device)
    {
      setSelectedDevice(device);
    }
  }
    
  useEffect(() => {

      async function setAppStart() 
      { 
        setDevices(await getRegisteredAndConnectedDevices());
        await startSSEConnection(handleDataPacket, handleErrorPacket, handleDevicePacket);
      }

      setAppStart();
  }, []);

  return (
    <>
      <LocalizationProvider dateAdapter={AdapterDayjs}>
        <Router>
          <Routes>
            <Route element={<ProtectedRoute />}>
              <Route path="/" element={<Dashboard data={data} logs={logs} selectedDevice={selectedDevice} devices={devices} setSelectedDevice={changeSelectedDevice}/>} />
              <Route path="/logs" element={<Logs logs={logs} selectedDevice={selectedDevice} devices={devices} setSelectedDevice={changeSelectedDevice}/>} />
            </Route>  

            <Route path="/callback" element={<Callback />} />
            <Route path="*" element={<Notfound />} />
          </Routes>
        </Router>
      </LocalizationProvider>
    </>
  )
}

export default App

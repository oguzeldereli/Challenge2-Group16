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
  const [tempdata, setTempData] =  useState([]);
  const [phdata, setPhData] =  useState([]);
  const [rpmdata, setRpmData] =  useState([]);
  const [selectedDeviceId, setSelectedDeviceId] =  useState(null);
  const [logs, setLogs] = useState([]);

  const selectedDevice = selectedDeviceId
    ? devices.find((device) => device.deviceId === selectedDeviceId)
    : null;

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

    if(data_type === "log")
    {
      const time_stamp = deviceData.data.time_stamp;
      const log_level = deviceData.data.log_level;
      const log_message = deviceData.data.log_message;
      setLogs((prevLogs) => [...prevLogs, {clientId: client_id, timeStamp: time_stamp, type: log_level, message: log_message}]);
    }
    else if (data_type === "status") 
    {
      const deviceId = deviceData.client_id;
      const status = deviceData.data.data.status;
      const tempTarget = deviceData.data.data.tempTarget;
      const phTarget = deviceData.data.data.phTarget;
      const rpmTarget = deviceData.data.data.rpmTarget;
      
      setDevices((prevState) => {
        const index = prevState.findIndex((device) => device.deviceId === deviceId);
        return prevState.map((device, i) =>
          i === index
            ? { ...device, status, tempTarget, phTarget, rpmTarget }
            : device
        );
      });
    }
    else if (data_type === "temperature") 
    {
      setTempData((prevData) => [...prevData, deviceData]);
    }
    else if (data_type === "ph") 
    {
      setPhData((prevData) => [...prevData, deviceData]);
    }
    else if (data_type === "rpm") 
    {
      setRpmData((prevData) => [...prevData, deviceData]);
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
      if(selectedDeviceId && selectedDeviceId === client_id)
      {
        setSelectedDeviceId(null);
      }
      setDevices((prevDevices) => prevDevices.filter(device => device.deviceId !== client_id));
    }
  };

  function changeSelectedDevice(deviceId)
  {
    if(deviceId)
    {
      setSelectedDeviceId(deviceId);
    }
  }

  return (
    <>
      <LocalizationProvider dateAdapter={AdapterDayjs}>
        <Router>
          <Routes>
            <Route element={<ProtectedRoute />}>
              <Route path="/" element={<Dashboard setDevices={setDevices} handleDataPacket={handleDataPacket} handleDevicePacket={handleDevicePacket} handleErrorPacket={handleErrorPacket} tempdata={tempdata} phdata={phdata} rpmdata={rpmdata} logs={logs} selectedDevice={selectedDevice} devices={devices} setSelectedDevice={changeSelectedDevice}/>} />
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

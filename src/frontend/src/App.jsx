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
import { getRegisteredAndConnectedDevices } from './common/apiUtils';
import { DataProvider } from './components/DataProvider';
import { startSSEConnection } from './common/eventHandler';


function App() {
  const [devices, setDevices] =  useState([]);
  const [errors, setErrors] =  useState([]);
  const [data, setData] =  useState([]);
  const [selectedDevice, setSelectedDevice] =  useState("");
  const [logs, setLogs] = useState([]);

  const handleErrorPacket = (event) => {
    const errorData = JSON.parse(event.data);
    const client_id = errorData.client_id;
    const time_stamp = errorData.time_stamp;
    const error = errorData.error;

    setLogs((prevLogs) => [...prevLogs, {clientId: client_id, timeStamp: time_stamp, type: "Error", message: error}]);
    setErrors((prevErrors) => [...prevErrors, errorData]);
  };

  const handleDataPacket = (event) => {
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
  
  const handleDevicePacket = (event) => {
    const deviceData = JSON.parse(event.data);
    const client_id = deviceData.client_id;
    const action = deviceData.action;

    if(action === "add")
    {
      setDevices((prevDevices) => [...prevDevices, client_id]);
    }
    else if (action === "remove") 
    {
      setDevices((prevDevices) => prevDevices.filter(id => id !== client_id));
    }
  };
    
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
              <Route path="/" element={<Dashboard data={data} logs={logs} selectedDevice={selectedDevice} devices={devices} setSelectedDevice={setSelectedDevice}/>} />
              <Route path="/logs" element={<Logs logs={logs} selectedDevice={selectedDevice} devices={devices} setSelectedDevice={setSelectedDevice}/>} />
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

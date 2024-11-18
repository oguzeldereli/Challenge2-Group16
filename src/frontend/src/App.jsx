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


function App() {
  const [selectedDevice, setSelectedDevice] =  useState("");
  
  let devices = [];
    
  useEffect(() => {
      devices = getRegisteredAndConnectedDevices();
  }, []);

  const logs = [{type: "Error", message: "Failed to connect to device.", time: "12:11:59"}, {type: "Information", message: "Retrying connection...", time: "13:08:34"}, {type: "Success", message: "Connected to device.", time: "13:08:38"}];
    
  return (
    <>
      <LocalizationProvider dateAdapter={AdapterDayjs}>
        <Router>
          <Routes>
            <Route element={<ProtectedRoute />}>
              <Route path="/" element={<Dashboard logs={logs} selectedDevice={selectedDevice} devices={devices} setSelectedDevice={setSelectedDevice}/>} />
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

import { useState } from 'react'
import './App.css'
import {
  BrowserRouter as Router,
  Route,
  Link,
  Routes
} from "react-router-dom";
import Dashboard from "./pages/Dashboard"
import Notfound from "./pages/NotFound"
import ProtectedRoute from './components/ProtectedRoute';
import Callback from './callback';

function App() {

  return (
    <>
      <Router>
        <Routes>
          <Route element={<ProtectedRoute />}>
            <Route path="/" element={<Dashboard />} />
          </Route>  

          <Route path="/callback" element={<Callback />} />
          <Route path="*" element={<Notfound />} />
        </Routes>
      </Router>
    </>
  )
}

export default App

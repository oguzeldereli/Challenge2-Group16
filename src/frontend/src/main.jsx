import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './index.css'
import App from './App.jsx'
import { DataProvider } from './components/DataProvider.jsx'

createRoot(document.getElementById('root')).render(
  
  <DataProvider>
    <App />
  </DataProvider>,
)

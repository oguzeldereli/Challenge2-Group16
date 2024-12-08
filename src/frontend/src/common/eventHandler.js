import polyfilledEventSource from '@sanity/eventsource/browser'
import apiClient, { apiBaseUrl } from "./apiClient";
import { isAuthenticated } from './IsAuthenticated';

let eventSource = null;
export async function startSSEConnection(handleDataPacket, handleErrorPacket, handleDevicePacket)
{
    if (eventSource) {
      console.log("SSE Connection already established");
      return;
    }

    const accessToken = localStorage.getItem("accessToken");
    
    const headers = {
        Authorization: `Bearer ${accessToken}`
    };

    eventSource = new polyfilledEventSource(apiBaseUrl + '/devices/events', { headers });

    eventSource.onmessage = (event) => {
        const data = JSON.parse(event.data); 
        document.getElementById('time').textContent = `Time: ${data.time}`;
      };
  
      eventSource.onerror = (event) => {
        console.log("Error on event");
      };
  
      eventSource.addEventListener('data', async (event) => await handleDataPacket(event));

      eventSource.addEventListener('error', async (event) => await handleErrorPacket(event));

      eventSource.addEventListener('device', async (event) => await handleDevicePacket(event));
}

export function stopSSEConnection() {
  if (eventSource) {
    eventSource.close();
    eventSource = null;
  }
}
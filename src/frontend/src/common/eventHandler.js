import apiClient, { apiBaseUrl } from "./apiClient";


export function startSSEConnection()
{
    const eventSource = new EventSource(apiBaseUrl + '/devices/events');
}
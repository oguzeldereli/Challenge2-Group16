import { isAuthenticated } from "./IsAuthenticated";

const api = 'https://localhost:443';

export async function getRegisteredAndConnectedDevices()
{
    if(!isAuthenticated())
    {
        return null;
    }
    
    const accessToken = localStorage.getItem("accessToken")
    try {
        const response = await fetch(`${api}/devices`, {
            method: 'GET',
            headers: {
                'Authorization': `Bearer ${accessToken}`
            }
        });

        const responseData = await response.json();

        console.log(responseData);
        if (response.ok && responseData) {
            return responseData;
        }


        return null;
    } catch (error) {
        console.error("Authentication check failed:", error);
    }
}

export async function getDeviceStatus(id) {
    if(!isAuthenticated())
    {
        return null;
    }
    
    const accessToken = localStorage.getItem("accessToken")
    try {
        const response = await fetch(`${api}/devices/${id}/status`, {
            method: 'GET',
            headers: {
                'Authorization': `Bearer ${accessToken}`
            }
        });

        const responseData = await response.json();

        console.log(responseData);
        if (response.ok && responseData) {
            return responseData;
        }


        return null;
    } catch (error) {
        console.error("Authentication check failed:", error);
    }
}

export async function setDeviceTarget(id, dataType, target)
{
    if(!isAuthenticated())
    {
        return null;
    }
    const accessToken = localStorage.getItem("accessToken")
    try {
        const response = await fetch(`${api}/devices/${id}/target`, {
            method: 'POST',
            headers: {
                'Authorization': `Bearer ${accessToken}`,
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                dataType: dataType, // Ensure keys match DTO property names (case-insensitive)
                target: target
            })
        });

        const responseData = await response.json();

        console.log(responseData);
        if (response.ok && responseData) {
            return responseData;
        }

        return null;
    } catch (error) {
        console.error("Authentication check failed:", error);
    }
}

export async function GetData(id, type, timestamp1, timestamp2)
{
    if(!isAuthenticated())
    {
        return null;
    }
    
    const accessToken = localStorage.getItem("accessToken")
    try {
        const response = await fetch(`${api}/devices/${id}/${type}?timeStamp1=${timestamp1}&timeStamp2=${timestamp2}`, {
            method: 'GET',
            headers: {
                'Authorization': `Bearer ${accessToken}`
            }
        });

        const responseData = await response.json();

        console.log(responseData);
        if (response.ok && responseData) {
            return responseData;
        }


        return null;
    } catch (error) {
        console.error("Authentication check failed:", error);
    }
}

export async function StartDevice(id)
{
    if(!isAuthenticated())
    {
        return null;
    }
    
    const accessToken = localStorage.getItem("accessToken")
    try {
        const response = await fetch(`${api}/devices/${id}/start`, {
            method: 'POST',
            headers: {
                'Authorization': `Bearer ${accessToken}`
            }
        });

        const responseData = await response.json();

        console.log(responseData);
        if (response.ok && responseData) {
            return responseData;
        }


        return null;
    } catch (error) {
        console.error("Authentication check failed:", error);
    }
}

export async function PauseDevice(id)
{
    if(!isAuthenticated())
    {
        return null;
    }
    
    const accessToken = localStorage.getItem("accessToken")
    try {
        const response = await fetch(`${api}/devices/${id}/pause`, {
            method: 'POST',
            headers: {
                'Authorization': `Bearer ${accessToken}`
            }
        });

        const responseData = await response.json();

        console.log(responseData);
        if (response.ok && responseData) {
            return responseData;
        }


        return null;
    } catch (error) {
        console.error("Authentication check failed:", error);
    }
}
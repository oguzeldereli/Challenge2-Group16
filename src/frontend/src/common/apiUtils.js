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
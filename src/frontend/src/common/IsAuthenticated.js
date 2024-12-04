import { TryRefreshAccessToken } from "./apiClient";


const api = 'https://localhost:443';

export async function isAuthenticated() 
{
    
    const accessToken = localStorage.getItem("accessToken")
    const refreshToken = localStorage.getItem("refreshToken")
    if(!refreshToken || !refreshToken.trim())
    {
        return false;
    }

    if(!accessToken || !accessToken.trim())
    {
        const {accessToken: accessToken1} = await TryRefreshAccessToken();
        accessToken = accessToken1;
    }

    try {
        const response = await fetch(`${api}/auth/is-signed-in`, {
            method: 'GET',
            headers: {
                'Authorization': `Bearer ${accessToken}`
            }
        });

        // Return based on the status code
        if (response.ok) {
            return true;
        }
    } catch (error) {
        console.error("Authentication check failed:", error);
    }

    return false;
}

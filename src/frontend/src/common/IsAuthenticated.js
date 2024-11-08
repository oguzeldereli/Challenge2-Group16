    // auth.js or similar file

export async function isAuthenticated() 
{
    try {
        const accessToken = localStorage.getItem("access_token")
        const refreshToken = localStorage.getItem("refresh_token")
        
        if(!refreshToken || refreshToken.trim() === "" || !accessToken || accessToken.trim() === "")
        {
            return false;
        }

        return true;
    } catch (error) {
        console.error("Failed to check auth status", error);
        return false;
    }
}

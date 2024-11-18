import apiClient, { TryRefreshAccessToken } from "./apiClient";
import { isAuthenticated } from "./IsAuthenticated";
import { jwtDecode } from "jwt-decode";

const api = 'https://localhost:443';

export function generateCodeVerifier(length = 128) {
    const array = new Uint8Array(length);
    window.crypto.getRandomValues(array);

    const string = String.fromCharCode(...array);
    const base64String = btoa(string)
        .replace(/\+/g, '-')
        .replace(/\//g, '_')
        .replace(/=+$/, '');
    
    return base64String;
}

export function generateState(length = 32) {
    const array = new Uint8Array(length);
    window.crypto.getRandomValues(array);

    const string = String.fromCharCode(...array);
    const base64String = btoa(string)
        .replace(/\+/g, '-')
        .replace(/\//g, '_')
        .replace(/=+$/, '');
    
    return base64String;
}

export async function generateCodeChallenge(codeVerifier) {
    const encoder = new TextEncoder();
    const data = encoder.encode(codeVerifier);
    const digest = await window.crypto.subtle.digest('SHA-256', data);
    
    // Convert digest (ArrayBuffer) directly to Base64
    let base64String = btoa(String.fromCharCode(...new Uint8Array(digest)));
    
    // Make the string URL-safe by replacing certain characters
    base64String = base64String
        .replace(/\+/g, '-')
        .replace(/\//g, '_')
        .replace(/=+$/, '');

    return base64String;
}


export const scheduleTokenRefresh = (accessToken) => {
    const { exp } = jwtDecode(accessToken);
    const timeout = exp * 1000 - Date.now() - 10000; // Refresh 10 seconds before expiry

    setTimeout(async () => {
        try {
            const {accessToken} = await TryRefreshAccessToken()

            scheduleTokenRefresh(accessToken);
        } catch (error) {
            console.error('Token refresh failed');
            localStorage.clear();
            window.location.href = '/';
        }
    }, timeout);
};

export async function SignOut()
{
    const refreshToken = localStorage.getItem('refreshToken');
    const accessToken = localStorage.getItem('accessToken');

    if (!refreshToken?.trim() || !accessToken?.trim()) {
        console.warn('No valid tokens found, skipping sign-out.');
        return;
    }

    try {
        const response = await apiClient.post(
            `/auth/oauth/revoke`,
            new URLSearchParams({
                accessToken: accessToken,
                refreshToken: refreshToken,
            }),
            {
                headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
            }
        );

        
        const responseData = await response.json();

        console.log('Sign-out successful:', responseData);
    } catch (error) {
        console.error('Sign-out failed:', error);
    } finally {
        // Clean up tokens after sign-out attempt
        localStorage.clear();
        window.location.href = "/";
    }
}
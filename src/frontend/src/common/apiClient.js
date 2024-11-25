import axios from 'axios';
import { jwtDecode } from 'jwt-decode';

export const apiBaseUrl = "https://localhost:443";

const apiClient = axios.create({
    baseURL: 'https://localhost:443'
});

export async function TryRefreshAccessToken()
{
    const storedRefreshToken = localStorage.getItem('refreshToken');
    const response = await apiClient.post(
        `/auth/oauth/refresh`,
        new URLSearchParams({
            grant_type: 'refresh_token', 
            refresh_token: storedRefreshToken,
            scope: "all",
        }),
        {
            headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
        }
    );
    
    const responseData = await response.json();

    const { accessToken, tokenType, expiresIn, refreshToken } = responseData;
    localStorage.setItem('accessToken', accessToken);
    localStorage.setItem('refreshToken', refreshToken);
    localStorage.setItem('expiresIn', expiresIn);

    return responseData;
}

apiClient.interceptors.request.use(
    async (config) => {
        const accessToken = localStorage.getItem('accessToken');
        if (accessToken) {
            const {exp} = jwtDecode(accessToken);
            if(exp * 1000 <= Date.now())
            {
                const {accessToken: accessToken1} = await TryRefreshAccessToken();
                config.headers.Authorization = `Bearer ${accessToken1}`;
            }
            else
            {
                config.headers.Authorization = `Bearer ${accessToken}`;
            }
        }

        return config;
    },
    (error) => Promise.reject(error)
);

apiClient.interceptors.response.use(
    (response) => response,
    async (error) => {
        const originalRequest = error.config;

        if (error.response.status === 401 && !originalRequest._retry) {
            originalRequest._retry = true;

            try {
                
                const { accessToken } = await TryRefreshAccessToken();
                
                apiClient.defaults.headers.common.Authorization = `Bearer ${accessToken}`;
                return apiClient(originalRequest);
            } catch (refreshError) {
                console.error('Token refresh failed', refreshError);

                localStorage.clear();
                window.location.href = '/';
                return Promise.reject(refreshError);
            }
        }

        return Promise.reject(error);
    }
);

export default apiClient;
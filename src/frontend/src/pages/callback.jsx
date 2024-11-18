import { useState, useEffect } from "react";
import { Route, Navigate, Link, Outlet, useLocation } from "react-router-dom";
import { scheduleTokenRefresh } from "../common/authUtils";

const tokenEndpoint = 'https://localhost:443/auth/oauth/token';
const clientId = '5a672cc7-1f72-4c1c-82d6-94657afbf4ef';
const redirectUri = 'http://localhost:5173/callback';

function Callback() {
  const [isAuth, setIsAuth] = useState(null);
  const location = useLocation();
  const urlParams = new URLSearchParams(location.search);

  useEffect(() => {
    const fetchToken = async (urlParams) => {
      const code = urlParams.get('code');
      const urlState = urlParams.get('state');
      const codeVerifier = localStorage.getItem('pkceCodeVerifier');
      const state = localStorage.getItem('state');

      if (!code || !codeVerifier || !state || !urlState) {
        console.error('Authorization code or state or code verifier missing');
        setIsAuth(false);
        return;
      }

      if (state !== urlState) {
        console.error('Authorization state incorrect');
        setIsAuth(false);
        return;
      }

      try {
        const response = await fetch(tokenEndpoint, {
          method: 'POST',
          headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
          body: new URLSearchParams({
            grant_type: 'authorization_code',
            client_id: clientId,
            code,
            redirect_uri: redirectUri,
            code_verifier: codeVerifier,
          }),
        });

        if (!response.ok) {
          throw new Error(`HTTP error! status: ${response.status}`);
        }

        const responseData = await response.json();
        if (responseData.accessToken && responseData.refreshToken) {
          localStorage.setItem('accessToken', responseData.accessToken);
          localStorage.setItem('refreshToken', responseData.refreshToken);
          if (responseData.expiresIn) {
            localStorage.setItem('expiresIn', responseData.expiresIn);
          }
          scheduleTokenRefresh(responseData.accessToken);
          setIsAuth(true);
        } else {
          console.error('Token exchange failed:', responseData);
          setIsAuth(false);
        }
      } catch (error) {
        console.error('An error occurred:', error.message);
        setIsAuth(false);
      }
    };

    fetchToken(urlParams);
  }, [urlParams]);

  if (isAuth === null) {
    return <div>Loading...</div>; // Placeholder while checking auth
  }

  return isAuth ? <Navigate to="/" /> : <Navigate to="/" />;
}

export default Callback;
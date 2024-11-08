import { useState, useEffect } from "react";
import { Route, Navigate, Link, Outlet } from "react-router-dom";

const tokenEndpoint = 'https://localhost:443/auth/oauth/token';
const clientId = '5a672cc7-1f72-4c1c-82d6-94657afbf4ef';
const redirectUri = 'http://localhost:5173/callback';

function Callback() {
  
  const [isAuth, setIsAuth] = useState(null);

  useEffect(() => {
    const fetchToken = async () => {
      const urlParams = new URLSearchParams(window.location.search);
      const code = urlParams.get('code');
      const urlState = urlParams.get('state');
      const codeVerifier = localStorage.getItem('pkce_code_verifier');
      const state = localStorage.getItem('state');

      if (!code || !codeVerifier || !state || !urlState) {
        console.error('Authorization code or state or code verifier missing');
        return;
      }
      
      if(state !== urlState)
      {
        console.log(state);
        console.log(urlState);
        console.error('Authorization state incorrect');
        return;
      }

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

      const data = await response.json();
      
      console.log(data);
      if (data.accessToken && data.refreshToken) {
        setIsAuth(true);
        localStorage.setItem('access_token', data.accessToken);
        localStorage.setItem('refresh_token', data.refreshToken);
        if (data.expiresIn)
        {
            localStorage.setItem('access_expires_in', data.expiresIn);
        }
        
      } else {
        console.error('Token exchange failed:', data);
      }
    };

    fetchToken();
  }, []);

  return isAuth ? <Navigate to="/"/> : <Navigate to="/"/>;
}

export default Callback;
// ProtectedRoute.js
import { useState, useEffect } from "react";
import { Route, Navigate, Link, Outlet } from "react-router-dom";
import { isAuthenticated } from "../common/IsAuthenticated"; // import the function above
import { generateCodeVerifier, generateCodeChallenge, generateState } from '../common/authUtils';


const clientId = '5a672cc7-1f72-4c1c-82d6-94657afbf4ef';
const authorizationEndpoint = 'https://localhost:443/auth/authorize';
const redirectUri = 'http%3A%2F%2Flocalhost%3A5173%2Fcallback';
const scope = 'all';

const ProtectedRoute = () => {
    const [isAuth, setIsAuth] = useState(null);

    useEffect(() => {
        const checkAuth = async () => {
            const authStatus = await isAuthenticated();
            setIsAuth(authStatus);
        };
        checkAuth();
    }, []);

    if(isAuth == false)
    {
        const verifier = generateCodeVerifier();
        const state = generateState();
        generateCodeChallenge(verifier).then(code_challenge => {
            // Store verifier and state in local storage
            localStorage.setItem('pkce_code_verifier', verifier);
            localStorage.setItem('state', state);
            
            // Set the window location after code_challenge is ready
            window.location.href = `${authorizationEndpoint}?response_type=code&client_id=${clientId}&redirect_uri=${redirectUri}&scope=${scope}&state=${state}&code_challenge=${code_challenge}&code_challenge_method=S256`;
        });
    }

    return isAuth ? <Outlet /> : null;
};

export default ProtectedRoute;  
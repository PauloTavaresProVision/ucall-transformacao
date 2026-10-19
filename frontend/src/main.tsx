import React from 'react';import{createRoot}from'react-dom/client';import App from './App';import DevelopmentRequestForm from './DevelopmentRequestForm';import './styles.css';
createRoot(document.getElementById('root')!).render(<React.StrictMode>{window.location.pathname.replace(/\/$/,'')==='/pedidos/novo'?<DevelopmentRequestForm/>:<App/>}</React.StrictMode>);


import React from 'react';import{createRoot}from'react-dom/client';import App from './App';import DevelopmentRequestForm from './DevelopmentRequestForm';import ReviewPortal from './ReviewPortal';import './styles.css';
createRoot(document.getElementById('root')!).render(<React.StrictMode>{window.location.pathname.replace(/\/$/,'')==='/pedidos/novo'?<DevelopmentRequestForm/>:window.location.pathname.replace(/\/$/,'')==='/pedidos/meus'?<ReviewPortal/>:<App/>}</React.StrictMode>);


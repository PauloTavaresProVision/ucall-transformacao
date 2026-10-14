-- Executar no PostgreSQL existente, com uma conta que possa criar roles e bases.
-- Substituir a password localmente. Este ficheiro não deve guardar credenciais reais.
CREATE ROLE ucall_transformation LOGIN PASSWORD 'SUBSTITUIR_LOCALMENTE';
CREATE DATABASE ucall_transformation OWNER ucall_transformation;
-- Ligar à nova base antes de executar 001-initial.sql.

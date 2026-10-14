# UCALL Transformação

## Colocar a aplicação no servidor

A instalação cria automaticamente aplicação, PostgreSQL, base de dados, credenciais e volumes. Não pede domínio, rede ou dados de PostgreSQL. Requer Docker e Docker Compose v2 já instalados.

Primeira instalação no PuTTY:

```bash
git clone https://github.com/PauloTavaresProVision/ucall-transformacao.git
cd ucall-transformacao
bash instalar.sh
```

Se a pasta já existe:

```bash
cd ucall-transformacao
git pull --ff-only
bash instalar.sh
```

## Nginx

O instalador tenta ligar automaticamente a aplicação à rede de um Nginx Proxy Manager existente.

No Nginx Proxy Manager:
- Scheme: http
- Forward Hostname: ucall-transformation
- Forward Port: 8080
- Activar SSL/HTTPS no domínio escolhido.

Se o proxy não partilhar uma rede Docker, usar o IP do servidor com porta 7766. O domínio é configurado apenas no Nginx; não é necessário durante a instalação. O login de produção exige HTTPS.

## Primeira conta

Abrir o domínio HTTPS e criar a conta de Paulo. O código de instalação está no campo BOOTSTRAP_TOKEN de .env.online. A configuração inicial deixa de estar disponível depois da primeira conta. A password do PostgreSQL é gerada automaticamente.

Esta instalação leva o código completo e os dados iniciais do mandato. Os utilizadores, anexos e valores entretanto gravados no PC não são enviados no repositório. A transferência desses dados exige uma migração da base e do volume de ficheiros e chaves.

## Preservação e actualizações

.env.online e os volumes são preservados quando se repete instalar.sh. Não apagar os volumes. Guardar a base e o volume app_data, que contém anexos e chaves de cifragem.

Uma instalação anteriormente configurada com .env.contabo e compose.contabo.yaml é actualizada mantendo a ligação e os dados existentes.

## Diagnóstico

```bash
docker logs --tail 80 ucall-transformation
docker compose --env-file .env.online -f compose.online.yaml ps
```

## Estado da aplicação

Acções, processos, KPIs, reuniões, decisões, Gate, reporte, utilizadores, alertas e configuração SMTP/OpenAI estão incluídos. Áudio de reuniões e pipeline de iniciativas ainda não estão implementados.

Frontend compilado. A instalação real na Contabo ainda tem de ser executada; não existe acesso remoto ao servidor nesta sessão.
